using System;
using System.Collections.Generic;
using System.IO;

namespace ljp.Deploy.Local
{
    public class LocalSession : ILocalServiceProvider
    {
        public object args = null;
        public object filePairCollection = null;

        // Output.
        public void write(string text)
        {
            Console.WriteLine("\t" + text);
        }

        // FileSystem.
        public string readFile(string filename)
        {
            return File.ReadAllText(filename);
        }
        public void writeFile(string filename, string text)
        {
            File.WriteAllText(filename, text);
        }
        public void appendFile(string filename, string text)
        {
            File.AppendAllText(filename, text);
        }
        public void deleteFile(string filename)
        {
            File.Delete(filename);
        }
        public bool fileExists(string filename)
        {
            return File.Exists(filename);
        }

        // Pairings.
        public Filepair filePair(string r, string l)
        {
            return new Filepair(r, l);
        }
        public Filepair[] filePairCollectionNative(string rd, string ld, bool recursive)
        {
            List<Filepair> arr = new List<Filepair>();
            List<string> pendingDirectories = new List<string>();

            ld = Path.GetFullPath(ld);
            ld = ld.Replace("\\", "/");

            if (!Directory.Exists(ld)) { throw new ArgumentException("The given local directory is not actually a directory."); }
            pendingDirectories.Add(ld);

            while (pendingDirectories.Count > 0)
            {
                string dir = pendingDirectories[0].Replace("\\", "/");
                pendingDirectories.RemoveAt(0);

                foreach (string f in Directory.GetFiles(dir))
                {
                    string filename = f.Replace("\\", "/");
                    arr.Add(new Filepair(rd + "/" + filename.Replace(ld + "/", ""), f.Replace("\\", "/")));
                }

                if (!recursive) { continue; }

                foreach (string dirName in Directory.GetDirectories(dir))
                {
                    pendingDirectories.Add(dirName);
                }
            }

            return arr.ToArray();
        }
    }
}
