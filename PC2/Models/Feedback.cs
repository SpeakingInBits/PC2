using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PC2.Models
{
    /// <summary>
    /// Feedback left by a visitor after searching the Resource Guide
    /// </summary>
    public class Feedback
    {
        public const int CommentsMaxLength = 1000;
        public const int SearchedForMaxLength = 300;

        /// <summary>
        /// The unique identifier for the feedback
        /// </summary>
        [Key]
        public int FeedbackId { get; set; }

        /// <summary>
        /// The visitor's answer to "Did you find what you were looking for?"
        /// </summary>
        [Display(Name = "Found what they were looking for")]
        public bool IsResourceFound { get; set; }

        /// <summary>
        /// Optional comments from the visitor
        /// </summary>
        [MaxLength(CommentsMaxLength)]
        public string? Comments { get; set; }

        /// <summary>
        /// A description of the search the visitor performed, e.g. "Service: Respite Care, City: Tacoma"
        /// </summary>
        [Display(Name = "Searched For")]
        [MaxLength(SearchedForMaxLength)]
        public string? SearchedFor { get; set; }

        /// <summary>
        /// When the feedback was submitted, in UTC
        /// </summary>
        [Display(Name = "Submitted")]
        public DateTime SubmittedAt { get; set; }

        /// <summary>
        /// When an Admin or Staff member marked the feedback as reviewed on the website, in UTC.
        /// Reviewed feedback is not included in the weekly digest email.
        /// </summary>
        [Display(Name = "Reviewed")]
        public DateTime? ReviewedAt { get; set; }

        /// <summary>
        /// The weekly digest email this feedback was included in, or null if it has not been emailed
        /// </summary>
        public int? FeedbackDigestId { get; set; }

        /// <summary>
        /// The weekly digest email this feedback was included in
        /// </summary>
        public FeedbackDigest? FeedbackDigest { get; set; }

        /// <summary>
        /// True when the feedback has already been seen by the client, either in a digest email
        /// or by being marked as reviewed on the website
        /// </summary>
        [NotMapped]
        public bool IsHandled => ReviewedAt != null || FeedbackDigestId != null;
    }

    /// <summary>
    /// A record of a weekly feedback digest email that was sent
    /// </summary>
    public class FeedbackDigest
    {
        /// <summary>
        /// The unique identifier for the digest
        /// </summary>
        [Key]
        public int FeedbackDigestId { get; set; }

        /// <summary>
        /// When the digest email was sent, in UTC
        /// </summary>
        public DateTime SentAt { get; set; }

        /// <summary>
        /// The email address the digest was sent to
        /// </summary>
        [MaxLength(256)]
        public string SentTo { get; set; } = null!;

        /// <summary>
        /// The feedback included in the digest
        /// </summary>
        public List<Feedback> Feedback { get; set; } = new();
    }

    /// <summary>
    /// The data a visitor submits from the Resource Guide feedback form
    /// </summary>
    public class FeedbackSubmission
    {
        /// <summary>
        /// The visitor's answer to "Did you find what you were looking for?"
        /// </summary>
        [Required(ErrorMessage = "Please let us know if you found what you were looking for.")]
        public bool? IsResourceFound { get; set; }

        /// <summary>
        /// Optional comments from the visitor
        /// </summary>
        [MaxLength(Feedback.CommentsMaxLength)]
        public string? Comments { get; set; }

        /// <summary>
        /// A description of the search the visitor performed. Truncated rather than validated because it
        /// includes text the visitor typed into the search form.
        /// </summary>
        public string? SearchedFor { get; set; }

        /// <summary>
        /// The Google reCAPTCHA v3 token generated when the form was submitted
        /// </summary>
        public string? ReCaptchaToken { get; set; }

        /// <summary>
        /// The token from the "I'm not a robot" checkbox, sent instead of a v3 token after a low score
        /// </summary>
        public string? ReCaptchaCheckboxToken { get; set; }
    }

    /// <summary>
    /// View model for the Admin/Staff feedback management page
    /// </summary>
    public class ManageFeedbackViewModel
    {
        /// <summary>
        /// The feedback to display
        /// </summary>
        public List<Feedback> Feedback { get; set; } = new();

        /// <summary>
        /// True when all feedback is shown; false when only feedback that has not been
        /// emailed or reviewed is shown
        /// </summary>
        public bool ShowAll { get; set; }

        /// <summary>
        /// The number of feedback entries that have not been emailed or reviewed yet
        /// </summary>
        public int NewFeedbackCount { get; set; }

        /// <summary>
        /// The most recent digest email that was sent, if any
        /// </summary>
        public FeedbackDigest? LastDigest { get; set; }

        /// <summary>
        /// When the digest email is sent automatically, e.g. "Sunday at 8 AM", or null if automatic emails are disabled
        /// </summary>
        public string? DigestSchedule { get; set; }

        /// <summary>
        /// The time zone used to display dates
        /// </summary>
        public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Utc;
    }
}
