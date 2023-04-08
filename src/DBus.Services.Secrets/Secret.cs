using Tmds.DBus.Protocol;

namespace DBus.Services.Secrets;

/// <summary>
/// Wrapper for a secret value used in the D-Bus secret service.
/// </summary>
public struct Secret
{
    public ObjectPath SessionPath { get; set; }
    public byte[] Parameters { get; set; }
    public byte[] Value { get; set; }
    public string ContentType { get; set; }

    public static implicit operator Secret((ObjectPath, byte[], byte[], string) values) => new()
    {
        SessionPath = values.Item1,
        Parameters = values.Item2,
        Value = values.Item3,
        ContentType = values.Item4,
    };

    public static implicit operator (ObjectPath, byte[], byte[], string)(Secret secret) =>
    (
        secret.SessionPath,
        secret.Parameters,
        secret.Value,
        secret.ContentType
    );
}
