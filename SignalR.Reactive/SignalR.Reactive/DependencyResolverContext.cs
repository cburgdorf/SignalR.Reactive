using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SignalR.Infrastructure;

namespace SignalR.Reactive
{
    public static class DependencyResolverContext
    {
        public static IDependencyResolver Instance { get; set; }
    }
}
