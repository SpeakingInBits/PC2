using System.ComponentModel.DataAnnotations;

namespace PC2.Models
{
    /// <summary>
    /// Represents a agency category which may contain many agencies
    /// </summary>
    public class AgencyCategory
    {
        /// <summary>
        /// The unique identifier for AgencyCategory
        /// </summary>
        [Key]
        public int AgencyCategoryId {  get; set; }

        /// <summary>
        /// The agency category's name
        /// </summary>
        public string AgencyCategoryName {  get; set; }

        /// <summary>
        /// A list of agencies
        /// </summary>
        public List<Agency> Agencies {  get; set; } = new List<Agency>();
    }
}
