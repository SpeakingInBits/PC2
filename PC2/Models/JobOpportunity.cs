using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PC2.Models
{
    /// <summary>
    /// Represents a job opportunity posting
    /// </summary>
    public class JobOpportunity
    {
        /// <summary>
        /// The unique identifier for the job opportunity
        /// </summary>
        [Key]
        public int JobOpportunityId { get; set; }

        /// <summary>
        /// The title of the job
        /// </summary>
        [Required]
        public string Title { get; set; } = null!;

        /// <summary>
        /// Description of the job
        /// </summary>
        [Required]
        public string Description { get; set; } = null!;

        /// <summary>
        /// Optional date that applications close. When set, the job no longer
        /// appears on the public listing after this date passes.
        /// </summary>
        [Display(Name = "Closing Date")]
        [DataType(DataType.Date)]
        public DateOnly? ClosingDate { get; set; }

        /// <summary>
        /// True when the job has been manually closed by an Admin or Staff member
        /// </summary>
        public bool IsClosed { get; set; }

        /// <summary>
        /// Optional URL to an attachment stored in Azure Blob Storage
        /// </summary>
        public string? AttachmentLocation { get; set; }

        /// <summary>
        /// Optional display name for the attachment
        /// </summary>
        public string? AttachmentName { get; set; }

        /// <summary>
        /// True when the job is visible on the public listing: not manually
        /// closed and the closing date (if any) has not passed
        /// </summary>
        [NotMapped]
        public bool IsOpen
        {
            get
            {
                return !IsClosed &&
                    (ClosingDate == null || ClosingDate >= DateOnly.FromDateTime(DateTime.Today));
            }
        }
    }

    /// <summary>
    /// View model for displaying job opportunities with sanitized HTML descriptions.
    /// </summary>
    public class JobOpportunityViewModel
    {
        /// <summary>
        /// The original job opportunity data
        /// </summary>
        public JobOpportunity Job { get; set; } = null!;

        /// <summary>
        /// HTML-encoded job description with clickable links for URLs, emails, and phone numbers.
        /// Safe to render using @Html.Raw()
        /// </summary>
        public string SanitizedDescription { get; set; } = string.Empty;
    }
}
