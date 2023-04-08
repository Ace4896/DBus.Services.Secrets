namespace DBus.Services.Secrets;

/// <summary>
/// Represents the different encryption types for secret service sessions.
/// </summary>
public enum EncryptionType
{
    /// <summary>
    /// Plaintext (i.e. no encryption)
    /// </summary>
    Plain,

    /// <summary>
    /// DH Key Agreement
    /// </summary>
    Dh,
}
