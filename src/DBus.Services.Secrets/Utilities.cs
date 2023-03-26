using System.Threading.Tasks;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace DBus.Services.Secrets;

/// <summary>
/// Static class providing various utility methods.
/// </summary>
internal static class Utilities
{
    /// <summary>
    /// Displays a prompt required by the secret service using the specified window handle.
    /// </summary>
    /// <param name="connection">The current <see cref="Connection"/> in use.</param>
    /// <param name="promptPath">The <see cref="ObjectPath"/> of the prompt.</param>
    /// <param name="windowId">The platform-specific window handle for displaying the prompt. Defaults to an empty string.</param>
    /// <returns>The result of the prompt.</returns>
    public static async Task<(bool dismissed, DBusVariantItem result)> PromptAsync(Connection connection, ObjectPath promptPath, string windowId = "")
    {
        TaskCompletionSource<(bool, DBusVariantItem)> tcs = new();
        OrgFreedesktopSecretPrompt promptProxy = new(connection, Constants.ServiceName, promptPath);

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
