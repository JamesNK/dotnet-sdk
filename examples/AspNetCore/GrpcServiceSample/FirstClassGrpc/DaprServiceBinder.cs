using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Grpc.AspNetCore.Server;
using Grpc.AspNetCore.Server.Model;
using Grpc.Core;
using Grpc.Shared.Server;

namespace GrpcServiceSample.FirstClassGrpc
{
    internal class DaprServiceBinder<TService> : ServiceBinderBase where TService : class
    {
        private readonly MethodRegistration methodRegistration;
        private readonly ServerCallHandlerFactory<TService> serverCallHandlerFactory;
        private readonly Type declaringType;

        public DaprServiceBinder(MethodRegistration methodRegistration, ServerCallHandlerFactory<TService> serverCallHandlerFactory, Type declaringType)
        {
            this.methodRegistration = methodRegistration;
            this.serverCallHandlerFactory = serverCallHandlerFactory;
            this.declaringType = declaringType;
        }

        public override void AddMethod<TRequest, TResponse>(Method<TRequest, TResponse> method, ClientStreamingServerMethod<TRequest, TResponse> handler)
        {
            // Not supported.
        }

        public override void AddMethod<TRequest, TResponse>(Method<TRequest, TResponse> method, DuplexStreamingServerMethod<TRequest, TResponse> handler)
        {
            // Not supported.
        }

        public override void AddMethod<TRequest, TResponse>(Method<TRequest, TResponse> method, ServerStreamingServerMethod<TRequest, TResponse> handler)
        {
            // Not supported.
        }

        public override void AddMethod<TRequest, TResponse>(Method<TRequest, TResponse> method, UnaryServerMethod<TRequest, TResponse> handler)
        {
            var invoker = CreateModelCore<UnaryServerMethod<TService, TRequest, TResponse>>(
                method.Name,
                new[] { typeof(TRequest), typeof(ServerCallContext) });

            var serverCallHandler = this.serverCallHandlerFactory.CreateUnary<TRequest, TResponse>(method, invoker);

            methodRegistration.Methods.Add(method.Name.ToLower(), serverCallHandler);
        }

        private TDelegate CreateModelCore<TDelegate>(string methodName, Type[] methodParameters) where TDelegate : Delegate
        {
            var handlerMethod = GetMethod(methodName, methodParameters);

            if (handlerMethod == null)
            {
                throw new InvalidOperationException($"Could not find '{methodName}' on {typeof(TService)}.");
            }

            return (TDelegate)Delegate.CreateDelegate(typeof(TDelegate), handlerMethod);
        }

        private MethodInfo GetMethod(string methodName, Type[] methodParameters)
        {
            Type currentType = typeof(TService);
            while (currentType != null)
            {
                // Specify binding flags explicitly because we don't want to match static methods.
                var matchingMethod = currentType.GetMethod(
                    methodName,
                    BindingFlags.Public | BindingFlags.Instance,
                    binder: null,
                    types: methodParameters,
                    modifiers: null);

                if (matchingMethod == null)
                {
                    return null;
                }

                // Validate that the method overrides the virtual method on the base service type.
                // If there is a method with the same name it will hide the base method. Ignore it,
                // and continue searching on the base type.
                if (matchingMethod.IsVirtual)
                {
                    var baseDefinitionMethod = matchingMethod.GetBaseDefinition();
                    if (baseDefinitionMethod != null && baseDefinitionMethod.DeclaringType == this.declaringType)
                    {
                        return matchingMethod;
                    }
                }

                currentType = currentType.BaseType;
            }

            return null;
        }
    }
}
