using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using SignalR.Hubs;
using SignalR.Infrastructure;

namespace SignalR.Reactive
{
    class RxJsProxyGenerator
    {
        private static readonly Lazy<string> _template = new Lazy<string>(GetTemplate);
        private static readonly ConcurrentDictionary<string, string> _scriptCache = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private const string ScriptResource = "SignalR.Scripts.hubs.js";

        private readonly IHubLocator _hubLocator;
        private readonly IJavaScriptMinifier _javascriptMinifier;

        public RxJsProxyGenerator(IDependencyResolver resolver) :
            this(resolver.Resolve<IHubLocator>(),
                 resolver.Resolve<IJavaScriptMinifier>())
        {
        }

        public RxJsProxyGenerator(IHubLocator hubLocator, IJavaScriptMinifier javascriptMinifier)
        {
            _hubLocator = hubLocator;
            _javascriptMinifier = javascriptMinifier ?? NullJavaScriptMinifier.Instance;
        }

        public bool IsDebuggingEnabled { get; set; }

        public string GenerateProxy(string serviceUrl)
        {
            string script;
            if (_scriptCache.TryGetValue(serviceUrl, out script))
            {
                return script;
            }

            var template = _template.Value;

            script = template.Replace("{serviceUrl}", serviceUrl);

            var hubs = new StringBuilder();
            var first = true;
            foreach (var type in _hubLocator.GetHubs())
            {
                if (!first)
                {
                    hubs.AppendLine(",");
                    hubs.Append("        ");
                }
                GenerateType(serviceUrl, hubs, type);
                first = false;
            }

            script = script.Replace("/*hubs*/", hubs.ToString());

            if (!IsDebuggingEnabled)
            {
                script = _javascriptMinifier.Minify(script);
            }

            _scriptCache.TryAdd(serviceUrl, script);

            return script;
        }

        private void GenerateType(string serviceUrl, StringBuilder sb, Type type)
        {
            // Get public instance methods declared on this type only
            var methods = GetMethods(type);
            var members = methods.Select(m => m.Name).ToList();

            // Get observable properties declared on this type
            var observableProperties = ReflectionHelper.GetExportedHubObservables(type);

            members.Add("namespace");
            members.Add("ignoreMembers");
            members.Add("callbacks");

            sb.AppendFormat("{0}: {{", GetHubName(type)).AppendLine();
            sb.AppendFormat("            _: {{").AppendLine();
            sb.AppendFormat("                hubName: '{0}',", type.FullName ?? "null").AppendLine();
            sb.AppendFormat("                ignoreMembers: [{0}],", Commas(members, m => "'" + Json.CamelCase(m) + "'")).AppendLine();
            sb.AppendLine("                connection: function () { return signalR.hub; }");
            sb.AppendFormat("            }}");
            if (methods.Any() || observableProperties.Any())
            {
                sb.Append(",").AppendLine();
            }
            else
            {
                sb.AppendLine();
            }

            bool firstMethod = true;

            foreach (var method in methods)
            {
                if (!firstMethod)
                {
                    sb.Append(",").AppendLine();
                }
                GenerateMethod(serviceUrl, sb, type, method);
                firstMethod = false;
            }

            if (observableProperties.Any() && methods.Any())
            {
                sb.Append(",").AppendLine();
            }

            bool firstProperty = true;

            foreach (var observableProperty in observableProperties)
            {
                if (!firstProperty)
                {
                    sb.Append(",").AppendLine();
                }
                GenerateRxSubject(sb, type, observableProperty);
                firstProperty = false;
            }
            
            sb.AppendLine();
            sb.Append("        }");
        }

        protected virtual string GetHubName(Type type)
        {
            return ReflectionHelper.GetAttributeValue<HubNameAttribute, string>(type, a => a.HubName) ?? Json.CamelCase(type.Name);
        }

        protected virtual IEnumerable<MethodInfo> GetMethods(Type type)
        {
            // Pick the overload with the minimum number of arguments
            return from method in ReflectionHelper.GetExportedHubMethods(type)
                   group method by method.Name into overloads
                   let oload = (from overload in overloads
                                   orderby overload.GetParameters().Length
                                   select overload).FirstOrDefault()
                   select oload;
        }

        private void GenerateMethod(string serviceUrl, StringBuilder sb, Type type, MethodInfo method)
        {
            var parameters = method.GetParameters();
            var parameterNames = parameters.Select(p => p.Name).ToList();
            parameterNames.Add("callback");
            sb.AppendLine();
            sb.AppendFormat("            {0}: function ({1}) {{", GetMethodName(method), Commas(parameterNames)).AppendLine();
            sb.AppendFormat("                return serverCall(this, \"{0}\", $.makeArray(arguments));", method.Name).AppendLine();
            sb.Append("            }");
        }

        private void GenerateRxSubject(StringBuilder sb, Type type, PropertyInfo property)
        {
            Json.CamelCase(property.Name);
            var hubName = Json.CamelCase(type.Name);
            sb.AppendFormat("            subject : new Rx.Subject(),");
            sb.AppendLine();
            sb.AppendFormat("            subjectOnNext: function(value) {{ signalR.{0}.subject.onNext(value); }},", hubName);
            sb.AppendLine();
            sb.AppendFormat("            getObservable: function (eventName) {{ ").AppendLine();
            sb.AppendFormat("                                return Rx.Observable.create(function (obs) {{ ").AppendLine();
            sb.AppendFormat("                                                var disposable = signalR.{0}.subject ", hubName).AppendLine();
            sb.AppendFormat("                                                    .asObservable() ").AppendLine();
            sb.AppendFormat("                                                    .where(function (x) {{ return x.EventName.toLowerCase() === eventName.toLowerCase(); }}) ").AppendLine();
            sb.AppendFormat("                                                    .subscribe(function (x) {{ ").AppendLine();
            sb.AppendFormat("                                                        if (x.Type === 'onNext') obs.onNext(x.Data); ").AppendLine();
            sb.AppendFormat("                                                        if (x.Type === 'onError') obs.onError(x.Data); ").AppendLine();
            sb.AppendFormat("                                                        if (x.Type === 'onCompleted') obs.onCompleted(); ").AppendLine();
            sb.AppendFormat("                                                    }}); ").AppendLine();
            sb.AppendFormat("                                                return disposable.dispose; ").AppendLine();
            sb.AppendFormat("                                 }}); ").AppendLine();
            sb.AppendFormat("                             }} ").AppendLine();
        }

        private static string GetMethodName(MethodInfo method)
        {
            return ReflectionHelper.GetAttributeValue<HubMethodNameAttribute, string>(method, a => a.MethodName) ?? Json.CamelCase(method.Name);
        }

        private static string Commas(IEnumerable<string> values)
        {
            return Commas(values, v => v);
        }

        private static string Commas<T>(IEnumerable<T> values, Func<T, string> selector)
        {
            return String.Join(", ", values.Select(selector));
        }

        private static string GetTemplate()
        {
            using (Stream resourceStream = typeof(DefaultJavaScriptProxyGenerator).Assembly.GetManifestResourceStream(ScriptResource))
            {
                using (var reader = new StreamReader(resourceStream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
