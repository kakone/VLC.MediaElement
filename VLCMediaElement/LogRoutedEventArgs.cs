using Windows.Foundation.Diagnostics;
using Windows.UI.Xaml;

namespace VLC
{
    /// <summary>
    /// Provides event data for media failed events.
    /// </summary>
    public sealed class LogRoutedEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Gets the title of the error message.
        /// </summary>
        public LoggingLevel Level { get; internal set; }

        /// <summary>
        /// Gets the message component of the exception, as a string.
        /// </summary>
        public string Message { get; internal set; }
    }
}
