using System;
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

    /// <value>
    /// The <see cref="ObjectPath"/> to this <see cref="Collection"/>.
    /// </value>
    public ObjectPath CollectionPath { get; }

    internal Collection(Connection connection, ISession session, ObjectPath collectionPath)
    {
        _connection = connection;
        _session = session;
        CollectionPath = collectionPath;

        _collectionProxy = new OrgFreedesktopSecretCollection(connection, Constants.ServiceName, collectionPath);
    }

    #region D-Bus Properties

    /// <summary>
    /// Gets all <see cref="Item"/>s in this collection.
    /// </summary>
    /// <returns>An array containing all <see cref="Item"/>s in this collection.</returns>
    public async Task<Item[]> GetItemsAsync() =>
        (await _collectionProxy.GetItemsPropertyAsync())
            .Select(itemPath => new Item(_connection, _session, itemPath))
            .ToArray();

    /// <summary>
    /// Gets the displayed label for this collection.
    /// </summary>
    /// <returns>The displayed label for this collection.</returns>
    public async Task<string> GetLabelAsync() => await _collectionProxy.GetLabelPropertyAsync();

    /// <summary>
    /// Sets the displayed label for this collection.
    /// </summary>
    /// <param name="label">The new label to use.</param>
    public async Task SetLabelAsync(string label) => await _collectionProxy.SetLabelPropertyAsync(label);

    /// <summary>
    /// Checks whether this <see cref="Collection"/> is currently locked.
    /// </summary>
    /// <returns><see langword="true"/> if this <see cref="Collection"/> is currently locked, <see langword="false"/> otherwise.</returns>
    public async Task<bool> IsLockedAsync() => await _collectionProxy.GetLockedPropertyAsync();

    /// <summary>
    /// Gets the unix timestamp of when this <see cref="Collection"/> was created.
    /// </summary>
    /// <returns>The unix timestamp of when this <see cref="Collection"/> was created.</returns>
    public async Task<ulong> GetCreatedAsync() => await _collectionProxy.GetCreatedPropertyAsync();

    /// <summary>
    /// Gets the unix timestamp of when this <see cref="Collection"/> was modified.
    /// </summary>
    /// <returns>The unix timestamp of when this <see cref="Collection"/> was modified.</returns>
    public async Task<ulong> GetModifiedAsync() => await _collectionProxy.GetModifiedPropertyAsync();

    #endregion

    #region D-Bus Methods

    /// <summary>
    /// Attempts to lock this <see cref="Collection"/>, prompting the user if necessary.
    /// </summary>
    public async Task LockAsync() => await Utilities.LockOrUnlockAsync(_connection, true, CollectionPath);

    /// <summary>
    /// Attempts to unlock this <see cref="Collection"/>, prompting the user if necessary.
    /// </summary>
    public async Task UnlockAsync() => await Utilities.LockOrUnlockAsync(_connection, false, CollectionPath);

    /// <summary>
    /// Deletes this collection, prompting the user if necessary.
    /// </summary>
    public async Task DeleteAsync()
    {
        ObjectPath promptPath = await _collectionProxy.DeleteAsync();

        if (promptPath != "/")
        {
            await Utilities.PromptAsync(_connection, promptPath);
        }
    }

    /// <summary>
    /// Searches for items in this <see cref="Collection"/> that match the specified lookup attributes.
    /// </summary>
    /// <param name="lookupAttributes">The lookup attributes to use.</param>
    /// <returns>The list of <see cref="Item"/>s that match the specified lookup attributes.</returns>
    public async Task<Item[]> SearchItemsAsync(Dictionary<string, string> lookupAttributes)
    {
        ObjectPath[] matchedItemPaths = await _collectionProxy.SearchItemsAsync(lookupAttributes);

        return matchedItemPaths
            .Select(itemPath => new Item(_connection, _session, itemPath))
            .ToArray();
    }

    /// <summary>
    /// Creates an item in this <see cref="Collection"/>, unlocking it if necessary.
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
            lookupAttributes.Select(kvp => new DBusDictEntryItem(new DBusStringItem(kvp.Key), new DBusStringItem(kvp.Value))).ToArray()
        );

        Dictionary<string, DBusVariantItem> properties = new()
        {
            { Constants.ItemLabelProperty, new("s", new DBusStringItem(label)) },
            { Constants.ItemAttributesProperty, new("a{ss}", lookupAttributesArray) },
        };

        if (await IsLockedAsync())
        {
            await UnlockAsync();
        }

        (ObjectPath newItemPath, ObjectPath promptPath) = await _collectionProxy.CreateItemAsync(properties, secretStruct, replace);
        if (newItemPath == "/")
        {
            (bool dismissed, DBusVariantItem promptResult) = await Utilities.PromptAsync(_connection, promptPath);
            if (dismissed || promptResult.Value is not DBusObjectPathItem promptResultPathItem)
            {
                return null;
            }

            newItemPath = promptResultPathItem.Value;
        }

        return new Item(_connection, _session, newItemPath);
    }

    #endregion
}
