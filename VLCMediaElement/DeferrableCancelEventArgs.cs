using Nito.AsyncEx;
using System;
using System.Threading.Tasks;

namespace VLC
{
    /// <summary>
    /// Event arguments for cancellable deferrals
    /// </summary>
    public sealed class DeferrableCancelEventArgs
    {
        private DeferralManager Deferrals { get; } = new DeferralManager();

        /// <summary>
        /// Gets or sets a value indicating whether the event should be canceled.
        /// </summary>
        public bool Cancel { get; set; }

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
