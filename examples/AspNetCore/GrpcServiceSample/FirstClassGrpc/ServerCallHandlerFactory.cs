using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapr.Client.Autogen.Grpc.v1;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.AspNetCore.Server;
using Grpc.AspNetCore.Server.Model;
using Grpc.Core;
using Grpc.Shared.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace GrpcServiceSample.FirstClassGrpc
{
    /// <summary>
    /// Creates server call handlers. Provides a place to get services that call handlers will use.
    /// </summary>
    internal partial class ServerCallHandlerFactory<TService> where TService : class
    {
        private readonly IGrpcServiceActivator<TService> _serviceActivator;
        private readonly GrpcServiceOptions _globalOptions;
        private readonly GrpcServiceOptions<TService> _serviceOptions;

        public ServerCallHandlerFactory(
            IOptions<GrpcServiceOptions> globalOptions,
            IOptions<GrpcServiceOptions<TService>> serviceOptions,
            IGrpcServiceActivator<TService> serviceActivator)
        {
            _serviceActivator = serviceActivator;
            _serviceOptions = serviceOptions.Value;
            _globalOptions = globalOptions.Value;
        }

        // Internal for testing
        internal MethodOptions CreateMethodOptions()
        {
            return MethodOptions.Create(new[] { _globalOptions, _serviceOptions });
        }

        public IInvokeCallHandler CreateUnary<TRequest, TResponse>(Method<TRequest, TResponse> method, UnaryServerMethod<TService, TRequest, TResponse> invoker)
            where TRequest : class
            where TResponse : class
        {
            var options = CreateMethodOptions();
            var methodInvoker = new UnaryServerMethodInvoker<TService, TRequest, TResponse>(invoker, method, options, _serviceActivator);

            var handlerType = typeof(UnaryServerCallHandler<,,>).MakeGenericType(new[] { typeof(TService), typeof(TRequest), typeof(TResponse) });
            return (IInvokeCallHandler)Activator.CreateInstance(handlerType, methodInvoker);
        }
    }

    internal class UnaryServerCallHandler<TService, TRequest, TResponse> : IInvokeCallHandler
        where TService : class
        where TRequest : class, IMessage, new()
        where TResponse : class, IMessage
    {
        private readonly UnaryServerMethodInvoker<TService, TRequest, TResponse> invoker;

        public UnaryServerCallHandler(UnaryServerMethodInvoker<TService, TRequest, TResponse> invoker)
        {
            this.invoker = invoker;
        }

        public async Task<InvokeResponse> Invoke(HttpContext httpContext, ServerCallContext serverCallContext, InvokeRequest request)
        {
            var requestData = request.Data.Unpack<TRequest>();
            var responseData = await this.invoker.Invoke(httpContext, serverCallContext, requestData);
            return new InvokeResponse
            {
                Data = Any.Pack(responseData)
            };
        }
    }

    internal interface IInvokeCallHandler
    {
        Task<InvokeResponse> Invoke(HttpContext httpContext, ServerCallContext serverCallContext, InvokeRequest request);
    }
}
