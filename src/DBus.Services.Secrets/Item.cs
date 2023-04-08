using System.Collections.Generic;
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

    /// <value>
    /// The <see cref="ObjectPath"/> to this <see cref="Item"/>.
    /// </value>
    public ObjectPath ItemPath { get; }

    internal Item(Connection connection, ISession session, ObjectPath itemPath)
    {
        _connection = connection;
        _session = session;
        ItemPath = itemPath;

        _itemProxy = new OrgFreedesktopSecretItem(connection, Constants.ServiceName, itemPath);
    }

    #region D-Bus Properties

    /// <summary>
    /// Checks whether this <see cref="Item"/> is currently locked.
    /// </summary>
    /// <returns><see langword="true"/> if this <see cref="Item"/> is currently locked, <see langword="false"/> otherwise.</returns>
    public async Task<bool> IsLockedAsync() => await _itemProxy.GetLockedAsync();

    /// <summary>
    /// Gets the lookup attributes associated with this <see cref="Item"/>.
    /// </summary>
    /// <returns>The lookup attributes associated with this <see cref="Item"/>.</returns>
    public async Task<Dictionary<string, string>> GetLookupAttributesAsync() => await _itemProxy.GetAttributesAsync();

    /// <summary>
    /// Sets the lookup attributes associated with this <see cref="Item"/>.
    /// </summary>
    /// <param name="lookupAttributes">The new lookup attributes associated with this <see cref="Item"/>.</param>
    public async Task SetLookupAttributesAsync(Dictionary<string, string> lookupAttributes) => await _itemProxy.SetAttributesAsync(lookupAttributes);

    /// <summary>
    /// Gets the displayed label for this <see cref="Item"/>.
    /// </summary>
    /// <returns>The displayed label for this <see cref="Item"/>.</returns>
    public async Task<string> GetLabelAsync() => await _itemProxy.GetLabelAsync();

    /// <summary>
    /// Sets the displayed label for this <see cref="Item"/>.
    /// </summary>
    /// <param name="label">The new displayed label for this <see cref="Item"/>.</param>
    public async Task SetLabelAsync(string label) => await _itemProxy.SetLabelAsync(label);

    /// <summary>
    /// Gets the unix timestamp of when this <see cref="Item"/> was created.
    /// </summary>
    /// <returns>The unix timestamp of when this <see cref="Item"/> was created.</returns>
    public async Task<ulong> GetCreatedAsync() => await _itemProxy.GetCreatedAsync();

    /// <summary>
    /// Gets the unix timestamp of when this <see cref="Item"/> was modified.
    /// </summary>
    /// <returns>The unix timestamp of when this <see cref="Item"/> was modified.</returns>
    public async Task<ulong> GetModifiedAsync() => await _itemProxy.GetModifiedAsync();

    #endregion

    #region D-Bus Methods

    /// <summary>
    /// Locks this <see cref="Item"/>, prompting the user if necessary.
    /// </summary>
    public async Task LockAsync() => await Utilities.LockOrUnlockAsync(_connection, true, ItemPath);

    /// <summary>
    /// Unlocks this <see cref="Item"/>, prompting the user if necessary.
    /// </summary>
    public async Task UnlockAsync() => await Utilities.LockOrUnlockAsync(_connection, false, ItemPath);

    /// <summary>
    /// Deletes this <see cref="Item"/>, prompting the user if necessary.
    /// </summary>
    public async Task DeleteAsync()
    {
        ObjectPath promptPath = await _itemProxy.DeleteAsync();

        if (promptPath != "/")
        {
            await Utilities.PromptAsync(_connection, promptPath);
        }
    }

    /// <summary>
    /// Gets the secret associated with this <see cref="Item"/>, unlocking it if necessary.
    /// </summary>
    /// <returns>The secret associated with this item.</returns>
    public async Task<byte[]> GetSecretAsync()
    {
        if (await IsLockedAsync())
        {
            await UnlockAsync();
        }

        Secret secret = await _itemProxy.GetSecretAsync(_session.SessionPath);
        return _session.DecryptSecret(ref secret);
    }

    /// <summary>
    /// Sets the secret associated with this <see cref="Item"/>, unlocking it if necessary.
    /// </summary>
    /// <param name="secret">The new secret value associated with this item.</param>
    /// <param name="contentType">The content type of the secret value.</param>
    public async Task SetSecret(byte[] secret, string contentType)
    {
        Secret formattedSecret = _session.FormatSecret(secret, contentType);

        if (await IsLockedAsync())
        {
            await UnlockAsync();
        }

        await _itemProxy.SetSecretAsync(formattedSecret);
    }

    #endregion
}
