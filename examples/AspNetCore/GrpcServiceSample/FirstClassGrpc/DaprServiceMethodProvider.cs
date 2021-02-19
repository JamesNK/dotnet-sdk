using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Grpc.AspNetCore.Server.Model;
using Grpc.Shared.Server;

namespace GrpcServiceSample.FirstClassGrpc
{
    internal class DaprServiceMethodProvider<TService> : IServiceMethodProvider<TService> where TService : class
    {
        private readonly MethodRegistration methodRegistration;
        private readonly ServerCallHandlerFactory<TService> serverCallHandlerFactory;

        public DaprServiceMethodProvider(MethodRegistration methodRegistration, ServerCallHandlerFactory<TService> serverCallHandlerFactory)
        {
            this.methodRegistration = methodRegistration;
            this.serverCallHandlerFactory = serverCallHandlerFactory;
        }

        public void OnServiceMethodDiscovery(ServiceMethodProviderContext<TService> context)
        {
            var bindMethodInfo = BindMethodFinder.GetBindMethod(typeof(TService));

            // Invoke BindService(ServiceBinderBase, BaseType)
            if (bindMethodInfo != null)
            {
                // The second parameter is always the service base type
                var serviceParameter = bindMethodInfo.GetParameters()[1];

                var binder = new DaprServiceBinder<TService>(this.methodRegistration, this.serverCallHandlerFactory, serviceParameter.ParameterType);

                try
                {
                    bindMethodInfo.Invoke(null, new object[] { binder, null });
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Error binding gRPC service '{typeof(TService).Name}'.", ex);
                }
            }
            else
            {
                // Log that binding method was not found.
            }
        }
    }
}
