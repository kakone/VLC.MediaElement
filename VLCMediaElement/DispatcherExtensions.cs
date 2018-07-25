using System;
using System.Threading.Tasks;
using Windows.UI.Core;

namespace VLC
{
    /// <summary>
    /// Extensions methods for <see cref="CoreDispatcher"/> class
    /// </summary>
    internal static class DispatcherExtensions
    {
        /// <summary>
        /// Schedules the provided callback on the UI thread from a worker thread, and returns the results asynchronously
        /// </summary>
        /// <param name="dispatcher">event message dispatcher</param>
        /// <param name="agileCallback">the callback on which the dispatcher returns when the event is dispatched</param>
        public static async Task RunAsync(this CoreDispatcher dispatcher, DispatchedHandler agileCallback)
        {
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, agileCallback);
        }
    }
}
