using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SignalR.Reactive
{
    public static class ClientsideConstants
    {
        public const string OnNextMethodName = "subjectOnNext";
        public const string OnNextType = "onNext";
        public const string OnErrorType = "onError";
        public const string OnCompletedType = "onCompleted";
    }
}
