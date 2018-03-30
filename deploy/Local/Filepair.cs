namespace ljp.Deploy.Local
{
    public struct Filepair
    {
        public string remotePath;
        public string localPath;

        // Constructor.
        public Filepair(string r, string l)
        {
            this.remotePath = r;
            this.localPath = l;
        }
    }
}
