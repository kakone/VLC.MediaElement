using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;

namespace SampleApp
{
    /// <summary>
    /// ViewModel locator.
    /// </summary>
    public class ViewModelLocator
    {
        /// <summary>
        /// Register the view models.
        /// </summary>
        static ViewModelLocator()
        {
            var simpleIoc = SimpleIoc.Default;
            ServiceLocator.SetLocatorProvider(() => simpleIoc);

            simpleIoc.Register<MainViewModel>();
        }

        /// <summary>
        /// Gets the main viewmodel.
        /// </summary>
        public MainViewModel Main
        {
            get { return ServiceLocator.Current.GetInstance<MainViewModel>(); }
        }
    }
}
