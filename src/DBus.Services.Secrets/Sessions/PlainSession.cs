using System;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace DBus.Services.Secrets.Sessions;

/// <summary>
/// <see cref="ISession"/> implementation for <see cref="EncryptionType.Plain"/>.
/// </summary>
internal sealed class PlainSession : ISession
{
    public ObjectPath SessionPath { get; }

    internal PlainSession(ObjectPath sessionPath)
    {
        SessionPath = sessionPath;
    }

    internal static async Task<PlainSession> OpenPlainSessionAsync(Connection connection)
    {
        OrgFreedesktopSecretService serviceProxy = new(connection, Constants.ServiceName, Constants.ServicePath);
        (_, ObjectPath sessionPath) = await serviceProxy.OpenSessionAsync(Constants.SessionAlgorithmPlain, Constants.SessionInputPlain);

        return new PlainSession(sessionPath);
    }

    public Secret FormatSecret(byte[] data, string contentType) => new()
    {
        SessionPath = SessionPath,
        Parameters = Array.Empty<byte>(),
        Value = data,
        ContentType = contentType,
    };

    public byte[] DecryptSecret(ref Secret secret) => secret.Value;
}
