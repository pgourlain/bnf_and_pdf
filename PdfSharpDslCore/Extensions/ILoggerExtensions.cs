using System;
using Microsoft.Extensions.Logging;

namespace PdfSharpDslCore.Extensions
{
    internal static class LoggerExtensions
    {
        public static bool DebugEnabled(this ILogger? logger)
        {
            return logger?.IsEnabled(LogLevel.Debug) ?? false;
        }

        public static void WriteDebug(this ILogger? logger, object identifier, string message)
        {
            string identifierName = string.Empty;
            if (identifier is IHasName hasName)
            {
                identifierName = $"({hasName.Name})";
            }
            logger?.LogDebug($"{identifier.GetHashCode()}{identifierName}:{message}");
        }
    }
}