using System;
using System.Linq.Expressions;
using SignalR.Hosting.AspNet;
using SignalR.Hubs;
using SignalR.Infrastructure;

namespace SignalR.Reactive
{
    public class Clientside<T>
    {
        private readonly IObservable<T> _observable;
        
        internal Clientside(IObservable<T> observable)
        {
            _observable = observable;
        }
        
        public IDisposable Observable<THub>(string eventName) where THub : Hub, new()
        {
            return Observable<THub>(eventName, null);
        }

        public IDisposable Observable<THub>(string eventName, string clientName) where THub : Hub, new()
        {
            var connectionManager = AspNetHost.DependencyResolver.Resolve<IConnectionManager>();
            
            dynamic clients = connectionManager.GetClients<THub>();
            clients = string.IsNullOrEmpty(clientName) ? clients : clients[clientName];

            return _observable.Subscribe(
                x => clients.Invoke("subjectOnNext", new { Data = x, EventName = eventName, Type = "onNext" }),
                x => clients.Invoke("subjectOnNext", new { Data = x, EventName = eventName, Type = "onError" }),
                () => clients.Invoke("subjectOnNext", new { EventName = eventName, Type = "onCompleted" })
                );
        }
        
    }

    public static class SignalRObservableExtensions
    {
        public static Clientside<T> ToClientside<T>(this IObservable<T> observable)
        {
            return new Clientside<T>(observable);
        }
    }
}
