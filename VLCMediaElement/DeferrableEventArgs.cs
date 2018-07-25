using System;
using System.Threading.Tasks;
using Nito.AsyncEx;

namespace VLC
{
    /// <summary>
    /// Event arguments for deferrals
    /// </summary>
    public sealed class DeferrableEventArgs
#if CLASS_LIBRARY
        : EventArgs
#endif
    {
        private DeferralManager Deferrals { get; } = new DeferralManager();

        /// <summary>
        /// Requests a deferral. When the deferral is disposed, it is considered complete.
        /// </summary>
        /// <returns>a deferral.</returns>
        public IDisposable GetDeferral()
        {
            return Deferrals.GetDeferral();
        }

        /// <summary>
        /// Waits until the deferrals are completed.
        /// </summary>
        /// <returns>a task that is completed when all deferrals have completed.</returns>
        internal Task WaitForDeferralsAsync()
        {
            return Deferrals.SignalAndWaitAsync();
        }
    }
}
