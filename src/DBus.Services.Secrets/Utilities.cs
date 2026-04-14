using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace DBus.Services.Secrets;

/// <summary>
/// Static class providing various utility methods.
/// </summary>
internal static class Utilities
{
    /// <summary>
    /// Locks or unlocks the specified object paths, prompting the user where necessary.
    /// </summary>
    /// <param name="connection">The current <see cref="DBusConnection"/>.</param>
    /// <param name="newLockedValue">Whether the items should be locked or unlocked.</param>
    /// <param name="objectPaths">The <see cref="ObjectPath"/>s to lock or unlock.</param>
    public static async Task LockOrUnlockAsync(DBusConnection connection, bool newLockedValue, params ObjectPath[] objectPaths)
    {
        Generated.Service serviceProxy = new(connection, Constants.ServiceName, Constants.ServicePath);

        (_, ObjectPath promptPath) = newLockedValue switch
        {
            false => await serviceProxy.UnlockAsync(objectPaths),
            true => await serviceProxy.LockAsync(objectPaths),
        };

        if (promptPath != "/")
        {
            await Utilities.PromptAsync(connection, promptPath);
        }
    }

    /// <summary>
    /// Displays a prompt required by the secret service using the specified window handle.
    /// </summary>
    /// <param name="connection">The current <see cref="DBusConnection"/> in use.</param>
    /// <param name="promptPath">The <see cref="ObjectPath"/> of the prompt.</param>
    /// <param name="windowId">The platform-specific window handle for displaying the prompt. Defaults to an empty string.</param>
    /// <returns>The result of the prompt.</returns>
    public static async Task<(bool dismissed, VariantValue result)> PromptAsync(DBusConnection connection, ObjectPath promptPath, string windowId = "")
    {
        TaskCompletionSource<(bool, VariantValue)> tcs = new();
        Generated.Prompt promptProxy = new(connection, Constants.ServiceName, promptPath);

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

        await promptProxy.PromptAsync(windowId);

        return await tcs.Task;
    }
}
