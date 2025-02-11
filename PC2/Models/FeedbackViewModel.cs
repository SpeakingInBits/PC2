namespace PC2.Models
{
    /// <summary>
    /// ViewModel for displaying feedback information in the user interface.
    /// </summary>
    public class FeedbackViewModel
    {
        /// <summary>
        /// Gets or sets the unique identifier for the feedback entry.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the status of whether the user found the resource.
        /// This is displayed as a string ("Yes" or "No") instead of a boolean.
        /// </summary>
        public string FoundResource { get; set; }

        /// <summary>
        /// Gets or sets the comments provided by the user in the feedback.
        /// </summary>
        public string Comments { get; set; }

        /// <summary>
        /// Gets or sets the formatted submission date for display purposes.
        /// This ensures the date is shown in a user-friendly format.
        /// </summary>
        public string FormattedSubmittedAt { get; set; }
    }
}
