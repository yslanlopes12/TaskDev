using System.Net;
using TaskDev.Api.Adapter.OutBound.AdapterHttp.Client;
using TaskDev.Api.Domain.Ports.OutBound;

namespace TaskDev.Api.Infra.Configuration.OutBound;

public static class ConfigurationHttpClientAdapter
{
    public static IServiceCollection AddHttpClientAdapterConfiguration(this IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddHttpClient<IAgentHttpClient, AgentHttpClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
        })
        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            ConnectTimeout = TimeSpan.FromSeconds(10),
            AutomaticDecompression = DecompressionMethods.All,
        });

        services.AddSingleton<IPasswordHasher, PasswordHasherArgon2>();
        services.AddSingleton<ISecretEncryptor, SecretEncryptorAesGcm>();

        return services;
    }
}
