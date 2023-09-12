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
            CollectionProperties properties = await collection.GetAllPropertiesAsync();

            Console.WriteLine($"'{properties.Label}' Collection");
            Console.WriteLine($"- Path: {collection.CollectionPath}");
            Console.WriteLine($"- Total Items: {properties.Items.Length}");
            Console.WriteLine($"- Locked? {properties.Locked}");
            Console.WriteLine($"- Created on {properties.Created}");
            Console.WriteLine($"- Last modified on {properties.Modified}");
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
        Console.WriteLine("Creating new secret value...");

        const string label = "SecretValueLabel";

        Dictionary<string, string> lookupAttributes = new()
        {
            { "my-lookup-attribute", "my-lookup-attribute-value" }
        };

        byte[] secretBytes = Encoding.UTF8.GetBytes("whoa it's the updated secret value");
        const string contentType = "text/plain; charset=utf8";

        Item? createdItem = await defaultCollection.CreateItemAsync(label, lookupAttributes, secretBytes, contentType, true);
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

                // TODO: Reading all properties seem to fail...

                // ItemProperties properties = await item.GetAllPropertiesAsync();
                byte[] secret = await item.GetSecretAsync();
                string secretString = Encoding.UTF8.GetString(secret);

                // Console.WriteLine($"- Label: {properties.Label}");
                // Console.WriteLine($"- Locked? {properties.Locked}");
                // Console.WriteLine($"- Created on {properties.Created.ToLocalTime()}");
                // Console.WriteLine($"- Last modified on {properties.Modified.ToLocalTime()}");
                // Console.WriteLine($"- Lookup Attributes: {properties.Attributes.Count}");

                // foreach (KeyValuePair<string, string> lookupAttribute in properties.Attributes)
                // {
                //     Console.WriteLine($"  - {lookupAttribute.Key}: {lookupAttribute.Value}");
                // }

                Console.WriteLine($"Decoded Secret Value: {secretString}");
            }
        }

        Console.WriteLine("Finished; press any key to exit");
        Console.ReadKey();
    }
}
