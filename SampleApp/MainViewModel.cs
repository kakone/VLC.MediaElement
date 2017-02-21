using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Windows.Input;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;

namespace SampleApp
{
    /// <summary>
    /// Main ViewModel.
    /// </summary>
    public class MainViewModel : ViewModelBase, IMainViewModel
    {
        private const string FILE_TOKEN = "{1BBC4B94-BE33-4D79-A0CB-E5C6CDB9D107}";

        /// <summary>
        /// Initializes a new instance of MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            OpenFileCommand = new RelayCommand(OpenFile);
            Source = "http://download.blender.org/peach/bigbuckbunny_movies/big_buck_bunny_480p_surround-fix.avi";
            Title = "Big Buck Bunny";
        }

        private string _source;
        /// <summary>
        /// Gets the media source.
        /// </summary>
        public string Source
        {
            get { return _source; }
            private set { Set(nameof(Source), ref _source, value); }
        }

        private string _title;
        /// <summary>
        /// Gets the media title.
        /// </summary>
        public string Title
        {
            get { return _title; }
            private set { Set(nameof(Title), ref _title, value); }
        }

        /// <summary>
        /// Gets the open file command.
        /// </summary>
        public ICommand OpenFileCommand { get; private set; }

        private async void OpenFile()
        {
            var fileOpenPicker = new FileOpenPicker();
            fileOpenPicker.SuggestedStartLocation = PickerLocationId.VideosLibrary;
            fileOpenPicker.FileTypeFilter.Add("*");
            var file = await fileOpenPicker.PickSingleFileAsync();
            if (file != null)
            {
                StorageApplicationPermissions.FutureAccessList.AddOrReplace(FILE_TOKEN, file);
                Source = null;
                Source = $"winrt://{FILE_TOKEN}";
                Title = file.DisplayName;
            }
        }
    }
}
