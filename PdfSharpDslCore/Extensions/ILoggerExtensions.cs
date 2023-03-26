using System;
using Microsoft.Extensions.Logging;

namespace PdfSharpDslCore.Extensions
{
    internal static class LoggerExtensions
    {
        private static readonly Func<ILogger, int, string, IDisposable?> SProcessingWorkScope 
            = LoggerMessage.DefineScope<int, string>(
                "{int}:{string}");
        public static IDisposable? ProcessingWorkScope(
            this ILogger logger, int time, string message) =>
            SProcessingWorkScope(logger, time, message);


        public static bool DebugEnabled(this ILogger? logger)
        {
            return logger?.IsEnabled(LogLevel.Debug) ?? false;
        }

        public static void WriteDebug(this ILogger? logger, object identifier, string message)
        {
            logger?.LogDebug($"{identifier.GetHashCode()}:{message}");
        }
    }
}