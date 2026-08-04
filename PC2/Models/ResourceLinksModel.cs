using System.ComponentModel.DataAnnotations;

namespace PC2.Models;

/// <summary>
/// Represents a resource link displayed on the /Resources/ResourceLinks page.
/// Resources point to external websites (https://...) or internal files (~/).
/// FirstLetter is auto-calculated from LinkText for alphabetical grouping.
/// </summary>
public class ResourceLinksModel
{
    /// <summary>
    /// Primary key (auto-incremented by database). Used for edit/delete operations.
    /// </summary>
    [Key]
    public int ResourceID { get; set; }

    /// <summary>
    /// Display text for the clickable link (required). Used for alphabetical sorting.
    /// Example: "Medicare Resources", "Autism Services"
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
    public string? Description { get; set; }

    /// <summary>
    /// First character of LinkText (auto-calculated). Used for alphabetical grouping.
    /// Example: LinkText "Medicare Resources" -> FirstLetter 'M'
    /// Auto-set by AddResourceLink() and UpdateResourceLink() methods.
    /// </summary>
    public char FirstLetter { get; set; }
}
