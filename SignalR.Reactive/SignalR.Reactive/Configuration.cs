using SignalR.Hosting.AspNet;

namespace SignalR.Reactive
{
    public class Configuration
    {
        public static void EnableRxSupport()
        {
            AspNetHost.DependencyResolver.EnableRxSupport();
        }
    }
}
