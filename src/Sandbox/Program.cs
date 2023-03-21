using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace Sandbox;

public sealed class Program
{
    private const string ServicePath = "/org/freedesktop/secrets";
    private const string ServiceName = "org.freedesktop.secrets";

    private const string DefaultCollectionAlias = "default";

    public static async Task Main(string[] args)
    {
        // Attempt at replicating my example using the default collection
        Console.WriteLine("Establishing D-Bus session connection...");

        using var connection = new Connection(Address.Session!);
        await connection.ConnectAsync();
        var peerName = connection.UniqueName ?? string.Empty;

        Console.WriteLine($"Connected to D-Bus (peer name: {peerName})");

        Console.WriteLine("Opening new session with 'plain' transport encryption...");

        // Open a session with 'plain' text encryption
        // The input parameter is just an empty string (need to input a variant which is a bit annoying...)
        // The signature in the DBusVariantItem is important - it indicates the type
        // https://dbus.freedesktop.org/doc/dbus-specification.html#type-system
        var serviceProxy = new OrgFreedesktopSecretService(connection, ServiceName, ServicePath);
        (_, var sessionPath) = await serviceProxy.OpenSessionAsync("plain", new DBusVariantItem("s", new DBusStringItem(string.Empty)));

        Console.WriteLine($"Opened session at path '{sessionPath}'");

        Console.WriteLine("Retrieving default collection...");
        var defaultCollectionPath = await serviceProxy.ReadAliasAsync(DefaultCollectionAlias);
        if (defaultCollectionPath == "/")
        {
            Console.WriteLine("Default collection does not exist, exiting");
            return;
        }

        Console.WriteLine($"Retrieved default collection (object path: {defaultCollectionPath})");

        Console.WriteLine("Checking if collection is locked...");

        var defaultCollectionProxy = new OrgFreedesktopSecretCollection(connection, ServiceName, defaultCollectionPath);
        if (await defaultCollectionProxy.GetLockedAsync())
        {
            Console.WriteLine("Collection is locked, unlocking...");

            (_, var unlockPromptPath) = await serviceProxy.UnlockAsync(new[] { defaultCollectionPath });

            if (unlockPromptPath != "/")
            {
                Console.WriteLine("Prompt required to unlock");
                (var dismissed, _) = await PromptAsync(connection, unlockPromptPath);
                if (dismissed)
                {
                    Console.WriteLine("Prompt for unlocking collection dismissed, exiting");
                    return;
                }
            }

            Console.WriteLine("Collection unlocked");
        }
        else
        {
            Console.WriteLine("Collection is already unlocked");
        }

        Console.WriteLine("Creating new secret value...");

        // TODO: Constructing these is a bit more difficult due to stricter types...
        const string itemLabel = "SecretValueLabel";

        var lookupAttributes = new Dictionary<string, string>()
        {
            { "my-lookup-attribute", "my-lookup-attribute-value" }
        };

        // The source generator code is a bit trickier to use
        // It requires knowledge of type signatures to work correctly
        // To add a "dictionary" into a variant type, we have to convert the dictionary into an array of dictionary items
        // Then the type signature would be 'a{<keytype><valuetype>}' - in our case, 'a{ss}' for an array of dictionary items with string keys + values
        var lookupAttributesArray = new DBusArrayItem(
            DBusType.DictEntry,
            lookupAttributes.Select(kvp => new DBusDictEntryItem(new DBusStringItem(kvp.Key), new DBusStringItem(kvp.Value)))
        );

        var algoParams = Array.Empty<byte>();

        const string secretValue = "whoa it's the new secret value";
        var secretBytes = Encoding.UTF8.GetBytes(secretValue);

        const string contentType = "text/plain; charset=utf8";

        var secret = (sessionPath, algoParams, secretBytes, contentType);

        var createItemParams = new Dictionary<string, DBusVariantItem>()
        {
            { "org.freedesktop.Secret.Item.Label", new DBusVariantItem("s", new DBusStringItem(itemLabel)) },
            { "org.freedesktop.Secret.Item.Attributes", new DBusVariantItem("a{ss}", lookupAttributesArray) },
        };

        (var newItemPath, var newItemPromptPath) = await defaultCollectionProxy.CreateItemAsync(createItemParams, secret, true);
        if (newItemPath == "/")
        {
            Console.WriteLine("Prompt required to create new item");

            (var dismissed, _) = await PromptAsync(connection, newItemPromptPath);
            if (dismissed)
            {
                Console.WriteLine("Prompt to create new item dismissed, exiting");
                return;
            }
        }

        Console.WriteLine("Created new secret value");

        Console.WriteLine("Searching for matching secret values...");

        var matchedItemPaths = await defaultCollectionProxy.SearchItemsAsync(lookupAttributes);

        if (matchedItemPaths.Length == 0)
        {
            Console.WriteLine("No matching items");
        }
        else
        {
            foreach (var matchedItemPath in matchedItemPaths)
            {
                Console.WriteLine($"Found matching item at {matchedItemPath}");
                var matchedItemProxy = new OrgFreedesktopSecretItem(connection, ServiceName, matchedItemPath);

                Console.WriteLine("Checking if item is locked...");
                if (await matchedItemProxy.GetLockedAsync())
                {
                    Console.WriteLine("Item is locked, unlocking...");
                    (_, var unlockPromptPath) = await serviceProxy.UnlockAsync(new[] { matchedItemPath });

                    if (unlockPromptPath != "/")
                    {
                        Console.WriteLine("Prompt required to unlock item");
                        (var dismissed, _) = await PromptAsync(connection, unlockPromptPath);
                        if (dismissed)
                        {
                            Console.WriteLine("Prompt for unlocking item dismissed, skipping");
                            continue;
                        }
                    }

                    Console.WriteLine("Item unlocked");
                }

                (_, _, var matchedItemBytes, _) = await matchedItemProxy.GetSecretAsync(sessionPath);
                var matchedItemValue = Encoding.UTF8.GetString(matchedItemBytes);

                Console.WriteLine($"Matched item value: {matchedItemValue}");
            }
        }

        Console.WriteLine("Finished, press any key to exit");
        Console.ReadKey();
    }

    private static async Task<(bool, DBusVariantItem)> PromptAsync(Connection connection, ObjectPath path)
    {
        var tcs = new TaskCompletionSource<(bool, DBusVariantItem)>();
        var promptProxy = new OrgFreedesktopSecretPrompt(connection, ServiceName, path);

        // TODO: I don't know if the subscription will be leaked out... need to be careful about this
        // Looking at Avalonia's source code, it looks like I don't have to do anything?
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

        // TODO: Pass window ID as needed
        await promptProxy.PromptAsync("");

        return await tcs.Task;
    }
}
