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
        [Required]
        [StringLength(MaxNameLength)]
        [Display(Name = "Category Name")]
        public string AgencyCategoryName {  get; set; } = null!;

        /// <summary>
        /// A list of agencies
        /// </summary>
        public List<Agency> Agencies {  get; set; } = new List<Agency>();

        /// <summary>
        /// The maximum number of characters allowed in a category name
        /// </summary>
        public const int MaxNameLength = 100;
    }

    /// <summary>
    /// Represents a category shown on the Agency Categories management page
    /// </summary>
    public class AgencyCategoryDisplayViewModel
    {
        /// <summary>
        /// The category's unique identifier
        /// </summary>
        public int AgencyCategoryId { get; set; }

        /// <summary>
        /// The category's name
        /// </summary>
        public string AgencyCategoryName { get; set; } = null!;

        /// <summary>
        /// The number of agencies listed under the category
        /// </summary>
        public int AgencyCount { get; set; }
    }
}
