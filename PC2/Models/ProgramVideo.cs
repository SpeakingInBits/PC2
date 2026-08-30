using System.ComponentModel.DataAnnotations;

namespace PC2.Models
{
    /// <summary>
    /// Represents a program video entry
    /// </summary>
    public class ProgramVideo
    {
        /// <summary>
        /// The unique identifier for the program video
        /// </summary>
        [Key]
        public int ProgramVideoId { get; set; }

        /// <summary>
        /// The display title of the video
        /// </summary>
        [Required]
        public string Title { get; set; } = null!;

        /// <summary>
        /// The YouTube video ID (e.g. "tRadgduogZg")
        /// </summary>
        [Required]
        public string YouTubeVideoId { get; set; } = null!;

        /// <summary>
        /// Optional URL to a PDF attachment stored in Azure Blob Storage
        /// </summary>
        public string? PdfLocation { get; set; }

        /// <summary>
        /// Optional display name for the PDF attachment
        /// </summary>
        public string? PdfName { get; set; }
    }
}
