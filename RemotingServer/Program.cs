using System;
using System.Collections;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Lifetime;
using System.Runtime.Serialization.Formatters;

namespace CodeWhite.Remoting.RemotingServer
{
    internal class Program
    {
        static readonly string objUri = "DummyService";
        static readonly int port = 12345;
        static readonly TypeFilterLevel typeFilterLevel = TypeFilterLevel.Low;

        static void Main(string[] args)
        {
            var properties = new Hashtable()
            {
                { "port", port },
                { "typeFilterLevel", typeFilterLevel.ToString() },
                { "rejectRemoteRequests", true },
            };
            var sinkProvider = new BinaryServerFormatterSinkProvider(properties, null);
            var channel = new TcpServerChannel(properties, sinkProvider);

            // ensure TypeFilterLevel.Low
            var sink = (BinaryServerFormatterSink)sinkProvider.CreateSink(channel);
            if (sink.TypeFilterLevel != typeFilterLevel)
            {
                throw new Exception("");
            }
            ChannelServices.RegisterChannel(channel, false);

            var uriBuilder = new UriBuilder("tcp", "127.0.0.1", (int)properties["port"], objUri);

            // marshal service with the server type `IDummyService`
            MarshalByRefObject serviceImpl = new DummyService();
            var lease = (ILease)serviceImpl.InitializeLifetimeService();
            lease.InitialLeaseTime = TimeSpan.Zero;
            RemotingServices.Marshal(serviceImpl, objUri, typeof(IDummyService));

            Console.WriteLine($"{nameof(DummyService)} available at 'tcp://127.0.0.1:{properties["port"]}/{objUri}'");
            Console.WriteLine();
            Console.Write("Press Enter to exit ...");
            Console.ReadLine();
        }
    }

    internal interface IDummyService
    {
        // intentionally empty
    }

    internal class DummyService : MarshalByRefObject, IDummyService
    {
    }
}
