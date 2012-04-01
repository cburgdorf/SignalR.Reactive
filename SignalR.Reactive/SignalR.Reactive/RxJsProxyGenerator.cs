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
    class RxJsProxyGenerator : IJavaScriptProxyGenerator
    {
        private static readonly Lazy<string> _template = new Lazy<string>(GetTemplate);
        private static readonly ConcurrentDictionary<string, string> _scriptCache = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private const string ScriptResource = "SignalR.Scripts.hubs.js";

        private readonly IHubManager _manager;
        private readonly IJavaScriptMinifier _javascriptMinifier;

        public RxJsProxyGenerator(IDependencyResolver resolver) :
            this(resolver.Resolve<IHubManager>(),
                 resolver.Resolve<IJavaScriptMinifier>())
        {
        }

        public RxJsProxyGenerator(IHubManager manager, IJavaScriptMinifier javascriptMinifier)
        {
            _manager = manager;
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
            foreach (var descriptor in _manager.GetHubs())
            {
                if (!first)
                {
                    hubs.AppendLine(",");
                    hubs.Append("        ");
                }
                this.GenerateType(hubs, descriptor);
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

        private void GenerateType(StringBuilder sb, HubDescriptor descriptor)
        {
            // Get only actions with minimum number of parameters.
            var methods = GetMethods(descriptor);

            var members = methods.Select(m => m.Name).ToList();
            members.Add("namespace");
            members.Add("ignoreMembers");
            members.Add("callbacks");

            sb.AppendFormat("{0}: {{", GetHubName(descriptor)).AppendLine();
            sb.AppendFormat("            _: {{").AppendLine();
            sb.AppendFormat("                hubName: '{0}',", descriptor.Name ?? "null").AppendLine();
            sb.AppendFormat("                ignoreMembers: [{0}],", Commas(members, m => "'" + Json.CamelCase(m) + "'")).AppendLine();
            sb.AppendLine("                connection: function () { return signalR.hub; }");
            sb.AppendFormat("            }}");

            if (methods.Any())
            {
                sb.Append(",").AppendLine();
            }
            else
            {
                sb.AppendLine();
            }

            bool first = true;

            foreach (var method in methods)
            {
                if (!first)
                {
                    sb.Append(",").AppendLine();
                }
                this.GenerateMethod(sb, method);
                first = false;
            }

            GenerateRxSubject(sb, descriptor);

            sb.AppendLine();
            sb.Append("        }");
        }

        protected virtual string GetHubName(HubDescriptor descriptor)
        {
            return Json.CamelCase(descriptor.Name);
        }

        private IEnumerable<MethodDescriptor> GetMethods(HubDescriptor descriptor)
        {
            return from method in _manager.GetHubMethods(descriptor.Name)
                   group method by method.Name into overloads
                   let oload = (from overload in overloads
                                orderby overload.Parameters.Count
                                select overload).FirstOrDefault()
                   select oload;
        }

        private void GenerateMethod(StringBuilder sb, MethodDescriptor method)
        {
            var parameterNames = method.Parameters.Select(p => p.Name).ToList();
            parameterNames.Add("callback");
            sb.AppendLine();
            sb.AppendFormat("            {0}: function ({1}) {{", GetMethodName(method), Commas(parameterNames)).AppendLine();
            sb.AppendFormat("                return serverCall(this, \"{0}\", $.makeArray(arguments));", method.Name).AppendLine();
            sb.Append("            }");
        }

        private void GenerateRxSubject(StringBuilder sb, HubDescriptor descriptor)
        {
            var hubName = Json.CamelCase(descriptor.Name);
            sb.AppendFormat("            ,").AppendLine();
            sb.AppendFormat("            subject : new Rx.Subject(),").AppendLine();
            sb.AppendFormat("            subjectOnNext: function(value) {{ signalR.{0}.subject.onNext(value); }},", hubName).AppendLine();

            sb.AppendFormat("            observe: function (eventName) {{ ").AppendLine();
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

        private static string GetMethodName(MethodDescriptor method)
        {
            return Json.CamelCase(method.Name);
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
