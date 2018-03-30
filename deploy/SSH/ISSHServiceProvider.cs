using ljp.Deploy.Local;

namespace ljp.Deploy.SSH
{
    internal interface ISSHServiceProvider
    {
        // Hashlist.
        void setHashlistPath(string path);
        void updateHashlist(Filepair p);
        bool matchesHashlist(Filepair p);
        void purgeHashlist(Filepair[] files);

        // Files.
        void uploadFile(Filepair p);
        void uploadIfRequired(Filepair p);
        void downloadFile(Filepair p);
        void deleteFile(string remoteFile);
        void setFilePermissions(string remoteFile, string octalPermissionString, bool useSudo);

        // Services.
        void stopService(string name);
        void startService(string name);

        // Custom.
        void executeCustomCommand(string command);
    }
}
