using System;
using System.Threading.Tasks;
using Nito.AsyncEx;

namespace VLC
{
    /// <summary>
    /// Event arguments for login dialog box.
    /// </summary>
    public sealed class LoginDialogEventArgs
#if CLASS_LIBRARY
        : EventArgs
#endif
    {
        /// <summary>
        /// Initializes a new instance of LoginDialogEventArgs class.
        /// </summary>
        /// <param name="title">title.</param>
        /// <param name="text">description.</param>
        /// <param name="defaultUsername">default username.</param>
        /// <param name="askToStoreCredentials">true if the user must be asked to store credentials, false otherwise.</param>
        public LoginDialogEventArgs(string title, string text, string defaultUsername, bool askToStoreCredentials)
        {
            Title = title;
            Text = text;
            DefaultUsername = defaultUsername;
            AskToStoreCredentials = askToStoreCredentials;
        }

        private DeferralManager Deferrals { get; } = new DeferralManager();

        /// <summary>
        /// Gets the title of the login dialog box.
        /// </summary>
        public string Title { get; private set; }

        /// <summary>
        /// Gets the description of the login dialog box.
        /// </summary>
        public string Text { get; private set; }

        /// <summary>
        /// Gets the default username.
        /// </summary>
        public string DefaultUsername { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the user must be asked to store credentials or not.
        /// </summary>
        public bool AskToStoreCredentials { get; private set; }

        /// <summary>
        /// Dialog result.
        /// </summary>
        public LoginDialogResult DialogResult { get; set; }

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
