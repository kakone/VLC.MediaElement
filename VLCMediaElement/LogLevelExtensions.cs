using Windows.Foundation.Diagnostics;

namespace VLC
{
    /// <summary>
    /// Extensions methods for <see cref="LogLevel"/> enumeration.
    /// </summary>
    internal static class LogLevelExtensions
    {
        /// <summary>
        /// Converts a VLC LogLevel to a Windows.Foundation.Diagnostics.LoggingLevel.
        /// </summary>
        /// <param name="logLevel">log level.</param>
        /// <returns>the corresponding LoggingLevel.</returns>
        public static LoggingLevel ToLoggingLevel(this LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Debug:
                    return LoggingLevel.Verbose;
                case LogLevel.Error:
                    return LoggingLevel.Error;
                case LogLevel.Warning:
                    return LoggingLevel.Warning;
                default:
                    return LoggingLevel.Information;
            }
        }
    }
}
