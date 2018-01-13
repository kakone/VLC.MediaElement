using System;
using System.Threading.Tasks;
using Windows.Foundation.Diagnostics;
using Windows.Storage;

namespace VLC
{
    static class Logger
    {
        private static LoggingSession LoggingSession { get; } = new LoggingSession("VLC.MediaElement");

        /// <summary>
        /// Adds a logging channel to the current logging session.
        /// </summary>
        /// <param name="name">The name of the logging channel.</param>
        /// <returns>a new logging channel.</returns>
        public static LoggingChannel AddLoggingChannel(string name)
        {
            var loggingChannel = new LoggingChannel(string.IsNullOrWhiteSpace(name) ? "VLC" : name, null);
            LoggingSession.AddLoggingChannel(loggingChannel);
            return loggingChannel;
        }

        /// <summary>
        /// Removes the specified logging channel from the current logging session.
        /// </summary>
        /// <param name="loggingChannel">The logging channel to remove.</param>
        public static void RemoveLoggingChannel(LoggingChannel loggingChannel)
        {
            if (loggingChannel != null)
            {
                try
                {
                    LoggingSession.RemoveLoggingChannel(loggingChannel);
                    loggingChannel.Dispose();
                }
                catch (Exception) { }
            }
        }

        /// <summary>
        /// Saves the current logging session to a file.
        /// </summary>
        /// <param name="logFilename">The name of the log file.</param>
        /// <returns>When this method completes, it returns the new file as a StorageFile.</returns>
        public static async Task<StorageFile> SaveToFileAsync(string logFilename)
        {
            return await LoggingSession.SaveToFileAsync(ApplicationData.Current.LocalFolder, logFilename);
        }
    }
}
