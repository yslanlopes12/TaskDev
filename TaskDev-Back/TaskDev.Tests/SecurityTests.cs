using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TaskDev.Api.Adapter.InBound.AdapterHttp.Routes;
using TaskDev.Api.Adapter.OutBound.AdapterHttp.Client;
using TaskDev.Api.Domain.Core.Entities;
using TaskDev.Api.Domain.Core.Models;
using TaskDev.Api.Domain.Ports.InBound;
using TaskDev.Api.Domain.Ports.OutBound;
using TaskDev.Api.Infra.Configuration.InBound;
using TaskDev.Api.Infra.Configuration.OutBound;

namespace TaskDev.Tests;

public class SecurityTests
{
    private static IConfiguration Config => new ConfigurationBuilder().Build();

    [Theory]
    [InlineData(null, null, 403)]
    [InlineData(null, "chosen-by-client", 403)]
    [InlineData("cookie-token", null, 403)]
    [InlineData("cookie-token", "different", 403)]
    [InlineData("same-token", "same-token", 204)]
    public async Task AuthenticatedWritesRequireBothMatchingCsrfTokens(string? cookie, string? header, int expected)
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test")], "test"));
        context.Request.Method = "POST";
        if (cookie != null) context.Request.Headers.Cookie = $"XSRF-TOKEN={cookie}";
        if (header != null) context.Request.Headers[CsrfMiddleware.AlternativeHeaderName] = header;
        var middleware = new CsrfMiddleware(ctx => { ctx.Response.StatusCode = 204; return Task.CompletedTask; }, Config);
        await middleware.InvokeAsync(context);
        Assert.Equal(expected, context.Response.StatusCode);
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("169.254.169.254")]
    [InlineData("10.0.0.1")]
    [InlineData("172.16.0.1")]
    [InlineData("192.168.1.1")]
    [InlineData("100.64.0.1")]
    [InlineData("0.0.0.0")]
    [InlineData("224.0.0.1")]
    [InlineData("::")]
    [InlineData("::1")]
    [InlineData("::ffff:127.0.0.1")]
    [InlineData("fd00::1")]
    [InlineData("fe80::1")]
    [InlineData("ff02::1")]
    public void AgentRejectsRestrictedDestinations(string address)
    {
        var policy = new AgentEndpointPolicy(Config);
        Assert.Throws<ArgumentException>(() => policy.ValidateAddress(new Uri("https://example.com"), IPAddress.Parse(address)));
    }

    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("2606:4700:4700::1111")]
    public void AgentAllowsPublicUnicast(string address) => Assert.True(AgentEndpointPolicy.IsPublicAddress(IPAddress.Parse(address)));

    [Theory]
    [InlineData("file:///etc/passwd")]
    [InlineData("http://example.com")]
    [InlineData("https://user:password@example.com")]
    [InlineData("https://example.com/#fragment")]
    [InlineData("http://localhost:11434")]
    public void AgentRejectsUnsafeEndpointSyntax(string endpoint) =>
        Assert.Throws<ArgumentException>(() => new AgentEndpointPolicy(Config).ValidateEndpoint(endpoint));

    [Fact]
    public void LocalOllamaRequiresExplicitOptInAndCannotResolveToOtherPrivateServices()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Agent:AllowLocalOllama"] = "true" }).Build();
        var policy = new AgentEndpointPolicy(config);
        var uri = policy.ValidateEndpoint("http://localhost:11434/v1/chat/completions");
        policy.ValidateAddress(uri, IPAddress.Loopback);
        Assert.Throws<ArgumentException>(() => policy.ValidateEndpoint("http://localhost:8080"));
        Assert.Throws<ArgumentException>(() => policy.ValidateAddress(uri, IPAddress.Parse("10.0.0.1")));
    }

    [Fact]
    public void AgentTransportDoesNotFollowRedirectsOrUseAnUnvalidatedProxy()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Config);
        services.AddHttpClientAdapterConfiguration();
        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpMessageHandlerFactory>();
        var handler = factory.CreateHandler(typeof(IAgentHttpClient).Name);
        while (handler is DelegatingHandler wrapper) handler = wrapper.InnerHandler!;
        var sockets = Assert.IsType<SocketsHttpHandler>(handler);
        Assert.False(sockets.AllowAutoRedirect);
        Assert.False(sockets.UseProxy);
        Assert.NotNull(sockets.ConnectCallback);
        Assert.Equal(1024 * 1024, provider.GetRequiredService<IHttpClientFactory>()
            .CreateClient(typeof(IAgentHttpClient).Name).MaxResponseContentBufferSize);
    }

    [Fact]
    public async Task ProviderErrorDoesNotExposeTheResponseBody()
    {
        using var http = new HttpClient(new ProviderErrorHandler());
        var client = new AgentHttpClient(http, NullLogger<AgentHttpClient>.Instance, new AgentEndpointPolicy(Config));
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.TestConnectionAsync("https://example.com/v1/chat/completions", null, "OPENAI_COMPATIBLE", "test"));
        Assert.Contains("401", exception.Message);
        Assert.DoesNotContain("private-provider-detail", exception.Message);
    }

    [Fact]
    public async Task AuthRoutesActuallyEnableTheConfiguredRateLimiter()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSessionAndSecurity(builder.Configuration);
        builder.Services.AddScoped<IAuthUseCase>(_ => throw new NotSupportedException("Metadata test only."));
        await using var app = builder.Build();
        app.MapEndpointAuth();
        var endpoints = ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>();
        foreach (var route in new[] { "/api/auth/login", "/api/auth/register" })
        {
            var endpoint = Assert.Single(endpoints, endpoint => endpoint.RoutePattern.RawText == route);
            Assert.Equal(ConfigurationSession.AuthPolicyName, endpoint.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName);
        }
    }

    [Fact]
    public async Task SessionRejectsExpiredAndLegacyCookiesAndReadsTheCurrentRole()
    {
        var protection = new EphemeralDataProtectionProvider();
        var repository = DispatchProxy.Create<IUserRepository, UserRepositoryProxy>();
        var proxy = (UserRepositoryProxy)repository;
        proxy.User = new User { Id = Guid.NewGuid(), Username = "member", Role = "MEMBER", IsActive = true, AuthVersion = 1 };
        var handler = new SessionHandler(protection, repository, Config);
        string Cookie(DateTimeOffset expires) => protection.CreateProtector("TaskDev.Session.Protection.v1").Protect(JsonSerializer.Serialize(new AuthSessionModel {
            UserId = proxy.User.Id, AuthVersion = 1, Role = "ADMIN", Username = "old", ExpiresAt = expires
        }));
        DefaultHttpContext Context(string cookie) { var context = new DefaultHttpContext(); context.Request.Headers.Cookie = $"taskdev-session={cookie}"; return context; }
        Assert.Null(await handler.GetCurrentUserAsync(Context(Cookie(DateTimeOffset.UtcNow.AddMinutes(-1)))));
        Assert.Null(await handler.GetCurrentUserAsync(Context(Cookie(default))));
        var session = await handler.GetCurrentUserAsync(Context(Cookie(DateTimeOffset.UtcNow.AddDays(1))));
        Assert.NotNull(session);
        Assert.Equal("MEMBER", session.Role);
        Assert.Equal("member", session.Username);
    }
}

public class ProviderErrorHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized) { Content = new StringContent("private-provider-detail") });
}

public class UserRepositoryProxy : DispatchProxy
{
    public User User { get; set; } = new();
    protected override object? Invoke(MethodInfo? method, object?[]? args) => method?.Name == nameof(IUserRepository.GetByIdAsync)
        ? Task.FromResult<User?>(User) : throw new NotSupportedException();
}
