using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapr.Client;
using Google.Protobuf;
using Grpc.Core;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Dapr.Client
{
    public class DaprCallInvoker : CallInvoker
    {
        private readonly DaprClient daprClient;
        private readonly string appId;

        public DaprCallInvoker(DaprClient daprClient, string appId)
        {
            this.daprClient = daprClient;
            this.appId = appId;
        }

        public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options)
        {
            throw new NotSupportedException();
        }

        public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options)
        {
            throw new NotSupportedException();
        }

        public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options, TRequest request)
        {
            throw new NotSupportedException();
        }

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options, TRequest request)
        {
            var call = new DaprGrpcCall<TRequest, TResponse>(this.daprClient, this.appId, method, options, request);

            return new AsyncUnaryCall<TResponse>(
                responseAsync: call.GetResponseAsync(),
                responseHeadersAsync: Callbacks<TRequest, TResponse>.GetResponseHeadersAsync,
                getStatusFunc: Callbacks<TRequest, TResponse>.GetStatus,
                getTrailersFunc: Callbacks<TRequest, TResponse>.GetTrailers,
                disposeAction: Callbacks<TRequest, TResponse>.Dispose,
                call);
        }

        public override TResponse BlockingUnaryCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options, TRequest request)
        {
            var call = AsyncUnaryCall(method, host, options, request);
            return call.ResponseAsync.GetAwaiter().GetResult();
        }

        // Store static callbacks so delegates are allocated once
        private static class Callbacks<TRequest, TResponse>
            where TRequest : class
            where TResponse : class
        {
            internal static readonly Func<object, Task<Metadata>> GetResponseHeadersAsync = state => ((DaprGrpcCall<TRequest, TResponse>)state).GetResponseHeadersAsync();
            internal static readonly Func<object, Status> GetStatus = state => ((DaprGrpcCall<TRequest, TResponse>)state).GetStatus();
            internal static readonly Func<object, Metadata> GetTrailers = state => ((DaprGrpcCall<TRequest, TResponse>)state).GetTrailers();
            internal static readonly Action<object> Dispose = state => ((DaprGrpcCall<TRequest, TResponse>)state).Dispose();
        }

        private class DaprGrpcCall<TRequest, TResponse>
        {
            private readonly DaprClient daprClient;
            private readonly string appId;
            private readonly Method<TRequest, TResponse> method;
            private readonly CallOptions options;
            private readonly TRequest request;

            public DaprGrpcCall(DaprClient daprClient, string appId, Method<TRequest, TResponse> method, CallOptions options, TRequest request)
            {
                this.daprClient = daprClient;
                this.appId = appId;
                this.method = method;
                this.options = options;
                this.request = request;
            }

            public Task<TResponse> GetResponseAsync()
            {
                // Required because InvokeMethodGrpcAsync has generic constraints
                var methods = typeof(DaprClient).GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                var filteredMethods = methods.Where(m => m.Name == "InvokeMethodGrpcAsync" && m.GetParameters().Length == 4 && m.GetGenericArguments().Length == 2).ToList();
                var method = filteredMethods.Single();
                var genericMethod = method.MakeGenericMethod(new[] { typeof(TRequest), typeof(TResponse) });

                var result = (Task<TResponse>)genericMethod.Invoke(this.daprClient, new object[] { this.appId, this.method.Name.ToLower(), this.request, this.options.CancellationToken });
                return result;
            }

            public Task<Metadata> GetResponseHeadersAsync()
            {
                throw new NotSupportedException();
            }

            public Status GetStatus()
            {
                throw new NotSupportedException();
            }

            public Metadata GetTrailers()
            {
                throw new NotSupportedException();
            }

            public void Dispose()
            {

            }
        }
    }
}
