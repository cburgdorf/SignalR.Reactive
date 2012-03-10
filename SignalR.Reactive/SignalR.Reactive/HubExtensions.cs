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

        public static void RaiseOnNext<T>(this Hub hub, string eventName, T payload)
        {
            RaiseOnNext(hub,eventName, null, payload);
        }

        public static void RaiseOnNext<T>(this Hub hub, string eventName, string clientName, T payload)
        {
            var clients = GetClients(hub, clientName);
            clients.Invoke("subjectOnNext", new { Data = payload, EventName = eventName, Type = "onNext" });
        }

        public static void RaiseOnError<T>(this Hub hub, string eventName, T payload)
        {
            RaiseOnNext(hub, eventName, null, payload);
        }

        public static void RaiseOnError<T>(this Hub hub, string eventName, string clientName, T payload)
        {
            var clients = GetClients(hub, clientName);
            clients.Invoke("subjectOnNext", new { Data = payload, EventName = eventName, Type = "onError" });
        }

        public static void RaiseCompleted(this Hub hub, string eventName)
        {
            RaiseCompleted(hub, eventName, null);
        }

        public static void RaiseCompleted(this Hub hub, string eventName, string clientName)
        {
            var clients = GetClients(hub, clientName);
            clients.Invoke("subjectOnNext", new { EventName = eventName, Type = "onCompleted" });
        }
    }
}
