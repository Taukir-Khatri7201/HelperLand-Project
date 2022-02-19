namespace HelperLand.Security
{
    public interface ICustomDataProtector
    {
        public string Encrypt(string normalText);
        public string Decrypt(string cipherText);
    }
}
