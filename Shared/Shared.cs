using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization;

namespace CodeWhite.Remoting.Shared
{
    public class Utils
    {
        public static IMethodCallMessage CreateMethodCall(MethodInfo method, object[] methodArgs)
        {
            var methodSignature = method.GetParameters().Select(p => p.ParameterType).ToArray();
            return CreateMethodCall(method.DeclaringType.AssemblyQualifiedName, method.Name, methodArgs, methodSignature);
        }

        public static IMethodCallMessage CreateMethodCall(string typeName, string methodName, object[] args, Type[] signature)
        {
            // use `Object.ToString` for initialization to survive `ResolveType()` and `ResolveMethod()` calls
            var headers = new List<Header>()
            {
                new Header("__TypeName", "System.Object, mscorlib"),
                new Header("__MethodName", "ToString"),
            };
            // set actual values
            const string ns = "http://schemas.microsoft.com/clr/soap/messageProperties";
            var methodCall = new MethodCall(headers.ToArray());
            headers = new List<Header>()
            {
                new Header("__TypeName", typeName, true, ns),
                new Header("__MethodName", methodName, true, ns),
            };
            if (args != null)
            {
                headers.Add(new Header("__Args", args, true, ns));
            }
            if (signature != null)
            {
                headers.Add(new Header("__MethodSignature", signature, true, ns));
            }
            methodCall.HeaderHandler(headers.ToArray());
            return methodCall;
        }

        public static IMethodReturnMessage CallRemoteToStringMethod(Uri url, IDictionary<string, object> logicalCallContextData = null)
        {
            var transparentProxy = (MarshalByRefObject)RemotingServices.Connect(typeof(MarshalByRefObject), url.ToString());

            // create method call to `object.ToString()`
            var method = typeof(object).GetMethod("ToString", new Type[0]);
            var methodArgs = new object[0];
            var methodCall = Utils.CreateMethodCall(method, methodArgs);

            if (logicalCallContextData != null)
            {
                foreach (var pair in logicalCallContextData)
                {
                    methodCall.LogicalCallContext.SetData(pair.Key, pair.Value);
                }
            }
            ;
            return transparentProxy.CallMethod(methodCall);
        }

        public static void PrintInfo(MarshalByRefObject mbro)
        {
            Console.WriteLine($"[*] Obtained MBRO type: {mbro.GetType()}");
            Console.WriteLine($"[*] IsTransparentProxy: {RemotingServices.IsTransparentProxy(mbro)}");
            Console.WriteLine($"[*] GetObjectUri: {RemotingServices.GetObjectUri(mbro)}");
        }
    }

    static class RemotingExtensions
    {
        public static MarshalByRefObject CallMethod(this MarshalByRefObject mbro, string assemblyName, string typeName, string methodName, object[] args = null, Type[] signature = null)
        {
            return mbro.CallMethod<MarshalByRefObject>(assemblyName, typeName, methodName, args, signature);
        }

        public static T CallMethod<T>(this MarshalByRefObject mbro, string assemblyName, string typeName, string methodName, object[] args, Type[] signature)
        {
            if (mbro == null) throw new ArgumentNullException("mbro");
            if (typeName == null) throw new ArgumentNullException("typeName");
            if (args != null && signature != null && signature.Length != args.Length) throw new ArgumentException("signature length and args length are not equal");

            if (!string.IsNullOrEmpty(assemblyName)) typeName = $"{typeName}, {assemblyName}";

            var methodCall = Utils.CreateMethodCall(typeName, methodName, args, signature);
            var methodReturnMessage = mbro.CallMethod(methodCall);
            if (methodReturnMessage.Exception != null)
            {
                throw methodReturnMessage.Exception;
            }
            return (T)methodReturnMessage.ReturnValue;
        }

        public static IMethodReturnMessage CallMethod(this MarshalByRefObject mbro, IMethodCallMessage methodCall)
        {
            var remotingProxy = RemotingServices.GetRealProxy(mbro);
            return (IMethodReturnMessage)remotingProxy.Invoke(methodCall);
        }
    }

    [Serializable]
    public class UniversalMarshal : ISerializable
    {
        private readonly string _assemblyName;
        private readonly string _typeName;
        private readonly IDictionary<string, object> _serializationInfo;

        public UniversalMarshal(string assemblyName, string typeName, IDictionary<string, object> serializationInfo = null)
        {
            this._assemblyName = assemblyName;
            this._typeName = typeName;
            this._serializationInfo = serializationInfo;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AssemblyName = this._assemblyName;
            info.FullTypeName = this._typeName;
            if (this._serializationInfo != null)
            {
                foreach (var p in this._serializationInfo)
                {
                    info.AddValue(p.Key, p.Value);
                }
            }
        }
    }
}
