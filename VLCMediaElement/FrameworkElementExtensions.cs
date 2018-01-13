using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace VLC
{
    /// <summary>
    /// Extensions methods for <see cref="FrameworkElement"/> class.
    /// </summary>
    static class FrameworkElementExtensions
    {
        /// <summary>
        /// Find descendant control using its name.
        /// </summary>
        /// <param name="element">Parent element.</param>
        /// <param name="name">Name of the control to find.</param>
        /// <returns>Descendant control or null if not found.</returns>
        public static FrameworkElement FindDescendantByName(this FrameworkElement element, string name)
        {
            if (element == null || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            if (name.Equals(element.Name, StringComparison.OrdinalIgnoreCase))
            {
                return element;
            }

            var childCount = VisualTreeHelper.GetChildrenCount(element);
            for (var i = 0; i < childCount; i++)
            {
                var result = ((FrameworkElement)VisualTreeHelper.GetChild(element, i)).FindDescendantByName(name);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }
    }
}
