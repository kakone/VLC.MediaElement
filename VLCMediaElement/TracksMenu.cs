using Windows.UI.Xaml.Controls;

namespace VLC
{
    /// <summary>
    /// Tracks menu
    /// </summary>
    class TracksMenu
    {
        /// <summary>
        /// Gets or sets the menu flyout.
        /// </summary>
        public MenuFlyout MenuFlyout { get; set; }

        /// <summary>
        /// Gets or sets the available state name.
        /// </summary>
        public string AvailableStateName { get; set; }

        /// <summary>
        /// Gets or sets the unavailable state name.
        /// </summary>
        public string UnavailableStateName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the menu has a 'None' entry
        /// </summary>
        public bool HasNoneItem { get; set; }
    }
}
