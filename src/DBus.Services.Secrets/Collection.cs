using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DBus.Services.Secrets.Sessions;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace DBus.Services.Secrets;

/// <summary>
/// Represents a collection of secret items.
/// </summary>
public sealed class Collection
{
    private OrgFreedesktopSecretCollection _collectionProxy;

    private Connection _connection;
    private ISession _session;

    public ObjectPath CollectionPath { get; }

    internal Collection(Connection connection, ISession session, ObjectPath collectionPath)
    {
        _connection = connection;
        _session = session;
        CollectionPath = collectionPath;

        _collectionProxy = new OrgFreedesktopSecretCollection(connection, Constants.ServiceName, collectionPath);
    }

    /// <summary>
    /// Checks whether this <see cref="Collection"/> is currently locked.
    /// </summary>
    /// <returns><see langword="true"/> if this <see cref="Collection"/> is currently locked, <see langword="false"/> otherwise.</returns>
    public async Task<bool> IsLockedAsync() => await _collectionProxy.GetLockedAsync();

    /// <summary>
    /// Creates an item in this collection.
    /// </summary>
    /// <param name="label">The label for the new item.</param>
    /// <param name="lookupAttributes">The lookup attributes to associate with the new item.</param>
    /// <param name="secret">The secret value to store.</param>
    /// <param name="contentType">The content type of the secret value.</param>
    /// <param name="replace">Whether to replace an existing item with the same lookup attributes.</param>
    /// <returns>The created <see cref="Item"/>, or <see langword="null"/> if it could not be created (e.g. prompt was dismissed).</returns>
    public async Task<Item?> CreateItemAsync(string label, Dictionary<string, string> lookupAttributes, byte[] secret, string contentType, bool replace)
    {
        Secret secretStruct = _session.FormatSecret(secret, contentType);

        DBusArrayItem lookupAttributesArray = new(
            DBusType.DictEntry,
            lookupAttributes.Select(kvp => new DBusDictEntryItem(new DBusStringItem(kvp.Key), new DBusStringItem(kvp.Value)))
        );

        Dictionary<string, DBusVariantItem> properties = new()
        {
            { Constants.ItemLabelProperty, new("s", new DBusStringItem(label)) },
            { Constants.ItemAttributesProperty, new("a{ss}", lookupAttributesArray) },
        };

        (ObjectPath newItemPath, ObjectPath promptPath) = await _collectionProxy.CreateItemAsync(properties, secretStruct, replace);
        if (newItemPath == "/")
        {
            (bool dismissed, DBusVariantItem promptResult) = await Utilities.PromptAsync(_connection, promptPath);
            if (dismissed)
            {
                return null;
            }

            if (promptResult.Value is not DBusObjectPathItem promptResultPathItem)
            {
                return null;
            }

            newItemPath = promptResultPathItem.Value;
        }

        return new Item(_connection, _session, newItemPath);
    }

    /// <summary>
    /// Searches for items in this <see cref="Collection"/> that match the specified lookup attributes.
    /// </summary>
    /// <param name="lookupAttributes">The lookup attributes to use.</param>
    /// <returns>The list of <see cref="Item"/>s that match the specified lookup attributes.</returns>
    public async Task<List<Item>> SearchItemsAsync(Dictionary<string, string> lookupAttributes)
    {
        ObjectPath[] matchedItemPaths = await _collectionProxy.SearchItemsAsync(lookupAttributes);

        return matchedItemPaths
            .Select(itemPath => new Item(_connection, _session, itemPath))
            .ToList();
    }
}
