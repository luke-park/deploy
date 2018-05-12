using ljp.Deploy.Local;
using ljp.Deploy.SSH.Hash;
using ljp.Deploy.V8;
using Renci.SshNet;
using Renci.SshNet.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ljp.Deploy.SSH
{
    public class SSHSession : IDisposable, ISSHServiceProvider
    {
        private const int BUFFER_SIZE = 1024;

        private V8Context _ctx;
        private SshClient _sshClient;
        private SftpClient _sftpClient;
        private ShellStream _shell;
        private Hashlist _hashList;
        private string _hashListPath;
        private bool _hashListChanged;

        // Constructor.
        public SSHSession(V8Context ctx, string hostName, string userName, int port, string keyFile, string keyFilePassword)
        {
            PrivateKeyAuthenticationMethod pkMethod = new PrivateKeyAuthenticationMethod(userName, new PrivateKeyFile(keyFile, keyFilePassword ?? ""));
            ConnectionInfo connInfo = new ConnectionInfo(hostName, port, userName, pkMethod);

            _ctx = ctx;
            _sshClient = new SshClient(connInfo);
            _sftpClient = new SftpClient(connInfo);
            _hashList = null;
            _hashListPath = null;
            _hashListChanged = false;

            _sshClient.Connect();
            _sftpClient.Connect();
            _shell = _sshClient.CreateShellStream("dumb", 0, 0, 0, 0, 1024);
            WaitForPrompt();
        }

        // Dispose.
        public void Dispose()
        {
            if (_sshClient != null) { _sshClient.Dispose(); _sshClient = null; }
            if (_sftpClient != null) { _sftpClient.Dispose(); _sftpClient = null; }
            if (_shell != null) { _shell.Dispose(); _shell = null; }
        }

        // Hashlist.
        public void setHashlistPath(string path)
        {
            _ctx.Write("Downloading hashlist from " + path + "...");

            try { _hashList = Hashlist.Load(DownloadFile(path)); }
            catch
            {
                _hashList = new Hashlist();
                _ctx.Write("Couldn't find a valid hashlist at the given location.  Creating an empty one...");
            }
            _hashListPath = path;
            _hashListChanged = false;

            _ctx.Write("Hashlist successfully downloaded!");
        }
        public void updateHashlist(Filepair p)
        {
            _hashList.Update(new Hashpair(p.remotePath, p.localPath));
            _hashListChanged = true;
        }
        public bool matchesHashlist(Filepair p)
        {
            if (!_hashList.Pairs.ContainsKey(p.remotePath)) { return false; }
            byte[] currentHash = _hashList.Pairs[p.remotePath].Hash;
            byte[] newHash = SSHHelper.ComputeHash(p.localPath);
            return SSHHelper.TimeSafeComparison(currentHash, newHash);
        }
        public void purgeHashlist(Filepair[] files)
        {
            _ctx.Write("Performing purge...");
            HashSet<string> safeKeys = new HashSet<string>();
            List<string> pendingDelete = new List<string>();

            foreach (Filepair p in files) { safeKeys.Add(p.remotePath); }
            foreach (String k in _hashList.Pairs.Keys)
            {
                if (!safeKeys.Contains(k))
                {
                    _ctx.Write("\tDeleting " + k + "...");
                    try { DeleteFile(k); }
                    catch { }

                    pendingDelete.Add(k);
                }
            }
            foreach (string k in pendingDelete)
            {
                _hashList.Pairs.Remove(k);
                _hashListChanged = true;
            }
        }

        // Files.
        public void uploadFile(Filepair p)
        {
            _ctx.Write("Uploading " + Path.GetFileName(p.localPath) + " to " + p.remotePath);
            UploadFileWithDirectories(p.localPath, p.remotePath);
        }
        public void uploadIfRequired(Filepair p)
        {
            if (!matchesHashlist(p))
            {
                uploadFile(p);
                updateHashlist(p);
            }
        }
        public void downloadFile(Filepair p)
        {
            string temp = DownloadFile(p.remotePath);
            if (File.Exists(p.localPath)) { File.Delete(p.localPath); }

            File.Copy(temp, p.localPath);
            File.Delete(temp);
        }
        public void deleteFile(string remoteFile)
        {
            DeleteFile(remoteFile);
        }
        public void setFilePermissions(string remoteFile, string octalPermissionString, bool useSudo)
        {
            executeCustomCommand(String.Format("{0}chmod {1} {2}", useSudo ? "sudo " : "", octalPermissionString, remoteFile));
        }

        // Services.
        public void stopService(string name)
        {
            ExecuteCommand("sudo systemctl stop " + name);
            _ctx.Write("Stopped service '" + name + "'.");
        }
        public void startService(string name)
        {
            ExecuteCommand("sudo systemctl start " + name);
            _ctx.Write("Started service '" + name + "'.");
        }

        // Custom.
        public void executeCustomCommand(string command)
        {
            _ctx.Write("> " + command);
            ExecuteCommand(command);
        }

        // Helper.
        public void ReuploadHashlist()
        {
            if (!_hashListChanged) { return; }

            _ctx.Write("Uploading new hashlist...");
            string tempPath = Path.GetTempFileName();
            _hashList.Save(tempPath);
            UploadFile(tempPath, _hashListPath);

            try { File.Delete(tempPath); }
            catch { }
            try { File.Delete(_hashListPath); }
            catch { }

            _ctx.Write("Uploaded!");
        }
        public string DownloadFile(string fileName)
        {
            string tempName = Path.GetTempFileName();
            using (FileStream fs = File.Open(tempName, FileMode.Create))
            {
                _sftpClient.DownloadFile(fileName, fs);
            }

            return tempName;
        }
        public void UploadFile(string fileName, string dest)
        {
            using (FileStream fs = File.Open(fileName, FileMode.Open))
            {
                _sftpClient.UploadFile(fs, dest.Replace("\\", "/"));
            }
        }
        public void UploadFileWithDirectories(string fileName, string dest)
        {
            string[] directoryNames = Path.GetDirectoryName(dest).Replace("\\", "/").Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            int direction = -1;

            try { UploadFile(fileName, dest); }
            catch (SftpPathNotFoundException)
            {
                for (int i = directoryNames.Length - 1; ; i += direction)
                {
                    string directoryToCreate = "";
                    for (int j = 0; j < i + 1; j++) { directoryToCreate += "/" + directoryNames[j]; }

                    try
                    {
                        _sftpClient.CreateDirectory(directoryToCreate);
                        if (direction == -1) { direction = 1; }
                        if (direction == 1 && i == directoryNames.Length - 1) { break; }
                    }
                    catch (SftpPathNotFoundException) { }
                }
            }

            UploadFile(fileName, dest);
        }
        public void DeleteFile(string fileName)
        {
            _sftpClient.DeleteFile(fileName);
        }
        public void WaitForPrompt()
        {
            ExecuteCommand(null);
        }
        public string ExecuteCommand(string command)
        {
            if (!String.IsNullOrEmpty(command)) { _shell.Write(command + Environment.NewLine); }

            string data = String.Empty;
            while (!data.EndsWith("~$ ") && !data.EndsWith("~# "))
            {
                byte[] buf = new byte[BUFFER_SIZE];
                int bytesRead = _shell.Read(buf, 0, buf.Length);
                Array.Resize(ref buf, bytesRead);
                data += Encoding.UTF8.GetString(buf);
            }

            if (String.IsNullOrEmpty(command)) { return null; }

            data = data.Replace("\r\n", "\n");
            string[] spl = data.Split(new string[] { "\n" }, StringSplitOptions.None);
            if (spl.Length < 2) { return String.Empty; }

            string result = String.Empty;
            for (int i = 1; i < spl.Length; i++)
            {
                if (spl[i].EndsWith("~$ ") || spl[i].EndsWith("~# ")) { continue; }
                result += spl[i] + Environment.NewLine;
            }

            if (result.EndsWith(Environment.NewLine)) { result = result.Substring(0, result.Length - Environment.NewLine.Length); }

            return result;
        }
    }
}
