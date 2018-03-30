using System;
using System.IO;
using System.Text;

namespace ljp.Deploy.SSH.Hash
{
    internal struct Hashpair
    {
        public string RemotePath { get; private set; }
        public byte[] Hash { get; private set; }

        // Constructor.
        public Hashpair(string remotePath, byte[] hash)
        {
            if (remotePath.Length > UInt16.MaxValue) { throw new ArgumentException("The remote path is larger than UInt16.MaxValue", "remotePath"); }

            RemotePath = remotePath ?? throw new ArgumentException("You cannot pass a null remote path.", "remotePath");
            Hash = hash ?? throw new ArgumentException("You cannot pass a null hash.", "hash");
        }
        public Hashpair(string remotePath, string localPath)
        {
            if (remotePath.Length > UInt16.MaxValue) { throw new ArgumentException("The remote path is larger than UInt16.MaxValue", "remotePath"); }

            RemotePath = remotePath ?? throw new ArgumentException("You cannot pass a null remote path.", "remotePath");
            Hash = SSHHelper.ComputeHash(localPath);
        }

        // Update.
        public void Update(string localPath)
        {
            Hash = SSHHelper.ComputeHash(localPath);
        }

        // Serialize.
        public byte[] Serialize()
        {
            byte[] r = new byte[RemotePath.Length + 2 + Hash.Length];
            byte[] rpSizeRaw = BitConverter.GetBytes((ushort)RemotePath.Length);
            byte[] rpRaw = Encoding.UTF8.GetBytes(RemotePath);

            Array.Copy(rpSizeRaw, 0, r, 0, rpSizeRaw.Length);
            Array.Copy(rpRaw, 0, r, rpSizeRaw.Length, rpRaw.Length);
            Array.Copy(Hash, 0, r, rpSizeRaw.Length + rpRaw.Length, Hash.Length);

            return r;
        }

        // Build.
        public static Hashpair BuildFromStream(Stream s)
        {
            if (!s.CanRead) { throw new ArgumentException("The given stream cannot be read from.", "s"); }

            byte[] rpSizeRaw = new byte[2];
            int c = s.Read(rpSizeRaw, 0, rpSizeRaw.Length);
            if (c != rpSizeRaw.Length) { throw new IOException("Expecting 2 bytes but read " + c.ToString() + " bytes."); }

            ushort rpSize = BitConverter.ToUInt16(rpSizeRaw, 0);
            byte[] rpRaw = new byte[rpSize];
            c = s.Read(rpRaw, 0, rpRaw.Length);
            if (c != rpSize) { throw new IOException("Expecting " + rpSize.ToString() + " bytes but read " + c.ToString() + " bytes."); }

            byte[] hash = new byte[32];
            c = s.Read(hash, 0, hash.Length);
            if (c != hash.Length) { throw new IOException("Expecting 32 bytes but read " + c.ToString() + " bytes."); }

            return new Hashpair(Encoding.UTF8.GetString(rpRaw), hash);
        }
    }
}
