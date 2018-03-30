using System.Collections.Generic;
using System.IO;

namespace ljp.Deploy.SSH.Hash
{
    internal class Hashlist
    {
        public Dictionary<string, Hashpair> Pairs { get; private set; }

        // Constructor.
        public Hashlist()
        {
            Pairs = new Dictionary<string, Hashpair>();
        }

        // Add/Remove.
        public void Add(Hashpair hashpair)
        {
            Pairs.Add(hashpair.RemotePath, hashpair);
        }
        public void Update(Hashpair hashpair)
        {
            Pairs[hashpair.RemotePath] = hashpair;
        }
        public void Remove(string remotePath)
        {
            Pairs.Remove(remotePath);
        }

        // Save/Load.
        public void Save(string filename)
        {
            using (FileStream fs = File.Open(filename, FileMode.Create))
            {
                foreach(string s in Pairs.Keys)
                {
                    byte[] rawHashpair = Pairs[s].Serialize();
                    fs.Write(rawHashpair, 0, rawHashpair.Length);
                }
            }
        }
        public static Hashlist Load(string filename)
        {
            Hashlist r = new Hashlist();

            using (FileStream fs = File.Open(filename, FileMode.Open))
            {
                while (fs.Position != fs.Length)
                {
                    r.Add(Hashpair.BuildFromStream(fs));
                }
            }

            return r;
        }
    }
}
