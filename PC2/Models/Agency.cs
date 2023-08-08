using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace PC2.Models
{
    /// <summary>
    /// Represents a agency as a object
    /// </summary>
    public class Agency
    {
        /// <summary>
        /// The Agency's unique identifier
        /// </summary>
        [Key]
        public int AgencyId { get; set; }

        /// <summary>
        /// The agency's name
        /// </summary>
        [Required]
        public string AgencyName {  get; set;}

        /// <summary>
        /// The agency's second name
        /// </summary>
        public string? AgencyName2 {  get; set;}

        /// <summary>
        /// The agency's contact information
        /// </summary>
        public string? Contact {  get; set;}

        /// <summary>
        /// The agency's first address
        /// </summary>
        public string? Address1 {  get; set;}

        /// <summary>
        /// The agency's second address
        /// </summary>
        public string? Address2 {  get; set;}

        /// <summary>
        /// The city the agency's is located
        /// </summary>
        public string? City {  get; set;}

        /// <summary>
        /// The state the agency is located
        /// </summary>
        public string? State {  get; set;}

        /// <summary>
        /// The agency's zip code
        /// </summary>
        public string? Zip {  get; set;}

        /// <summary>
        /// The agency's mailing address
        /// </summary>
        public string? MailingAddress {  get; set;}

        /// <summary>
        /// The agency's phone number
        /// </summary>
        public string? Phone {  get; set;}

        /// <summary>
        /// The agency's toll free phone number
        /// </summary>
        public string? TollFree {  get; set;}

        /// <summary>
        /// The agency's teletypewriter number
        /// </summary>
        public string? TTY {  get; set;}

        /// <summary>
        /// The agency's telecommunications device for the deaf's number
        /// </summary>
        public string? TDD {  get; set;}

        /// <summary>
        /// The crisis help line phone number
        /// </summary>
        public string? CrisisHelpHotline {  get; set;}

        /// <summary>
        /// The agency's fax number
        /// </summary>
        public string? Fax { get; set;}

        /// <summary>
        /// The agency's email address
        /// </summary>
        public string? Email {  get; set;}

        /// <summary>
        /// The agency's personal website
        /// </summary>
        public string? Website { get; set;}

        /// <summary>
        /// The agency's general description
        /// </summary>
        public string? Description {  get; set;}

        public List<AgencyCategory> AgencyCategories { get; set; } = new List<AgencyCategory>();
        /// <summary>
        /// Creates a formated string to print from the Phone field
        /// </summary>
        /// <returns>A formated phone number string to be displayed</returns>
        public string PhoneToString()
        {
            Regex regex = new Regex(@"^\(?\d{3}\)?[\s.-]\d{3}[\s.-]\d{4}[\s]?$");
            if (regex.IsMatch(Phone))
            {
                return "tel:" + Phone;
            }
            else
            {
                return Phone;
            }
        }
        /// <summary>
        /// Creates a formated string to print from the CrisisHelpHotline field
        /// </summary>
        /// <returns>A formated CrisisHelpHotline phone number to be displayed</returns>
        public string CrisisToString()
        {
            Regex regex = new Regex(@"^\(?\d{3}\)?[\s.-]\d{3}[\s.-]\d{4}$");
            if (regex.IsMatch(CrisisHelpHotline))
            {
                return "tel:" + CrisisHelpHotline;
            }
            else
            {
                return CrisisHelpHotline;
            }
        }
    }
    /// <summary>
    /// Represents the information to be displayed by a view for the agency class
    /// </summary>
    public class AgencyDisplayViewModel
    {
        /// <summary>
        /// The Agency's unique identifier
        /// </summary>
        public int AgencyId { get; set; }

        /// <summary>
        /// The agency's name
        /// </summary>
        public string AgencyName { get; set; } = null!;

        /// <summary>
        /// The agency's contact information
        /// </summary>
        public string? AgencyName2 { get; set; }

        /// <summary>
        /// The city the agency's is located
        /// </summary>
        public string? City { get; set; }
    }
}
