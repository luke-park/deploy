using ljp.Deploy.V8;
using System;

namespace ljp.Deploy
{
    internal class Deploy
    {
        // Entry.
        public static void Main(string[] args)
        {
            V8Session session = new V8Session(args, Environment.CurrentDirectory + "\\deploy.js", OutputHandler, CompleteHandler);
            session.Start();
        }

        // Complete.
        private static void CompleteHandler(Exception ex)
        {
            if (ex != null)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                return;
            }
        }

        // Output.
        private static void OutputHandler(string text)
        {
            Console.WriteLine(text);
        }
    }
}
