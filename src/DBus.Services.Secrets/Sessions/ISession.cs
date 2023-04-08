using Tmds.DBus.Protocol;

namespace DBus.Services.Secrets.Sessions;

/// <summary>
/// Interface for classes providing wrapper APIs around sessions.
/// </summary>
public interface ISession
{
    /// <summary>
    /// The <see cref="ObjectPath"/> to this session.
    /// </summary>
    ObjectPath SessionPath { get; }

    /// <summary>
    /// Formats the provided data into a <see cref="Secret"/>, which may involve encrypting it with the session's data.
    /// </summary>
    /// <param name="data">The data to format as a secret.</param>
    /// <param name="contentType">The content type of the data.</param>
    /// <returns>The formatted <see cref="Secret"/>.</returns>
    Secret FormatSecret(byte[] data, string contentType);

    /// <summary>
    /// Decrypts the provided secret using the session's data.
    /// </summary>
    /// <param name="secret">The secret with the data to decrypt.</param>
    /// <returns>The decrypted secret.</returns>
    byte[] DecryptSecret(ref Secret secret);
}
