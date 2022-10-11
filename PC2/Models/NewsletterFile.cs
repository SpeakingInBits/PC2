using System.ComponentModel.DataAnnotations;

namespace PC2.Models
{
    public class NewsletterFile
    {
        [Key] // for naming the newsletter
        public int NewsletterId { get; set; }

        [Required] // must be the filename
        public string? NewsletterName { get; set; }

        [Required] // used to create dynamic links
        public string? NewsletterFileLocation { get; set; }

    }
}
