using System;
using System.Security.Cryptography;
using Tmds.DBus.Protocol;

namespace DBus.Services.Secrets;

/// <summary>
/// Represents a session in the D-Bus secret service.
/// </summary>
public class Session
{
    private byte[]? _aesKey;

    public ObjectPath SessionPath { get; }

    public bool IsEncryptedSession => _aesKey != null;

    /// <summary>
    /// Creates a wrapper around an unencrypted session.
    /// </summary>
    /// <param name="sessionPath">The <see cref="ObjectPath"/> to the session.</param>
    public Session(ObjectPath sessionPath)
    {
        SessionPath = sessionPath;
    }

    /// <summary>
    /// Creates a wrappper around a DH encrypted session.
    /// </summary>
    /// <param name="sessionPath">The <see cref="ObjectPath"/> to the session.</param>
    /// <param name="aesKey">The AES key to use for encryption/decryption.</param>
    public Session(ObjectPath sessionPath, byte[] aesKey)
    {
        SessionPath = sessionPath;
        _aesKey = aesKey;
    }

    /// <summary>
    /// Encrypts the provided data using the provided AES initialisation vector.
    /// Only works when this session has an associated AES key.
    /// </summary>
    /// <param name="data">The data to encrypt.</param>
    /// <param name="aesIv">The AES initialisation vector.</param>
    /// <returns>The encrypted data.</returns>
    public byte[] Encrypt(byte[] data, byte[] aesIv)
    {
        if (_aesKey == null)
        {
            throw new InvalidOperationException("Cannot encrypt data while using plain transport!");
        }

        // Secret service uses AES in CBC mode with PKCS7 padding
        Aes aes = Aes.Create();
        aes.Key = _aesKey;

        return aes.EncryptCbc(data, aesIv, PaddingMode.PKCS7);
    }

    /// <summary>
    /// Decrypts the provided data using the provided AES initialisation vector.
    /// Only works when this session has an associated AES key.
    /// </summary>
    /// <param name="encryptedData">The data to decrypt.</param>
    /// <param name="aesIv">The AES initialisation vector.</param>
    /// <returns>The decrypted data.</returns>
    public byte[] Decrypt(byte[] encryptedData, byte[] aesIv)
    {
        if (_aesKey == null)
        {
            throw new InvalidOperationException("Cannot decrypt data while using plain transport!");
        }

        // Secret service uses AES in CBC mode with PKCS7 padding
        Aes aes = Aes.Create();
        aes.Key = _aesKey;

        return aes.DecryptCbc(encryptedData, aesIv, PaddingMode.PKCS7);
    }
}
