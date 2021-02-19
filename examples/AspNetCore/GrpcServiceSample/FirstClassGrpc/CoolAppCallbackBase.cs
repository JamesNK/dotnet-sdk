using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapr.AppCallback.Autogen.Grpc.v1;
using Dapr.Client.Autogen.Grpc.v1;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace GrpcServiceSample.FirstClassGrpc
{
    internal class CoolAppCallbackBase : AppCallback.AppCallbackBase
    {
        public override async Task<InvokeResponse> OnInvoke(InvokeRequest request, ServerCallContext context)
        {
            var methodRegistration = context.GetHttpContext().RequestServices.GetRequiredService<MethodRegistration>();

            if (methodRegistration.Methods.TryGetValue(request.Method, out var invoker))
            {
                return await invoker.Invoke(context.GetHttpContext(), context, request);
            }

            // Unknown method. Do nothing
            return new InvokeResponse();
        }
    }
}
