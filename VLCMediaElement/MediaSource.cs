using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace VLC
{
    /// <summary>
    /// Represents a media source.
    /// </summary>
    public sealed class MediaSource : IMediaSource
    {
        /// <summary>
        /// Gets the URI associated with the MediaSource.
        /// </summary>
        public string Uri { get; set; }

        /// <summary>
        /// Gets the collection of external timed text sources associated with the MediaSource.
        /// </summary>
        public IList<TimedTextSource> ExternalTimedTextSources { get; } = new ObservableCollection<TimedTextSource>();

        /// <summary>
        /// Creates an instance of MediaSource from the provided Uri.
        /// </summary>
        /// <param name="uri">The URI from which the MediaSource is created.</param>
        /// <returns>The new media source.</returns>
        public static MediaSource CreateFromUri(string uri)
        {
            return new MediaSource() { Uri = uri };
        }
    }
}
