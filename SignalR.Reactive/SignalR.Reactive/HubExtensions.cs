using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SignalR.Hubs;

namespace SignalR.Reactive
{
    public static class HubExtensions
    {
        public static void RaiseOnNext<T>(this Hub hub, string eventName, T payload)
        {
            RaiseOnNext(hub,eventName, null, payload);
        }

        public static void RaiseOnNext<T>(this Hub hub, string eventName, string clientName, T payload)
        {
            RxHelper.WithClient(hub, clientName, clients => RxHelper.RaiseOnNext(eventName, clients, payload));
        }

        public static void RaiseOnNextOnGroup<T>(this Hub hub, string eventName, string groupName, T payload)
        {
            RxHelper.WithGroup(hub, groupName, clients => RxHelper.RaiseOnNext(eventName, clients, payload));
        }



        public static void RaiseOnError<T>(this Hub hub, string eventName, T payload)
        {
            RaiseOnNext(hub, eventName, null, payload);
        }

        public static void RaiseOnError<T>(this Hub hub, string eventName, string clientName, T payload) where T : Exception
        {
            RxHelper.WithClient(hub, clientName, clients => RxHelper.RaiseOnError(eventName, clients, payload));
        }

        public static void RaiseOnErrorOnGroup<T>(this Hub hub, string eventName, string groupName, T payload) where T : Exception
        {
            RxHelper.WithGroup(hub, groupName, clients => RxHelper.RaiseOnError(eventName, clients, payload));
        }


        public static void RaiseCompleted(this Hub hub, string eventName)
        {
            RaiseCompleted(hub, eventName, null);
        }

        public static void RaiseCompleted(this Hub hub, string eventName, string clientName)
        {
            RxHelper.WithClient(hub, clientName, clients => RxHelper.RaiseOnCompleted(eventName, clients));
        }

        public static void RaiseCompletedOnGroup(this Hub hub, string eventName, string groupName)
        {
            RxHelper.WithGroup(hub, groupName, clients => RxHelper.RaiseOnCompleted(eventName, clients));
        }
    }
}
