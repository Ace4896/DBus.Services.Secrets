using System.Threading.Tasks;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace DBus.Services.Secrets;

public class SecretService
{
    private const string ServiceName = "org.freedesktop.secrets";
    private const string ServicePath = "/org/freedesktop/secrets";

    private const string DefaultCollectionAlias = "default";

    private OrgFreedesktopSecretService _serviceProxy;

    private Connection _connection;
    private Session _session;

    internal SecretService(Connection connection, Session session)
    {
        _connection = connection;
        _session = session;

        _serviceProxy = new OrgFreedesktopSecretService(connection, ServiceName, ServicePath);
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

        OrgFreedesktopSecretService serviceProxy = new(connection, ServiceName, ServicePath);
        
        // TODO: AES IV needs to go in session, not sure where it goes yet
        (string algorithm, DBusVariantItem input) = GetSessionParameters(encryptionType);
        (_, ObjectPath sessionPath) = await serviceProxy.OpenSessionAsync(algorithm, input);
        Session session = new(sessionPath);

        return new SecretService(connection, session);
    }

    // TODO: DH Encryption Type
    private static (string algorithm, DBusVariantItem input) GetSessionParameters(EncryptionType encryptionType)
    {
        const string algorithm = "plain";
        DBusVariantItem input = new("s", new DBusStringItem(string.Empty));

        return (algorithm, input);
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
    public Task<Collection?> GetDefaultCollectionAsync() => GetCollectionByAliasAsync(DefaultCollectionAlias);

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
            await PromptAsync(promptPath);
        }
    }

    private async Task<(bool dismissed, DBusVariantItem result)> PromptAsync(ObjectPath path)
    {
        TaskCompletionSource<(bool, DBusVariantItem)> tcs = new();
        OrgFreedesktopSecretPrompt promptProxy = new(_connection, ServiceName, path);

        await promptProxy.WatchCompletedAsync(
            (exception, result) =>
            {
                if (exception != null)
                {
                    tcs.TrySetException(exception);
                }
                else
                {
                    tcs.TrySetResult(result);
                }
            }
        );

        await promptProxy.PromptAsync(string.Empty);

        return await tcs.Task;
    }
}
