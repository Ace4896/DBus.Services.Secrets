using System;
using System.Linq;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace DBus.Services.Secrets;

public class SecretService
{
    private OrgFreedesktopSecretService _serviceProxy;

    private Connection _connection;
    private Session _session;

    internal SecretService(Connection connection, Session session)
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

        OrgFreedesktopSecretService serviceProxy = new(connection, Constants.ServiceName, Constants.ServicePath);

        // Open a new session based on the provided input type
        Session session = encryptionType switch
        {
            EncryptionType.Plain => await OpenPlainSessionAsync(serviceProxy),
            EncryptionType.DH => await OpenDhSessionAsync(serviceProxy),
            _ => throw new NotImplementedException($"{encryptionType} encryption type not implemented")
        };

        return new SecretService(connection, session);
    }

    private static async Task<Session> OpenPlainSessionAsync(OrgFreedesktopSecretService serviceProxy)
    {
        (_, ObjectPath sessionPath) = await serviceProxy.OpenSessionAsync(Constants.SessionAlgorithmPlain, Constants.SessionInputPlain);
        return new Session(sessionPath);
    }

    private static async Task<Session> OpenDhSessionAsync(OrgFreedesktopSecretService serviceProxy)
    {
        // Input for a DH encrypted session is our DH public key
        // Output from OpenSession call is service's DH public key
        DhKeypair dhKeypair = new();
        byte[] clientPublicKeyBytes = dhKeypair.PublicKey.ToByteArray(true, true);  // Unsigned, big endian

        DBusVariantItem sessionInput = new("ay", new DBusArrayItem(DBusType.Byte, clientPublicKeyBytes.Select(b => new DBusByteItem(b))));
        (DBusVariantItem sessionOutput, ObjectPath sessionPath) = await serviceProxy.OpenSessionAsync(Constants.SessionAlgorithmDh, sessionInput);

        if (sessionOutput.Value is not DBusArrayItem { ArrayType: DBusType.Byte } sessionOutputArray)
        {
            throw new InvalidCastException("Could not retrieve server DH public key bytes");
        }

        byte[] serverPublicKeyBytes = sessionOutputArray
            .Cast<DBusByteItem>()
            .Select(b => b.Value)
            .ToArray();
            
        byte[] aesKey = dhKeypair.DeriveSharedSecret(serverPublicKeyBytes);

        return new Session(sessionPath, aesKey);
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

    /// <summary>
    /// Locks the specified <see cref="Collection"/>.
    /// </summary>
    /// <param name="collection">The <see cref="Collection"/> to lock.</param>
    public Task LockAsync(Collection collection) => LockOrUnlockAsync(true, collection.CollectionPath);

    /// <summary>
    /// Locks the specified <see cref="Item"/>.
    /// </summary>
    /// <param name="item">The <see cref="Item"/> to lock.</param>
    public Task LockAsync(Item item) => LockOrUnlockAsync(true, item.ItemPath);

    /// <summary>
    /// Unlocks the specified <see cref="Collection"/>.
    /// </summary>
    /// <param name="collection">The <see cref="Collection"/> to unlock.</param>
    public Task UnlockAsync(Collection collection) => LockOrUnlockAsync(false, collection.CollectionPath);

    /// <summary>
    /// Unlocks the specified <see cref="Item"/>.
    /// </summary>
    /// <param name="item">The <see cref="Item"/> to unlock.</param>
    public Task UnlockAsync(Item item) => LockOrUnlockAsync(false, item.ItemPath);

    private async Task LockOrUnlockAsync(bool newLockedValue, params ObjectPath[] objectPaths)
    {
        (_, ObjectPath promptPath) = newLockedValue switch
        {
            false => await _serviceProxy.UnlockAsync(objectPaths),
            true => await _serviceProxy.LockAsync(objectPaths),
        };

        if (promptPath != "/")
        {
            await Utilities.PromptAsync(_connection, promptPath);
        }
    }
}
