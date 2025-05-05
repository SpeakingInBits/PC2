using System.ComponentModel.DataAnnotations;

namespace PC2.Models
{
    /// <summary>
    /// Represents user feedback submitted on the website.
    /// </summary>
    public class Feedback
    {
        /// <summary>
        /// Gets or sets the unique identifier for the feedback entry.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user found the resource they were looking for.
        /// True represents "Yes", and False represents "No".
        /// </summary>
        public bool IsResourceFound { get; set; }

        /// <summary>
        /// Gets or sets the optional comments provided by the user.
        /// </summary>
        [MaxLength(500)]
        public string? Comments { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the feedback was submitted.
        /// </summary>
        public DateTime SubmittedAt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the feedback has been reviewed by an administrator.
        /// </summary>
        public bool IsReviewed { get; set; } = false;
    }
}
