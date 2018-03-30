namespace ljp.Deploy.Local
{
    internal interface ILocalServiceProvider
    {
        // Output.
        void write(string text);

        // FileSystem.
        string readFile(string filename);
        void writeFile(string filename, string text);
        void appendFile(string filename, string text);
        void deleteFile(string filename);
        bool fileExists(string filename);

        // Pairings.
        Filepair filePair(string r, string l);
    }
}
