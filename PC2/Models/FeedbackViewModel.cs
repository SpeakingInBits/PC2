namespace PC2.Models
{
    public class FeedbackViewModel
    {
        public int Id { get; set; }

        // Convert the boolean FoundResource to a string ("Yes"/"No") for display
        public string FoundResource { get; set; }

        public string Comments { get; set; }

        // Format the date for better readability
        public string FormattedSubmittedAt { get; set; }
    }
}
