using Microsoft.AspNetCore.DataProtection;

namespace DatabaseFirstApproachPractice.Security
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
            return _protector.Unprotect(cipherText);
        }
    }
}
