using Windows.UI.Xaml;

namespace VLC
{
    /// <summary>
    /// Provides event data for media failed events.
    /// </summary>
    public sealed class MediaFailedRoutedEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Gets the title of the error message.
        /// </summary>
        public string ErrorTitle { get; internal set; }

        /// <summary>
        /// Gets the message component of the exception, as a string.
        /// </summary>
        public string ErrorMessage { get; internal set; }
    }
}
