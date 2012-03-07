using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using SignalR.Hubs;

namespace SignalR.Reactive
{
    class ExtendedReflectionHelper
    {
        private static readonly Type[] _excludeTypes = new[] { typeof(Hub), typeof(object) };
        private static readonly Type[] _excludeInterfaces = new[] { typeof(IHub), typeof(IDisconnect), typeof(IConnected) };

        internal static IEnumerable<T> ContinueWithOrReturnEmptyIfNotAHub<T>(Type type, Func<IEnumerable<T>> processWith)
        {
            return !typeof(IHub).IsAssignableFrom(type) ? Enumerable.Empty<T>() : processWith();
        }

        internal static IEnumerable<PropertyInfo> GetExportedHubObservables(Type type)
        {
            return ContinueWithOrReturnEmptyIfNotAHub(type, () =>
                type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(x => x.PropertyType.Name == "IObservable`1"));
        }
    }
}
