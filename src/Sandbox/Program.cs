using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DBus.Services.Secrets;

namespace Sandbox;

public sealed class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Connecting to D-Bus Secret Service API...");
        SecretService secretService = await SecretService.ConnectAsync(EncryptionType.Dh);
        Console.WriteLine("Connected to D-Bus Secret Service API");

        Console.WriteLine("Available Collections:");

        foreach (Collection collection in await secretService.GetAllCollectionsAsync())
        {
            string label = await collection.GetLabelAsync();
            Item[] items = await collection.GetItemsAsync();
            bool locked = await collection.IsLockedAsync();
            DateTimeOffset created = await collection.GetCreatedAsync();
            DateTimeOffset modified = await collection.GetModifiedAsync();

            Console.WriteLine($"'{label}' Collection");
            Console.WriteLine($"- Path: {collection.CollectionPath}");
            Console.WriteLine($"- Total Items: {items.Length}");

            foreach (Item item in items)
            {
                Console.WriteLine($"  - {item.ItemPath}");
            }

            Console.WriteLine($"- Locked? {locked}");
            Console.WriteLine($"- Created on {created}");
            Console.WriteLine($"- Last modified on {modified}");
            Console.WriteLine();
        }

        Console.WriteLine("Retrieving default collection...");
        Collection? defaultCollection = await secretService.GetDefaultCollectionAsync();

        if (defaultCollection == null)
        {
            Console.WriteLine("Could not retrieve default collection, exiting");
            return;
        }

        Console.WriteLine("Retrieved default collection");

        CollectionProperties defaultCollectionProperties = await defaultCollection.GetAllPropertiesAsync();
        Console.WriteLine($"- Label: {defaultCollectionProperties.Label}");
        Console.WriteLine($"- Path: {defaultCollection.CollectionPath}");
        Console.WriteLine($"- Total Items: {defaultCollectionProperties.Items.Length}");

        foreach (Item item in defaultCollectionProperties.Items)
        {
            Console.WriteLine($"  - {item.ItemPath}");
        }

        Console.WriteLine($"- Locked? {defaultCollectionProperties.Locked}");
        Console.WriteLine($"- Created on {defaultCollectionProperties.Created}");
        Console.WriteLine($"- Last modified on {defaultCollectionProperties.Modified}");

        Console.WriteLine();
        Console.WriteLine("Creating new secret value...");

        const string secretValueLabel = "SecretValueLabel";

        Dictionary<string, string> lookupAttributes = new()
        {
            { "my-lookup-attribute", "my-lookup-attribute-value" }
        };

        byte[] secretBytes = Encoding.UTF8.GetBytes("whoa it's the updated secret value");
        const string contentType = "text/plain; charset=utf8";

        Item? createdItem = await defaultCollection.CreateItemAsync(secretValueLabel, lookupAttributes, secretBytes, contentType, true);
        if (createdItem is null)
        {
            Console.WriteLine("Could not create item");
        }
        else
        {
            Console.WriteLine($"Created new item at {createdItem.ItemPath}");
        }

        Console.WriteLine("Searching for items...");

        Item[] matchedItems = await defaultCollection.SearchItemsAsync(lookupAttributes);

        if (matchedItems.Length == 0)
        {
            Console.WriteLine("Could not find any matching items");
        }
        else
        {
            foreach (Item item in matchedItems)
            {
                Console.WriteLine($"Found item at {item.ItemPath}");

                string label = await item.GetLabelAsync();
                bool locked = await item.IsLockedAsync();
                DateTimeOffset created = await item.GetCreatedAsync();
                DateTimeOffset modified = await item.GetModifiedAsync();
                Dictionary<string, string> attributes = await item.GetLookupAttributesAsync();

                byte[] secret = await item.GetSecretAsync();
                string secretString = Encoding.UTF8.GetString(secret);

                Console.WriteLine($"- Label: {label}");
                Console.WriteLine($"- Locked? {locked}");
                Console.WriteLine($"- Created on {created.ToLocalTime()}");
                Console.WriteLine($"- Last modified on {modified.ToLocalTime()}");
                Console.WriteLine($"- Lookup Attributes: {attributes.Count}");

                foreach (KeyValuePair<string, string> attribute in attributes)
                {
                    Console.WriteLine($"  - {attribute.Key}: {attribute.Value}");
                }

                Console.WriteLine($"- Decoded Secret Value: {secretString}");
            }
        }

        Console.WriteLine("Finished; press any key to exit");
        Console.ReadKey();
    }
}
