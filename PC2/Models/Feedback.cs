using System.ComponentModel.DataAnnotations;

namespace PC2.Models
{
    public class Feedback
    {
        public int Id { get; set; } // Primary key

        public bool FoundResource { get; set; } // True for "Yes", False for "No"

        [MaxLength(500)]
        public string? Comments { get; set; } // Optional comments from the user

        public DateTime SubmittedAt { get; set; } // Timestamp when feedback is submitted
    }
}
