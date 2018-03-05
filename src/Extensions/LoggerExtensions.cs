using System;
using System.Collections.Generic;

namespace Trainiot.LoggerExtensions.CommandStation
{
    internal static class LoggerExtensions
    {
        public static IDisposable BeginScope(this ILogger logger, params KeyValuePair<string, object> properties)
        {
            return this.BeginScope((IEnumerable<KeyValuePair<string,object>>)properties);
        } 
    }
}