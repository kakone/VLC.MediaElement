using CommonServiceLocator;
using GalaSoft.MvvmLight.Ioc;

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
        public MainViewModel Main => ServiceLocator.Current.GetInstance<MainViewModel>();
    }
}
