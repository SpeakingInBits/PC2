using System.ComponentModel.DataAnnotations;

namespace PC2.Models
{
    /// <summary>
    /// Represents the NewsLetterFile
    /// </summary>
    public class NewsletterFile
    {
        /// <summary>
        /// The unique identifier for the NewsLetter
        /// </summary>
        [Key]  
        public int NewsletterId { get; set; }

        /// <summary>
        /// The name of the NewsLetterFile
        /// </summary>
        [Required] 
        public string Name { get; set; } = null!;

        /// <summary>
        /// The location of the NewsLetterFile in the storage device
        /// </summary>
        [Required] 
        public string Location { get; set; } = null!;

    }
}
