namespace VLC
{
    /// <summary>
    /// Result for the login dialog box.
    /// </summary>
    public sealed class LoginDialogResult
    {
        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the credentials must be stored or not.
        /// </summary>
        public bool StoreCredentials { get; set; }
    }
}
