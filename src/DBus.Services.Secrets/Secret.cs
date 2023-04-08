using Tmds.DBus.Protocol;

namespace DBus.Services.Secrets;

/// <summary>
/// Wrapper for a secret value used in the D-Bus secret service.
/// </summary>
public struct Secret
{
    /// <summary>
    /// The <see cref="ObjectPath"/> to this <see cref="Collection"/>.
    /// </summary>
    public ObjectPath SessionPath { get; set; }

    /// <summary>
    /// The parameters used to encrypt this <see cref="Secret"/>.
    /// </summary>
    public byte[] Parameters { get; set; }

    /// <summary>
    /// The (possibly encrypted) secret value.
    /// </summary>
    public byte[] Value { get; set; }

    /// <summary>
    /// The content type of the secret value.
    /// </summary>
    public string ContentType { get; set; }

    /// <summary>
    /// Converts a (session path, parameters, value, content type) tuple into a <see cref="Secret"/>.
    /// </summary>
    /// <param name="values">The tuple to convert.</param>
    public static implicit operator Secret((ObjectPath, byte[], byte[], string) values) => new()
    {
        SessionPath = values.Item1,
        Parameters = values.Item2,
        Value = values.Item3,
        ContentType = values.Item4,
    };

    /// <summary>
    /// Converts a <see cref="Secret"/> into a (session path, parameters, value, content type) tuple.
    /// </summary>
    /// <param name="secret">The <see cref="Secret"/> to convert.</param>
    public static implicit operator (ObjectPath, byte[], byte[], string)(Secret secret) =>
    (
        secret.SessionPath,
        secret.Parameters,
        secret.Value,
        secret.ContentType
    );
}
