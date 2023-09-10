using System;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace DBus.Services.Secrets.Sessions;

/// <summary>
/// <see cref="ISession"/> implementation for <see cref="EncryptionType.Dh"/>.
/// </summary>
internal sealed class DhSession : ISession
{
    private readonly byte[] _aesKey;

    public ObjectPath SessionPath { get; }

    internal DhSession(ObjectPath sessionPath, byte[] aesKey)
    {
        SessionPath = sessionPath;
        _aesKey = aesKey;
    }

    internal static async Task<DhSession> OpenDhSessionAsync(Connection connection)
    {
        OrgFreedesktopSecretService serviceProxy = new OrgFreedesktopSecretService(connection, Constants.ServiceName, Constants.ServicePath);

        // Input for a DH encrypted session is our DH public key
        // Output from OpenSession call is service's DH public key
        DhKeypair dhKeypair = new();
        byte[] clientPublicKeyBytes = dhKeypair.PublicKey.ToByteArray(true, true);  // Unsigned, big endian

        DBusVariantItem sessionInput = new("ay", new DBusByteArrayItem(clientPublicKeyBytes));
        (DBusVariantItem sessionOutput, ObjectPath sessionPath) = await serviceProxy.OpenSessionAsync(Constants.SessionAlgorithmDh, sessionInput);

        if (sessionOutput.Value is not DBusArrayItem { ArrayType: DBusType.Byte } sessionOutputArray)
        {
            throw new InvalidCastException("Could not retrieve server DH public key bytes");
        }

        byte[] serverPublicKeyBytes = sessionOutputArray
            .Cast<DBusByteItem>()
            .Select(b => b.Value)
            .ToArray();

        byte[] aesKey = dhKeypair.DeriveSharedSecret(serverPublicKeyBytes);

        return new DhSession(sessionPath, aesKey);
    }

    public Secret FormatSecret(byte[] data, string contentType)
    {
        // DH encrypted sessions use AES in CBC mode with PKCS7 padding
        // Generated AES IV should be 16 bytes
        byte[] aesIv = RandomNumberGenerator.GetBytes(16);

        Aes aes = Aes.Create();
        aes.Key = _aesKey;

        byte[] encryptedSecret = aes.EncryptCbc(data, aesIv, PaddingMode.PKCS7);

        return new Secret
        {
            SessionPath = SessionPath,
            Parameters = aesIv,
            Value = encryptedSecret,
            ContentType = contentType,
        };
    }

    public byte[] DecryptSecret(ref Secret secret)
    {
        // Secret service uses AES in CBC mode with PKCS7 padding
        // Parameters should be AES IV
        Aes aes = Aes.Create();
        aes.Key = _aesKey;

        return aes.DecryptCbc(secret.Value, secret.Parameters, PaddingMode.PKCS7);
    }
}

/// <summary>
/// Represents a DH public-private keypair for use with encrypted sessions.
/// </summary>
internal class DhKeypair
{
    private static readonly BigInteger Two = new(2u);

    private static readonly BigInteger DhPrime = new(new byte[]
    {
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xC9, 0x0F, 0xDA, 0xA2, 0x21, 0x68, 0xC2, 0x34,
        0xC4, 0xC6, 0x62, 0x8B, 0x80, 0xDC, 0x1C, 0xD1, 0x29, 0x02, 0x4E, 0x08, 0x8A, 0x67, 0xCC, 0x74,
        0x02, 0x0B, 0xBE, 0xA6, 0x3B, 0x13, 0x9B, 0x22, 0x51, 0x4A, 0x08, 0x79, 0x8E, 0x34, 0x04, 0xDD,
        0xEF, 0x95, 0x19, 0xB3, 0xCD, 0x3A, 0x43, 0x1B, 0x30, 0x2B, 0x0A, 0x6D, 0xF2, 0x5F, 0x14, 0x37,
        0x4F, 0xE1, 0x35, 0x6D, 0x6D, 0x51, 0xC2, 0x45, 0xE4, 0x85, 0xB5, 0x76, 0x62, 0x5E, 0x7E, 0xC6,
        0xF4, 0x4C, 0x42, 0xE9, 0xA6, 0x37, 0xED, 0x6B, 0x0B, 0xFF, 0x5C, 0xB6, 0xF4, 0x06, 0xB7, 0xED,
        0xEE, 0x38, 0x6B, 0xFB, 0x5A, 0x89, 0x9F, 0xA5, 0xAE, 0x9F, 0x24, 0x11, 0x7C, 0x4B, 0x1F, 0xE6,
        0x49, 0x28, 0x66, 0x51, 0xEC, 0xE6, 0x53, 0x81, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
    }, true, true);

    public BigInteger PublicKey { get; }
    public BigInteger PrivateKey { get; }

    public DhKeypair()
    {
        // 128 byte unsigned integer, big endian
        PrivateKey = new BigInteger(RandomNumberGenerator.GetBytes(128), true, true);

        // (2 ^ private key) % DH prime
        PublicKey = BigInteger.ModPow(Two, PrivateKey, DhPrime);
    }

    public byte[] DeriveSharedSecret(byte[] serverPublicKeyBytes)
    {
        // Server public key should be in big endian
        BigInteger serverPublicKey = new(serverPublicKeyBytes, true, true);
        BigInteger sharedSecret = BigInteger.ModPow(serverPublicKey, PrivateKey, DhPrime);

        // Setup input key material for HKDF
        // Left pad the unsigned, big endian bytes from common secret
        byte[] inputKeyMaterial = new byte[128];
        Span<byte> sharedSecretSpan = inputKeyMaterial.AsSpan(128 - sharedSecret.GetByteCount(true));
        sharedSecret.TryWriteBytes(sharedSecretSpan, out var _, true, true);

        // Run HKDF with SHA256, null salt and empty info, returning a 128 bit (16 byte) key for AES
        byte[] aesKey = HKDF.DeriveKey(HashAlgorithmName.SHA256, inputKeyMaterial, 16, null, null);

        return aesKey;
    }
}

