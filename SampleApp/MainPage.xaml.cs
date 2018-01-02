using Windows.UI.Xaml.Controls;

namespace SampleApp
{
    /// <summary>
    /// Main page.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        /// <summary>
        /// Initializes a new instance of MainPage class.
        /// </summary>
        public MainPage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Gets the main viewmodel.
        /// </summary>
        public MainViewModel Vm => DataContext as MainViewModel;
    }
}
