using System.Security.Cryptography;
using System.Text;

namespace Saffrat.Helpers
{
    // Helper class for encryption methods.
    public class Encryption
    {
        // Computes and returns the MD5 hash of the input string.
        public static string GetMD5(string str)
        {
            var md5 = MD5.Create();
            byte[] fromData = Encoding.UTF8.GetBytes(str);
            byte[] targetData = md5.ComputeHash(fromData);
            string byte2String = String.Empty;

            for (int i = 0; i < targetData.Length; i++)
            {
                byte2String += targetData[i].ToString("x2");
            }
            return byte2String;
        }
    }
}
