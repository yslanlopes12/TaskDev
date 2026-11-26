using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace TaskDev.Api.Infra.Configuration.InBound;

public static class ConfigurationForwardedHeaders
{
    public static bool AddTrustedForwardedHeaders(this IServiceCollection services, IConfiguration configuration)
    {
        var proxies = configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [];
        var networks = configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? [];
        if (proxies.Length == 0 && networks.Length == 0)
        {
            return false;
        }

        var trustedProxies = proxies.Select(IPAddress.Parse).ToArray();
        var trustedNetworks = networks.Select(System.Net.IPNetwork.Parse).ToArray();
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;
            options.KnownProxies.Clear();
            options.KnownIPNetworks.Clear();
            foreach (var proxy in trustedProxies)
            {
                options.KnownProxies.Add(proxy);
            }

            foreach (var network in trustedNetworks)
            {
                options.KnownIPNetworks.Add(network);
            }
        });
        return true;
    }
}
