using ljp.Deploy.Local;
using ljp.Deploy.SSH;
using Microsoft.ClearScript.V8;
using System;
using System.IO;

namespace ljp.Deploy.V8
{
    public delegate void ConsoleOutputDelegate(string text);
    public delegate void SessionCompleteDelegate(Exception ex);

    public class V8Session
    {
        private string[] _args;
        private string _filename;
        private ConsoleOutputDelegate _outputHandler;
        private SessionCompleteDelegate _completeHandler;

        // Constructor.
        public V8Session(string[] args, string filename, ConsoleOutputDelegate outputHandler, SessionCompleteDelegate completeHandler)
        {
            _args = args;
            _filename = filename;
            _outputHandler = outputHandler;
            _completeHandler = completeHandler;
        }

        // Start.
        public void Start()
        {
            try { Work(); NotifySessionComplete(null); }
            catch (Exception ex) { NotifySessionComplete(ex); }
        }

        // Work.
        private void Work()
        {
            using (V8ScriptEngine engine = new V8ScriptEngine())
            {
                string scriptContents = File.ReadAllText(_filename);
                engine.AddHostObject("local", (ILocalServiceProvider)new LocalSession());
                V8EmbeddedMethods.AddFilePairCollectionMethod(engine);
                V8EmbeddedMethods.AddArgsArray(engine, _args);
                engine.Execute(scriptContents);

                V8Context ctx = V8Context.Build(engine);
                for (int i = 0; i < ctx.Hosts.Length; i++)
                {
                    string host = ctx.Hosts[i];

                    try
                    {
                        ctx.Write("\b-> " + host);

                        int port = (int)engine.Evaluate(String.Format("getPortForHost('{0}');", host));
                        string username = (string)engine.Evaluate(String.Format("getUsernameForHost('{0}');", host));
                        string keyFile = (string)engine.Evaluate(String.Format("getKeyfileForHost('{0}');", host));
                        string keyFilePassword = (string)engine.Evaluate(String.Format("getKeyfilePasswordForKeyfile('{0}');", keyFile));
                        ctx.Write("Connecting to " + host + ":" + port.ToString() + "...");

                        using (SSHSession ssh = new SSHSession(ctx, host, username, port, keyFile, keyFilePassword))
                        {
                            engine.AddHostObject("NATIVE_SSH_OBJ_" + i.ToString(), (ISSHServiceProvider)ssh);
                            engine.Execute("deploy('" + host + "', NATIVE_SSH_OBJ_" + i.ToString() + ");");
                            ssh.ReuploadHashlist();
                        }

                        ctx.Write("");
                    }
                    catch (Exception ex)
                    {
                        ctx.Write("\bDeployment failed for " + host + ": " + ex.Message);
                    }
                }
            }
        }

        // Thread-Safety.
        private void PerformConsoleWrite(string text)
        {
            _outputHandler(text);
        }
        private void NotifySessionComplete(Exception ex)
        {
            _completeHandler(ex);
        }
    }
}
