# What does the SignalR.Reactive library wants to be?

It's a binding for the Reactive Extensions for SignalR

# What is so cool about it?

If you know Rx in general than you will probably know that the basic idea of Rx is to turn 
the programming model up side down (from imperative to reactive). 

That's no different with this bindings. Where as the standard SignalR way is to call from the server
into the client by calling client side JavaScript methods from within a .NET hub, with this bindings 
that's no longer the case. Instead you have a serverside Observable<T> and map it one by one onto
a Observable that lives on the clientside (of course, RxJS is needed on the client)

# Ok, show me the codez


#Sample 1 - Having a serverside source that constantly produces values

    protected void Application_Start()
    {
        AreaRegistration.RegisterAllAreas();

        RegisterGlobalFilters(GlobalFilters.Filters);
        RegisterRoutes(RouteTable.Routes);

        //First, enable Rx Support so that you get the extended proxy generation
        Configuration.EnableRxSupport();

        //HOT STUFF
        //We have a serverside IObservable<string> that gets published on the client side
        //We essentially say map this Observable to an Observable property on the hub

        Observable
            .Interval(TimeSpan.FromSeconds(1))
            .Select(_ => DateTime.Now.ToLongTimeString())
            .ToClientside().Observable<RxHub>(x => x.SomeValue);
    }
    
We essentially say, take this Observable and map it to the Observable property "SomeValue" that lives on the hub.
Observable properties that live on the hub will automatically be available on the client side.
So working with them is trivial:

    var myHub = $.connection.rxHub;
    myHub.getObservable('SomeValue').subscribe(function (x) {
        $('#counter').html(x);
    });
    $.connection.hub.start();
    
#Sample 2 - Starting long running serverside operations that constantly give feedback about the progress
Here we have the Controller Method "DoALongRunningOperation" which gets invoked by the client with an unique
ID. Assume the operation exposes an Observable<string> for a detailed log. We publish that Observable
with the ID as its name to the client:

    public void DoALongRunningOperation(string id)
        {
            var subject = new Subject<string>();

            Task.Factory.StartNew(() =>
            {
                subject.OnNext("just started");
                Thread.Sleep(1000);
                subject.OnNext("One second passed, I'm still running");
                Thread.Sleep(5000);
                subject.OnNext("Another five seconds passed, I'm still running");
                Thread.Sleep(5000);
                subject.OnNext("Almost done");
                subject.OnCompleted();
            });

            subject.ToClientside().Observable<RxHub>(id);
        }
    }

Clientside, we invoke the Controller method and subscribe to the dynamically created Observable

    var guid = new Date().getTime().toString();

    $('#button').click(function () {
        $('#button').attr('disabled', true);
        myHub.getObservable(guid)
            .subscribe(function (log) {
                $('#operationLog').append(log);
                $('#operationLog').append('<br>');
            }, function () {
                $('#operationLog').append('fatal error');
                $('#operationLog').append('<br>');
            }, function () {
                $('#operationLog').append('we are done!');
                $('#operationLog').append('<br>');
                $('#button').attr('disabled', false);
            });
        $.get('/Home/DoALongRunningOperation/' + guid);
    });

Both examples can be found in this solution. Just grab the code and run it!
    
# What's left to say

Up to the moment you need a special SignalR build (comes bundled with this library) to use it.
I'm quite confident that you won't need that special built from the point on where the new SignalR version comes out.