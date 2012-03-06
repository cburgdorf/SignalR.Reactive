using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SignalR.Hubs;
using SignalR.Infrastructure;

namespace SignalR.Reactive
{
    public static class DependencyResolverExtensions
    {
        public static IDependencyResolver EnableRxSupport(this IDependencyResolver dependencyResolver)
        {
            var proxyGenerator = new Lazy<RxJsProxyGenerator>(() => new RxJsProxyGenerator(dependencyResolver));
            dependencyResolver.Register(typeof(IJavaScriptProxyGenerator), () => proxyGenerator.Value);

            return dependencyResolver;
        }
    }
}
