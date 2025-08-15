using Tmds.DBus.Protocol;

namespace DBus.Services.Secrets;

/// <summary>
/// Static class holding various constant values.
/// </summary>
internal static class Constants
{
    public const string ServiceName = "org.freedesktop.secrets";
    public const string ServicePath = "/org/freedesktop/secrets";

    public const string SessionAlgorithmPlain = "plain";
    public const string SessionAlgorithmDh = "dh-ietf1024-sha256-aes128-cbc-pkcs7";
    public static readonly VariantValue SessionInputPlain = VariantValue.String(string.Empty);

    public const string CollectionLabelProperty = "org.freedesktop.Secret.Collection.Label";
    public const string DefaultCollectionAlias = "default";

    public const string ItemLabelProperty = "org.freedesktop.Secret.Item.Label";
    public const string ItemAttributesProperty = "org.freedesktop.Secret.Item.Attributes";
}
