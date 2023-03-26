namespace DBus.Services.Secrets;

/// <summary>
/// Static class holding various constant values.
/// </summary>
public static class Constants
{
    public const string ServiceName = "org.freedesktop.secrets";
    public const string ServicePath = "/org/freedesktop/secrets";

    public const string DefaultCollectionAlias = "default";

    public const string ItemLabelProperty = "org.freedesktop.Secret.Item.Label";
    public const string ItemAttributesProperty = "org.freedesktop.Secret.Item.Attributes";
}
