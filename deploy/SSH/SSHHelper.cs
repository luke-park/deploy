using System.IO;
using System.Security.Cryptography;

namespace ljp.Deploy.SSH
{
    internal static class SSHHelper
    {
        // Equality.
        public static bool TimeSafeComparison(byte[] b1, byte[] b2)
        {
            if (b1.Length != b2.Length) { return false; }

            int r = 0;
            for (int i = 0; i < b1.Length; i++)
            {
                r |= b1[i] ^ b2[i];
            }

            return r == 0;
        }

        // Hash.
        public static byte[] ComputeHash(string filename)
        {
            using (SHA256 sha256 = SHA256.Create())
            using (FileStream fs = File.Open(filename, FileMode.Open))
            {
                return sha256.ComputeHash(fs);
            }
        }
    }
}
