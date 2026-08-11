using System.ComponentModel.DataAnnotations;

namespace PC2.Models;

/// <summary>
/// Represents a resource link displayed on the /Resources/ResourceLinks page.
/// Resources point to external websites (https://...) or internal files (~/).
/// </summary>
public class ResourceLinksModel
{
    /// <summary>
    /// Primary key (auto-incremented by database). Used for edit/delete operations.
    /// </summary>
    [Key]
    public int ResourceID { get; set; }

    /// <summary>
    /// URL/path for the resource (required).
    /// Examples: \"https://example.com\", \"~/PDF/ResourceLinks/Reduced-Cost-Service-Guide-May-2023.pdf\"
    /// </summary>
    public string LinkURL { get; set; } = string.Empty;

    /// <summary>
    /// Display text for the clickable link (required). Used for alphabetical sorting.
    /// Example: "Medicare Resources", "Autism Services"
    /// </summary>
    public string LinkText { get; set; } = string.Empty;

    /// <summary>
    /// Optional description displayed below the link.
    /// Example: "Information about Medicare enrollment and benefits"
    /// </summary>
    [MaxLength(100)]
    public string? Description { get; set; }
}
