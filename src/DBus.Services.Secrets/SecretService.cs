using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DBus.Services.Secrets.Sessions;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace DBus.Services.Secrets;

/// <summary>
/// Top-level wrapper over the D-Bus Secret Service API.
/// </summary>
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

    #region D-Bus Properties

    /// <summary>
    /// Gets all <see cref="Collection"/>s.
    /// </summary>
    /// <returns>An array containing all <see cref="Collection"/>s.</returns>
    public async Task<Collection[]> GetAllCollectionsAsync() =>
        (await _serviceProxy.GetCollectionsAsync())
            .Select(c => new Collection(_connection, _session, c))
            .ToArray();

    #endregion

    #region D-Bus Methods

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
    /// Creates a new <see cref="Collection"/> with the specified label and alias.
    /// </summary>
    /// <param name="label">The label for the new <see cref="Collection"/>.</param>
    /// <param name="alias">The alias for the new <see cref="Collection"/>.</param>
    /// <returns>The created <see cref="Collection"/>, or <see langword="null"/> if it could not be created (e.g. prompt was dismissed).</returns>
    public async Task<Collection?> CreateCollectionAsync(string label, string alias)
    {
        Dictionary<string, DBusVariantItem> properties = new()
        {
            { Constants.CollectionLabelProperty, new("s", new DBusStringItem(label)) },
        };

        (ObjectPath collectionPath, ObjectPath promptPath) = await _serviceProxy.CreateCollectionAsync(properties, alias);

        if (collectionPath == "/")
        {
            (bool dismissed, DBusVariantItem promptResult) = await Utilities.PromptAsync(_connection, promptPath);
            if (dismissed || promptResult.Value is not DBusObjectPathItem promptResultPathItem)
            {
                return null;
            }

            collectionPath = promptResultPathItem.Value;
        }

        return new Collection(_connection, _session, collectionPath);
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
    /// Searches for all items that match the specified lookup attributes.
    /// </summary>
    /// <param name="lookupAttributes">The lookup attributes to use.</param>
    /// <returns>Two arrays containing unlocked items and locked items.</returns>
    public async Task<(Item[] unlocked, Item[] locked)> SearchItemsAsync(Dictionary<string, string> lookupAttributes)
    {
        (ObjectPath[] unlocked, ObjectPath[] locked) = await _serviceProxy.SearchItemsAsync(lookupAttributes);
        Item[] unlockedItems = unlocked.Select(i => new Item(_connection, _session, i)).ToArray();
        Item[] lockedItems = locked.Select(i => new Item(_connection, _session, i)).ToArray();

        return (unlockedItems, lockedItems);
    }

    #endregion
}
