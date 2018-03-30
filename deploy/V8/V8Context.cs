using Microsoft.ClearScript.V8;
using System;
using System.Collections.Generic;

namespace ljp.Deploy.V8
{
    public class V8Context
    {
        public bool ShouldUseVerboseMessages { get; private set; }
        public bool HasDeployErrorFunction { get; private set; }
        public string[] Hosts { get; private set; }

        // Entry.
        public V8Context(bool verboseMessages, bool deployErrorFunction, string[] hosts)
        {
            ShouldUseVerboseMessages = verboseMessages;
            HasDeployErrorFunction = deployErrorFunction;
            Hosts = hosts;
        }

        // Build.
        public static V8Context Build(V8ScriptEngine engine)
        {
            bool hostsExists = !(bool)engine.Evaluate("typeof hosts !== 'object' || Object.prototype.toString.call(hosts) !== '[object Array]';");
            if (!hostsExists) { throw new Exception("The variable `hosts` is either not defined or is not an array."); }

            int hostsLen = (int)engine.Evaluate("hosts.length");
            bool hostsIsNotEmpty = hostsLen != 0;
            if (!hostsIsNotEmpty) { throw new Exception("The variable `hosts` is an array, but appears to be empty.  You must define at least one hostname."); }

            bool hostsAreStrings = (bool)engine.Evaluate("function f() { for (let host of hosts) { if (typeof host !== 'string') { return false; } } return true; } f();");
            if (!hostsAreStrings) { throw new Exception("The variable `hosts` is an array, but contains some elements that are not strings, and thus not valid hostnames."); }

            string[] requiredFunctionNames = new string[] { "getPortForHost", "getUsernameForHost", "getKeyfileForHost", "getKeyfilePasswordForKeyfile", "deploy" };
            foreach (string s in requiredFunctionNames)
            {
                bool funcExists = (bool)engine.Evaluate("typeof " + s + " === 'function';");
                if (!funcExists) { throw new Exception("The function `" + s + "` is either not defined or is not a valid function."); }
            }

            bool shouldUseVerboseMessages = (bool)engine.Evaluate("typeof useVerboseMessages !== 'boolean' || useVerboseMessages;");
            bool hasDeployErrorFunc = (bool)engine.Evaluate("typeof deployError === 'function';");

            List<string> hosts = new List<string>();
            for (int i = 0; i < hostsLen; i++)
            {
                hosts.Add((string)engine.Evaluate(String.Format("hosts[{0}]", i)));
            }

            return new V8Context(shouldUseVerboseMessages, hasDeployErrorFunc, hosts.ToArray());
        }

        // Write.
        public void Write(string line)
        {
            if (ShouldUseVerboseMessages) { Console.WriteLine("\t" + line); }
        }
    }
}
