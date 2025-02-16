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
        /// Gets or sets the status of whether the user found the resource. This is a required field.
        /// This value is displayed as a string ("Yes" or "No") instead of a boolean.
        /// </summary>
        public required string IsResourceFound { get; set; }

        /// <summary>
        /// Gets or sets the optional comments provided by the user in the feedback.
        /// This value can be null if no comments were provided.
        /// </summary>
        public string? Comments { get; set; }

        /// <summary>
        /// Gets or sets the formatted submission date for display purposes.
        /// This ensures the date is shown in a user-friendly format.
        /// This value is required for tracking and sorting purposes.
        /// </summary>
        public required string FormattedSubmittedAt { get; set; }
    }
}
