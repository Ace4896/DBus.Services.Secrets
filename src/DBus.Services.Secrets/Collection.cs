using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace DBus.Services.Secrets;

/// <summary>
/// Represents a collection of secret items.
/// </summary>
public class Collection
{
    private const string ServiceName = "org.freedesktop.secrets";

    private OrgFreedesktopSecretCollection _collectionProxy;

    private Connection _connection;
    private ObjectPath _sessionPath;

    public ObjectPath CollectionPath { get; }

    internal Collection(Connection connection, OrgFreedesktopSecretService serviceProxy, ObjectPath sessionPath, ObjectPath collectionPath)
    {
        _connection = connection;
        _sessionPath = sessionPath;
        CollectionPath = collectionPath;

        _collectionProxy = new OrgFreedesktopSecretCollection(connection, ServiceName, collectionPath);
    }

    /// <summary>
    /// Checks whether this <see cref="Collection"/> is currently locked.
    /// </summary>
    /// <returns><see langword="true"/> if this <see cref="Collection"/> is currently locked, <see langword="false"/> otherwise.</returns>
    public async Task<bool> IsLockedAsync() => await _collectionProxy.GetLockedAsync();

    /// <summary>
    /// Searches for items in this <see cref="Collection"/> that match the specified lookup attributes.
    /// </summary>
    /// <param name="lookupAttributes">The lookup attributes to use.</param>
    /// <returns>The list of <see cref="Item"/>s that match the specified lookup attributes.</returns>
    public async Task<List<Item>> SearchItemsAsync(Dictionary<string, string> lookupAttributes)
    {
        ObjectPath[] matchedItemPaths = await _collectionProxy.SearchItemsAsync(lookupAttributes);

        return matchedItemPaths
            .Select(itemPath => new Item(_connection, _sessionPath, itemPath))
            .ToList();
    }
}
