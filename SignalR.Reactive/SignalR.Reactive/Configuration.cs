using System.Web;
using SignalR.Hosting.AspNet;

[assembly: PreApplicationStartMethod(typeof(SignalR.Reactive.Configuration), "EnableRxSupport")]
namespace SignalR.Reactive
{
    public static class Configuration
    {
        public static void EnableRxSupport()
        {
            AspNetHost.DependencyResolver.EnableRxSupport();
        }
    }
}
