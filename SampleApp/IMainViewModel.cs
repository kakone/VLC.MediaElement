using System.Windows.Input;

namespace SampleApp
{
    /// <summary>
    /// Interface for the main viewmodel.
    /// </summary>
    public interface IMainViewModel
    {
        /// <summary>
        /// Gets the media source.
        /// </summary>
        string Source { get; }

        /// <summary>
        /// Gets the media title.
        /// </summary>
        string Title { get; }

        /// <summary>
        /// Gets the open file command.
        /// </summary>
        ICommand OpenFileCommand { get; }
    }
}
