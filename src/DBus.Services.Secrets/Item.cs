using System.Threading.Tasks;
using DBus.Services.Secrets.Sessions;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace DBus.Services.Secrets;

/// <summary>
/// Represents an item used by the D-Bus secret service.
/// </summary>
public sealed class Item
{
    private OrgFreedesktopSecretItem _itemProxy;

    private Connection _connection;
    private ISession _session;

    public ObjectPath ItemPath { get; }

    internal Item(Connection connection, ISession session, ObjectPath itemPath)
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
        return _session.DecryptSecret(ref secret);
    }
}
