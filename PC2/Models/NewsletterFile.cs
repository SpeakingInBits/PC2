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
        [Key] // for naming the newsletter  
        public int NewsletterId { get; set; }

        /// <summary>
        /// The name of the NewsLetterFile
        /// </summary>
        [Required] // must be the filename
        public string Name { get; set; } = null!;

        /// <summary>
        /// The location of the NewsLetterFile in the storage device
        /// </summary>
        [Required] // used to create dynamic links
        public string Location { get; set; } = null!;

    }
}
