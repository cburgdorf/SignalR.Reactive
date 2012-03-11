using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SignalR.Hubs;
using SignalR.Infrastructure;

namespace SignalR.Reactive
{
    public static class RxHelper
    {
        public static dynamic GetHubClients<THub>() where THub : Hub, new()
        {
            var connectionManager = DependencyResolverContext.Instance.Resolve<IConnectionManager>();
            return connectionManager.GetClients<THub>();
        }

        public static void RaiseOnNext<T>(string eventName, dynamic clients, T payload)
        {
            clients.Invoke(ClientsideConstants.OnNextMethodName, new { Data = payload, EventName = eventName, Type = ClientsideConstants.OnNextType});
        }

        public static void RaiseOnError(string eventName, dynamic clients, Exception payload)
        {
            clients.Invoke(ClientsideConstants.OnNextMethodName,new { Data = payload, EventName = eventName, Type = ClientsideConstants.OnErrorType});
        }

        public static void RaiseOnCompleted(string eventName, dynamic clients)
        {
            clients.Invoke(ClientsideConstants.OnNextMethodName, new { EventName = eventName, Type = ClientsideConstants.OnCompletedType});
        }
    }
}
