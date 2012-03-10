using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using SignalR.Hubs;

namespace SignalR.Reactive.Demo.Models
{

    public class RxHub : Hub
    {
        public void MoveShape(int x, int y)
        {
            this.RaiseOnNext("ShapeMoved", new
            {
                Cid = Context.ConnectionId,
                X = x,
                Y = y
            });
        }

    }
}