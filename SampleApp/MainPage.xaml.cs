using Microsoft.Practices.ServiceLocation;
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
            this.InitializeComponent();
        }

        /// <summary>
        /// Gets the main viewmodel.
        /// </summary>
        public MainViewModel Vm
        {
            get { return (MainViewModel)ServiceLocator.Current.GetInstance<IMainViewModel>(); }
        }
    }
}
