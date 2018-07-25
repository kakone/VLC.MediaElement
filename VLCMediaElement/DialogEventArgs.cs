using System;
using System.Threading.Tasks;
using libVLCX;
using Nito.AsyncEx;

namespace VLC
{
    /// <summary>
    /// Event arguments for dialog box.
    /// </summary>
    public sealed class DialogEventArgs
#if CLASS_LIBRARY
        : EventArgs
#endif
    {
        /// <summary>
        /// Initializes a new instance of the DialogEventArgs class.
        /// </summary>
        /// <param name="title">title.</param>
        /// <param name="text">description.</param>
        /// <param name="qType">question type.</param>
        /// <param name="cancel">cancellation text.</param>
        /// <param name="action1">text for the first action.</param>
        /// <param name="action2">text for the second action.</param>
        public DialogEventArgs(string title, string text, Question qType, string cancel, string action1, string action2)
        {
            Title = title;
            Text = text;
            QuestionType = qType;
            Cancel = cancel;
            Action1 = action1;
            Action2 = action2;
        }

        private DeferralManager Deferrals { get; } = new DeferralManager();

        /// <summary>
        /// Gets the title of the dialog box.
        /// </summary>
        public string Title { get; private set; }

        /// <summary>
        /// Gets the description of the dialog box.
        /// </summary>
        public string Text { get; private set; }

        /// <summary>
        /// Gets the question type.
        /// </summary>
        public Question QuestionType { get; private set; }

        /// <summary>
        /// Gets the cancellation text.
        /// </summary>
        public string Cancel { get; private set; }

        /// <summary>
        /// Gets the text for the first action.
        /// </summary>
        public string Action1 { get; private set; }

        /// <summary>
        /// Gets the text for the second action.
        /// </summary>
        public string Action2 { get; private set; }

        /// <summary>
        /// Gets or sets the index of the selected action.
        /// </summary>
        public int? SelectedActionIndex { get; set; }

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
