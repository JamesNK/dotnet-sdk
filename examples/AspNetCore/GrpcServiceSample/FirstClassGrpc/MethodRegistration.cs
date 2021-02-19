using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GrpcServiceSample.FirstClassGrpc
{
    internal class MethodRegistration
    {
        public IDictionary<string, IInvokeCallHandler> Methods { get; } = new Dictionary<string, IInvokeCallHandler>();
    }
}
