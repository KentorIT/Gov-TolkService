using System;
using System.Security.Cryptography;
using System.Text;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Services
{
    public static class HashHelper
    {
        public static string CreateSalt(int size)
        {
            //Generate a cryptographic random number.
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                byte[] buff = new byte[size];
                rng.GetBytes(buff);
                return Convert.ToBase64String(buff);
            }
        }

        public static string GenerateHash(string input, string salt)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input + salt);
            using (SHA256Managed sHA256ManagedString = new SHA256Managed())
            {
                byte[] hash = sHA256ManagedString.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        public static bool AreEqual(string plainTextInput, string hashedInput, string salt)
        {
            string newHashedPin = GenerateHash(plainTextInput, salt);
            return newHashedPin.EqualsSwedish(hashedInput);
        }
    }
}
