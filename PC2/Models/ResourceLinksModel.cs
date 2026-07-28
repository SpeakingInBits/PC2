namespace PC2.Models;

public class ResourceLinksModel
{
    // Unique Identifier for each link.
    public int ResourceID { get; set; }

    // Unique link stored as a string.
    public string LinkURL { get; set; } = string.Empty;

    // A string containing the text that replaces the link text in the view.
    public string LinkText { get; set; } = string.Empty;

    // The first letter of the LinkText for ordering alphabetically.
    public char FirstLetter { get; set; }
}
