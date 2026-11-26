using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using TaskDev.Api.Adapter.OutBound.AdapterHttp.Client;
using TaskDev.Api.Adapter.OutBound.AdapterSql.Repository;
using TaskDev.Api.Adapter.OutBound.AdapterSql.Settings;

namespace TaskDev.Api.Infra.Configuration.OutBound;

public static class ConfigurationDataProtection
{
    public static void AddPostgresDataProtection(this IServiceCollection services, IConfiguration configuration)
    {
        if (!configuration.GetValue<bool>("DataProtection:PersistKeysToPostgres"))
        {
            return;
        }

        services.AddDataProtection().SetApplicationName("TaskDev");
        services.AddOptions<KeyManagementOptions>().Configure<IDatabaseConnectionFactory>((options, connectionFactory) =>
            options.XmlRepository = new PostgresDataProtectionKeyRepository(
                connectionFactory, SecretEncryptorAesGcm.ForDataProtection(configuration)));
    }
}
