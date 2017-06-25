using System.Collections.Generic;

namespace VLC
{
    /// <summary>
    /// Represents a media source that delivers media samples to a media pipeline.
    /// </summary>
    public interface IMediaSource
    {
        /// <summary>
        /// Gets the URI associated with the MediaSource.
        /// </summary>
        string Uri { get; }

        /// <summary>
        /// Gets the collection of external timed text sources associated with the MediaSource.
        /// </summary>
        IList<TimedTextSource> ExternalTimedTextSources { get; }
    }
}
