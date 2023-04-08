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

    Secret FormatSecret(byte[] data, string contentType);

    byte[] DecryptSecret(ref Secret secret);
}
