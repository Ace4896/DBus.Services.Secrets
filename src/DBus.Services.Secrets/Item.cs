using System.Threading.Tasks;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace DBus.Services.Secrets;

/// <summary>
/// Represents an item used by the D-Bus secret service.
/// </summary>
public class Item
{
    private const string ServiceName = "org.freedesktop.secrets";

    private OrgFreedesktopSecretItem _itemProxy;

    private Connection _connection;
    private ObjectPath _sessionPath;

    public ObjectPath ItemPath { get; }

    internal Item(Connection connection, ObjectPath sessionPath, ObjectPath itemPath)
    {
        _connection = connection;
        _sessionPath = sessionPath;
        ItemPath = itemPath;

        _itemProxy = new OrgFreedesktopSecretItem(connection, ServiceName, itemPath);
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
        Secret secret = await _itemProxy.GetSecretAsync(_sessionPath);

        // TODO: When using DH encryption, other steps are needed to decrypt the secret
        return secret.Value;
    }
}
