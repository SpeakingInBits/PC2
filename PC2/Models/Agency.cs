using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace PC2.Models
{
    /// <summary>
    /// Represents an agency as an object
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
        [Display(Name = "Agency name")]
        [Required]
        public string AgencyName {  get; set;}

        /// <summary>
        /// The agency's second name
        /// </summary>
        [Display(Name = "Secondary agency name")]
        public string? AgencyName2 {  get; set;}

        /// <summary>
        /// The agency's contact information
        /// </summary>
        public string? Contact {  get; set;}

        /// <summary>
        /// The agency's first address
        /// </summary>
        [Display(Name = "Address line 1")]
        public string? Address1 {  get; set;}

        /// <summary>
        /// The agency's second address
        /// </summary>
        [Display(Name = "Address line 2")]
        public string? Address2 {  get; set;}

        /// <summary>
        /// The city where the agency is located
        /// </summary>
        public string? City {  get; set;}

        /// <summary>
        /// The state where the agency is located
        /// </summary>
        public string? State {  get; set;}

        /// <summary>
        /// The agency's zip code
        /// </summary>
        [Display(Name = "ZIP code")]
        public string? Zip {  get; set;}

        /// <summary>
        /// The agency's mailing address
        /// </summary>
        [Display(Name = "Mailing address")]
        public string? MailingAddress {  get; set;}

        /// <summary>
        /// The agency's phone number
        /// </summary>
        public string? Phone {  get; set;}

        /// <summary>
        /// The agency's toll free phone number
        /// </summary>
        [Display(Name = "Toll-free phone")]
        public string? TollFree {  get; set;}

        /// <summary>
        /// The agency's teletypewriter number
        /// </summary>
        [Display(Name = "TTY")]
        public string? TTY {  get; set;}

        /// <summary>
        /// The agency's Telecommunications Device for the Deaf (TDD) number
        /// </summary>
        [Display(Name = "TDD")]
        public string? TDD {  get; set;}

        /// <summary>
        /// The crisis help line phone number
        /// </summary>
        [Display(Name = "Crisis help hotline")]
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
        /// Creates a formatted string to print from the Phone field
        /// </summary>
        /// <returns>A formatted phone number string to be displayed</returns>
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
        /// Creates a formatted string to print from the CrisisHelpHotline field
        /// </summary>
        /// <returns>A formatted CrisisHelpHotline phone number to be displayed</returns>
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
        /// The agency's second name
        /// </summary>
        public string? AgencyName2 { get; set; }

        /// <summary>
        /// The city where the agency is located
        /// </summary>
        public string? City { get; set; }
    }
}
