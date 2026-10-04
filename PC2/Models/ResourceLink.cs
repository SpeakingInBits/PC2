using System.ComponentModel.DataAnnotations;

namespace PC2.Models
{
    /// <summary>
    /// Represents a link shown on the Resource Links page. A link points to
    /// either an external website or a PDF uploaded to Azure Blob Storage.
    /// </summary>
    public class ResourceLink
    {
        /// <summary>
        /// The unique identifier for the resource link
        /// </summary>
        [Key]
        public int ResourceLinkId { get; set; }

        /// <summary>
        /// The text displayed for the link
        /// </summary>
        [Required]
        [StringLength(200)]
        [Display(Name = "Link Text")]
        public string Name { get; set; } = null!;

        /// <summary>
        /// The website address, or the location of the uploaded file in Azure Blob Storage
        /// </summary>
        [Display(Name = "Website Address")]
        public string Url { get; set; } = null!;

        /// <summary>
        /// Optional description shown below the link
        /// </summary>
        [StringLength(1000)]
        public string? Description { get; set; }

        /// <summary>
        /// The name of the uploaded file. Null when the link points to a website.
        /// </summary>
        public string? FileName { get; set; }

        /// <summary>
        /// Returns true when the URL is a full http/https address.
        /// Other schemes such as javascript: are rejected so they can't be placed in a link.
        /// </summary>
        /// <param name="url">The URL entered by the user.</param>
        public static bool IsValidUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            return Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }
    }
}
