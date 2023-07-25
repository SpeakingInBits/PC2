namespace PC2.Models
{
    /// <summary>
    /// Represents the contact request from a website visitor
    /// </summary>
    public class ContactPageModel
    {
        /// <summary>
        /// The contact's name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The contact's email address
        /// </summary>
        public string Email { get; set; }
        /// <summary>
        /// The contact's phone number
        /// </summary>
        public string Phone { get; set; }
        /// <summary>
        /// The subject of the message
        /// </summary>
        public string Subject { get; set; }
        /// <summary>
        /// The message written for the contact by a website visitor
        /// </summary>
        public string Message { get; set; }
    }
}
