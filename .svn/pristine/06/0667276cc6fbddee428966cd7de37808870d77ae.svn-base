using System.Security.Cryptography;
using System.Text;

namespace ReliefApi
{
    public class EncryptDecrypt
    {
        private static string _key;

        public EncryptDecrypt()
        {
        }

        public static string Key
        {
            set { _key = value; }
        }

        public static string Encrypt(string strToEncrypt)
        {
            try
            {
                return Encrypt(strToEncrypt, _key);
            }
            catch (Exception ex)
            {
                return "Wrong Input. " + ex.Message;
            }
        }

        public static string Decrypt(string strEncrypted)
        {
            try
            {
                return Decrypt(strEncrypted, _key);
            }
            catch (Exception ex)
            {
                return "Wrong Input. " + ex.Message;
            }
        }

        public static string Encrypt(string strToEncrypt, string strKey)
        {
          
            try
            {
                using (var objDESCrypto = new TripleDESCryptoServiceProvider())
                using (var objHashMD5 = new MD5CryptoServiceProvider())
                {
                    byte[] byteHash;
                    byte[] byteBuff;
                    string strTempKey = strKey;

                    byteHash = objHashMD5.ComputeHash(Encoding.ASCII.GetBytes(strTempKey));
                    objHashMD5.Clear();

                    objDESCrypto.Key = byteHash;
                    objDESCrypto.Mode = CipherMode.ECB;

                    byteBuff = Encoding.ASCII.GetBytes(strToEncrypt);
                    return Convert.ToBase64String(objDESCrypto.CreateEncryptor().TransformFinalBlock(byteBuff, 0, byteBuff.Length));
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Encryption failed. " + ex.Message);
            }
        }

        public static string Decrypt(string strEncrypted, string strKey)
        {
            
            try
            {
                using (var objDESCrypto = new TripleDESCryptoServiceProvider())
                using (var objHashMD5 = new MD5CryptoServiceProvider())
                {
                    byte[] byteHash;
                    byte[] byteBuff;
                    string strTempKey = strKey;

                    byteHash = objHashMD5.ComputeHash(Encoding.ASCII.GetBytes(strTempKey));
                    objHashMD5.Clear();

                    objDESCrypto.Key = byteHash;
                    objDESCrypto.Mode = CipherMode.ECB;

                    byteBuff = Convert.FromBase64String(strEncrypted);
                    string strDecrypted = Encoding.ASCII.GetString(objDESCrypto.CreateDecryptor().TransformFinalBlock(byteBuff, 0, byteBuff.Length));
                    objDESCrypto.Clear();

                    return strDecrypted;
                }
            }
            catch (Exception ex)
            {
                return "";
            }
        }
    }
}
