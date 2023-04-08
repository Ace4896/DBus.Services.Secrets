using System;
using System.Threading.Tasks;
using DBus.Services.Secrets.Sessions;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace DBus.Services.Secrets;

public sealed class SecretService
{
    private OrgFreedesktopSecretService _serviceProxy;

    private Connection _connection;
    private ISession _session;

    internal SecretService(Connection connection, ISession session)
    {
        _connection = connection;
        _session = session;

        _serviceProxy = new OrgFreedesktopSecretService(connection, Constants.ServiceName, Constants.ServicePath);
    }

    /// <summary>
    /// Connects to the D-Bus Secret Service.
    /// </summary>
    /// <param name="encryptionType">The encryption method to use for transporting secrets.</param>
    /// <returns>A new instance of the <see cref="SecretService"/> class.</returns>
    public static async Task<SecretService> ConnectAsync(EncryptionType encryptionType)
    {
        Connection connection = new(Address.Session!);
        await connection.ConnectAsync();

        // Open a new session based on the specified encryption type
        ISession session = encryptionType switch
        {
            EncryptionType.Plain => await PlainSession.OpenPlainSessionAsync(connection),
            EncryptionType.Dh => await DhSession.OpenDhSessionAsync(connection),
            _ => throw new NotImplementedException($"{encryptionType} encryption type not implemented")
        };

        return new SecretService(connection, session);
    }

    /// <summary>
    /// Retrieves the collection with the specified alias if it exists.
    /// </summary>
    /// <param name="alias">The collection's alias.</param>
    /// <returns>The <see cref="Collection"/> with this alias, or <see langword="null"/> if no collection with this alias exists.</returns>
    public async Task<Collection?> GetCollectionByAliasAsync(string alias)
    {
        ObjectPath collectionPath = await _serviceProxy.ReadAliasAsync(alias);

        if (collectionPath == "/")
        {
            return null;
        }

        return new Collection(_connection, _session, collectionPath);
    }

    /// <summary>
    /// Retrieves the collection with the default alias.
    /// </summary>
    /// <returns>The <see cref="Collection"/> with the default alias, or <see langword="null"/> if no default collection exists.</returns>
    public Task<Collection?> GetDefaultCollectionAsync() => GetCollectionByAliasAsync(Constants.DefaultCollectionAlias);
}
