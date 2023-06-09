﻿using System;
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
                Console.WriteLine($"Found item at object path {item.ItemPath}");
                byte[] secret = await item.GetSecretAsync();
                string secretString = Encoding.UTF8.GetString(secret);
                Console.WriteLine($"Secret Value: {secretString}");
            }
        }

        Console.WriteLine("Finished; press any key to exit");
        Console.ReadKey();
    }
}
