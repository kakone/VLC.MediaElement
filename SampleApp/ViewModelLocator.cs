using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;

namespace SampleApp
{
    /// <summary>
    /// ViewModel locator.
    /// </summary>
    static class ViewModelLocator
    {
        /// <summary>
        /// Register the view models.
        /// </summary>
        public static void Register()
        {
            var simpleIoc = SimpleIoc.Default;
            ServiceLocator.SetLocatorProvider(() => simpleIoc);

            simpleIoc.Register<IMainViewModel, MainViewModel>();
        }
    }
}
