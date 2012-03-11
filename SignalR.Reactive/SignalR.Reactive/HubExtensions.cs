using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SignalR.Hubs;

namespace SignalR.Reactive
{
    public static class HubExtensions
    {
        private static dynamic GetClients(this Hub hub, string clientName)
        {
            return string.IsNullOrEmpty(clientName) ? hub.Clients : hub.Clients[clientName];
        }

        private static void WithClient(Hub hub, string clientName, Action<dynamic> continueWith)
        {
            var clients = GetClients(hub, clientName);
            continueWith(clients);
        }

        public static void RaiseOnNext<T>(this Hub hub, string eventName, T payload)
        {
            RaiseOnNext(hub,eventName, null, payload);
        }

        public static void RaiseOnNext<T>(this Hub hub, string eventName, string clientName, T payload)
        {
            WithClient(hub, clientName, clients => RxHelper.RaiseOnNext(eventName, clients, payload));
        }

        public static void RaiseOnError<T>(this Hub hub, string eventName, T payload)
        {
            RaiseOnNext(hub, eventName, null, payload);
        }

        public static void RaiseOnError<T>(this Hub hub, string eventName, string clientName, T payload) where T : Exception
        {
            WithClient(hub, clientName,clients => RxHelper.RaiseOnError(eventName, clients, payload));
        }

        public static void RaiseCompleted(this Hub hub, string eventName)
        {
            RaiseCompleted(hub, eventName, null);
        }

        public static void RaiseCompleted(this Hub hub, string eventName, string clientName)
        {
            WithClient(hub, clientName, clients => RxHelper.RaiseOnCompleted(eventName, clients));
        }
    }
}
