using System;
using System.Threading.Tasks;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using VLC;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace SampleApp
{
    /// <summary>
    /// Main ViewModel.
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private const string FILE_TOKEN = "{1BBC4B94-BE33-4D79-A0CB-E5C6CDB9D107}";
        private const string SUBTITLE_FILE_TOKEN = "{16BA03D6-BCA8-403E-B1E8-166B0020B4A7}";

        /// <summary>
        /// Initializes a new instance of MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            OnViewModeChanged();
            OpenFileCommand = new RelayCommand(OpenFileAsync);
            OpenSubtitleFileCommand = new RelayCommand(OpenSubtitleFileAsync, () => MediaSource != null);
            ViewModeChangedCommand = new RelayCommand(OnViewModeChanged);
            MediaSource = VLC.MediaSource.CreateFromUri("http://download.blender.org/peach/bigbuckbunny_movies/big_buck_bunny_480p_surround-fix.avi");
            Title = "Big Buck Bunny";
        }

        private IMediaSource _mediaSource;
        /// <summary>
        /// Gets the media source.
        /// </summary>
        public IMediaSource MediaSource
        {
            get => _mediaSource;
            private set
            {
                if (_mediaSource != value)
                {
                    _mediaSource = value;
                    RaisePropertyChanged(nameof(MediaSource));
                    OpenSubtitleFileCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private string _title;
        /// <summary>
        /// Gets the media title.
        /// </summary>
        public string Title
        {
            get => _title;
            private set => Set(nameof(Title), ref _title, value);
        }

        /// <summary>
        /// Gets the open file command.
        /// </summary>
        public ICommand OpenFileCommand { get; }

        /// <summary>
        /// Gets the open subtitle file command.
        /// </summary>
        public RelayCommand OpenSubtitleFileCommand { get; }

        /// <summary>
        /// Gets the command for the view mode changes.
        /// </summary>
        public ICommand ViewModeChangedCommand { get; }

        private async Task<StorageFile> PickSingleFileAsync(string fileTypeFilter, string token)
        {
            var fileOpenPicker = new FileOpenPicker()
            {
                SuggestedStartLocation = PickerLocationId.VideosLibrary
            };
            fileOpenPicker.FileTypeFilter.Add(fileTypeFilter);
            var file = await fileOpenPicker.PickSingleFileAsync();
            if (file != null)
            {
                StorageApplicationPermissions.FutureAccessList.AddOrReplace(token, file);
            }
            return file;
        }

        private async void OpenFileAsync()
        {
            var file = await PickSingleFileAsync("*", FILE_TOKEN);
            if (file != null)
            {
                MediaSource = VLC.MediaSource.CreateFromUri($"winrt://{FILE_TOKEN}");
                Title = file.DisplayName;
            }
        }

        private async void OpenSubtitleFileAsync()
        {
            var mediaSource = MediaSource;
            if (mediaSource == null)
            {
                return;
            }
            var file = await PickSingleFileAsync(".srt", SUBTITLE_FILE_TOKEN);
            if (file != null)
            {
                MediaSource.ExternalTimedTextSources.Add(TimedTextSource.CreateFromUri($"winrt://{SUBTITLE_FILE_TOKEN}"));
            }
        }

        private void OnViewModeChanged()
        {
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 4))
            {
                var applicationView = ApplicationView.GetForCurrentView();
                var applicationViewTitleBar = applicationView.TitleBar;
                var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
                if (applicationView.ViewMode == ApplicationViewMode.CompactOverlay)
                {
                    applicationViewTitleBar.ButtonBackgroundColor = Colors.Transparent;
                    coreTitleBar.ExtendViewIntoTitleBar = true;
                }
                else
                {
                    applicationViewTitleBar.ButtonBackgroundColor = null;
                    coreTitleBar.ExtendViewIntoTitleBar = false;
                }
            }
        }
    }
}
