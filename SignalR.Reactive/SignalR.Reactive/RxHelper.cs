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

        public static dynamic GetHubClients<THub>(string clientName) where THub : Hub, new()
        {
            var clients = GetHubClients<THub>();
            return GetHubClients(clients, clientName);
        }

        public static dynamic GetHubClients<THub>(Hub hub, string clientName) where THub : Hub, new()
        {
            var clients = GetHubClients<THub>();
            return GetHubClients(clients, clientName);
        }

        public static void WithClient(Hub hub, string clientName, Action<dynamic> continueWith)
        {
            var clients = GetHubClients(hub, clientName);
            continueWith(clients);
        }

        public static void WithClient<THub>(string clientName, Action<dynamic> continueWith) where THub : Hub, new()
        {
            var clients = GetHubClients<THub>(clientName);
            continueWith(clients);
        }

        public static dynamic GetHubClients(dynamic clients, string clientName)
        {
            return string.IsNullOrEmpty(clientName) ? clients : clients[clientName];
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

        public static RxHubRaiser<T> RaiseOn<T>() where T : Hub, new()
        {
            return new RxHubRaiser<T>();
        } 
    }

    public class RxHubRaiser<THub> where THub : Hub, new()
    {
        public void Next<T>(string eventName, T payload)
        {
            Next(eventName, null, payload);
        }

        public void Next<T>(string eventName, string clientName, T payload)
        {
            RxHelper.WithClient<THub>(clientName, clients => RxHelper.RaiseOnNext(eventName, clients, payload));
        }

        public void Error(string eventName, Exception exception)
        {
            Error(eventName, null, exception);
        }

        public void Error(string eventName, string clientName, Exception exception)
        {
            RxHelper.WithClient<THub>(clientName, clients => RxHelper.RaiseOnError(eventName, clients, exception));
        }

        public void Completed(string eventName)
        {
            Completed(eventName, null);
        }

        public void Completed(string eventName, string clientName)
        {
            RxHelper.WithClient<THub>(clientName, clients => RxHelper.RaiseOnCompleted(eventName, clients));
        }
    }
}
