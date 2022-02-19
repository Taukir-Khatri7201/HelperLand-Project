using Microsoft.AspNetCore.DataProtection;
using System;
using System.Security.Cryptography;

namespace HelperLand.Security
{
    public class CustomDataProtector : ICustomDataProtector
    {
        private const string purpose = "Password Protector";
        private readonly IDataProtector _protector;

        public CustomDataProtector(IDataProtectionProvider provider)
        {
            _protector = provider.CreateProtector(purpose);
        }
        public string Encrypt(string normalText)
        {
            return _protector.Protect(normalText);
        }

        public string Decrypt(string cipherText)
        {
            try
            {
                return _protector.Unprotect(cipherText);
            } 
            catch(Exception)
            {
                throw;
            }
        }
    }
}
