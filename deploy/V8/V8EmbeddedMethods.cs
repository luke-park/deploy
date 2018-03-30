using Microsoft.ClearScript.V8;
using System;

namespace ljp.Deploy.V8
{
    static internal class V8EmbeddedMethods
    {
        public static void AddArgsArray(V8ScriptEngine engine, string[] args)
        {
            string scr = "local.args=[];";
            foreach (string s in args) { scr += String.Format("local.args.push('{0}');", s); }

            engine.Execute(scr);
        }
        public static void AddFilePairCollectionMethod(V8ScriptEngine engine)
        {
            string scr = "local.filePairCollection = (rd, ld, sub) => {";
            scr += "let nArr = local.filePairCollectionNative(rd, ld, sub);";
            scr += "let r = [];";
            scr += "for (let x of nArr) { r.push(x); }";
            scr += "return r;";
            scr += "};";

            engine.Execute(scr);
        }
    }
}
