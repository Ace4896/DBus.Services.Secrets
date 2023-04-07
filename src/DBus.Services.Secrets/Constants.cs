using Tmds.DBus.SourceGenerator;

namespace DBus.Services.Secrets;

/// <summary>
/// Static class holding various constant values.
/// </summary>
public static class Constants
{
    public const string ServiceName = "org.freedesktop.secrets";
    public const string ServicePath = "/org/freedesktop/secrets";

    public const string SessionAlgorithmPlain = "plain";
    public const string SessionAlgorithmDh = "dh-ietf1024-sha256-aes128-cbc-pkcs7";
    public static readonly DBusVariantItem SessionInputPlain = new("s", new DBusStringItem(string.Empty));

    public const string DefaultCollectionAlias = "default";

    public const string ItemLabelProperty = "org.freedesktop.Secret.Item.Label";
    public const string ItemAttributesProperty = "org.freedesktop.Secret.Item.Attributes";
}
