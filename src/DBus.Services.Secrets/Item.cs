using System.Threading.Tasks;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace DBus.Services.Secrets;

/// <summary>
/// Represents an item used by the D-Bus secret service.
/// </summary>
public class Item
{
    private OrgFreedesktopSecretItem _itemProxy;

    private Connection _connection;
    private Session _session;

    public ObjectPath ItemPath { get; }

    internal Item(Connection connection, Session session, ObjectPath itemPath)
    {
        _connection = connection;
        _session = session;
        ItemPath = itemPath;

        _itemProxy = new OrgFreedesktopSecretItem(connection, Constants.ServiceName, itemPath);
    }

    /// <summary>
    /// Checks whether this <see cref="Item"/> is currently locked.
    /// </summary>
    /// <returns><see langword="true"/> if this <see cref="Item"/> is currently locked, <see langword="false"/> otherwise.</returns>
    public async Task<bool> IsLockedAsync() => await _itemProxy.GetLockedAsync();

    /// <summary>
    /// Gets the secret associated with this item.
    /// </summary>
    /// <returns>The secret associated with this item.</returns>
    public async Task<byte[]> GetSecretAsync()
    {
        Secret secret = await _itemProxy.GetSecretAsync(_session.SessionPath);

        // TODO: When using DH encryption, other steps are needed to decrypt the secret
        return secret.Value;
    }
}
