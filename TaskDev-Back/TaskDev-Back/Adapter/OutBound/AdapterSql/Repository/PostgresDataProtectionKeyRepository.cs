using System.Xml.Linq;
using Dapper;
using Microsoft.AspNetCore.DataProtection.Repositories;
using TaskDev.Api.Adapter.OutBound.AdapterSql.Settings;
using TaskDev.Api.Domain.Ports.OutBound;

namespace TaskDev.Api.Adapter.OutBound.AdapterSql.Repository;

internal sealed class PostgresDataProtectionKeyRepository(
    IDatabaseConnectionFactory connectionFactory, ISecretEncryptor encryptor) : IXmlRepository
{
    public IReadOnlyCollection<XElement> GetAllElements()
    {
        using var connection = connectionFactory.CreateConnection();
        return connection.Query<string>("SELECT encrypted_xml FROM taskdev_security.data_protection_keys ORDER BY id")
            .Select(value => XElement.Parse(encryptor.Decrypt(value)))
            .ToArray();
    }

    public void StoreElement(XElement element, string friendlyName)
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Execute("INSERT INTO taskdev_security.data_protection_keys (encrypted_xml) VALUES (@xml)",
            new { xml = encryptor.Encrypt(element.ToString(SaveOptions.DisableFormatting)) });
    }
}
