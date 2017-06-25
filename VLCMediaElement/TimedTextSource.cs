namespace VLC
{
    /// <summary>
    /// Represents a source of timed text data.
    /// </summary>
    public sealed class TimedTextSource
    {
        /// <summary>
        /// Gets the URI associated with the TimedTextSource.
        /// </summary>
        public string Uri { get; set; }

        /// <summary>
        /// Creates a new instance of TimedTextSource from the provided URI.
        /// </summary>
        /// <param name="uri">The URI from which the timed text source is created.</param>
        /// <returns>The new timed text source.</returns>
        public static TimedTextSource CreateFromUri(string uri)
        {
            return new TimedTextSource() { Uri = uri };
        }
    }
}
