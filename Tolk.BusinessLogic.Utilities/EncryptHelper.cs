using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Tolk.BusinessLogic.Utilities
{
    public static class EncryptHelper
    {
        private const string VIKey = "@sdfSDFHsdfgSYs¤";

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5379:Do Not Use Weak Key Derivation Function Algorithm", Justification = "Does not need fullfledged encryption, and might already be in use...")]
        public static string Encrypt(string plainText, string passwordHash, string saltKey)
        {
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);

            using (var hash = new Rfc2898DeriveBytes(passwordHash, Encoding.ASCII.GetBytes(saltKey)))
            {
                byte[] keyBytes = hash.GetBytes(256 / 8);
                using (var symmetricKey = new RijndaelManaged() { Mode = CipherMode.CBC, Padding = PaddingMode.Zeros })
                {
                    var encryptor = symmetricKey.CreateEncryptor(keyBytes, Encoding.ASCII.GetBytes(VIKey));

                    byte[] cipherTextBytes;

                    using (var memoryStream = new MemoryStream())
                    {
                        using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                        {
                            cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                            cryptoStream.FlushFinalBlock();
                            cipherTextBytes = memoryStream.ToArray();
                            cryptoStream.Close();
                        }
                        memoryStream.Close();
                    }
                    return Convert.ToBase64String(cipherTextBytes);
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5379:Do Not Use Weak Key Derivation Function Algorithm", Justification = "Does not need fullfledged encryption, and might already be in use...")]
        public static string Decrypt(string encryptedText, string PasswordHash, string SaltKey)
        {
            byte[] cipherTextBytes = Convert.FromBase64String(encryptedText);
            using (var hash = new Rfc2898DeriveBytes(PasswordHash, Encoding.ASCII.GetBytes(SaltKey)))
            {
                byte[] keyBytes = hash.GetBytes(256 / 8);
                using (var symmetricKey = new RijndaelManaged() { Mode = CipherMode.CBC, Padding = PaddingMode.Zeros })
                {

                    var decryptor = symmetricKey.CreateDecryptor(keyBytes, Encoding.ASCII.GetBytes(VIKey));
                    var memoryStream = new MemoryStream(cipherTextBytes);
                    var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
                    byte[] plainTextBytes = new byte[cipherTextBytes.Length];

                    int decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                    memoryStream.Close();
                    cryptoStream.Close();
                    return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount).TrimEnd("\0".ToCharArray());
                }
            }
        }
    }
}
