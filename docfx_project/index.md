# DBus.Services.Secrets

High-level C# bindings for the [D-Bus Secret Service API](https://specifications.freedesktop.org/secret-service/latest/).

## Basic Usage

```csharp
// Connect to the D-Bus Secret Service API
// Sessions can use either plaintext or encrypted transport
SecretService secretService = await SecretService.ConnectAsync(EncryptionType.Dh);  // DH Key Agreement for Encryption

// Items are stored in within collections
// Collections can be retrieved using their alias
// Note that collection retrieval can fail, so this would need to be handled
Collection? defaultCollection = await secretService.GetDefaultCollectionAsync();
if (defaultCollection == null)
{
    // ... handle case where collection is not found
}

// Items are created with the following:
// - Label - The displayed label in e.g. GNOME Keyring, KWallet etc.
// - Lookup Attributes - These are used to search for the item later
// - Secret - The secret value as a byte array
// - Content Type - A content type hint for the secret value
string itemLabel = "MySecretValue";
Dictionary<string, string> lookupAttributes = new()
{
    { "my-lookup-attribute", "my-lookup-attribute-value" }
};

byte[] secretValue = Encoding.UTF8.GetBytes("my secret value");
string contentType = "text/plain; charset=utf8";

// Note that item creation can fail, e.g. if the collection could not be unlocked
Item? createdItem = await defaultCollection.CreateItemAsync(label, lookupAttributes, secretBytes, contentType, true);
if (createdItem == null)
{
    // ... handle case where item creation failed
}

// Later, if we want to retrieve this secret value, we need to search using the same lookup attributes
// Note that it's possible for multiple items to match the provided lookup attributes
Item[] matchedItems = await defaultCollection.SearchItemsAsync(lookupAttributes);
foreach (Item matchedItem in matchedItems)
{
    byte[] matchedSecret = await item.GetSecretAsync();
    string matchedSecretString = Encoding.UTF8.GetString(secret);   // my secret value
}
```
