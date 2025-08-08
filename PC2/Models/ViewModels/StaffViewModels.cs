using System.ComponentModel.DataAnnotations;

namespace PC2.Models.ViewModels
{
    /// <summary>
    /// ViewModel for creating a new staff member with file upload support
    /// </summary>
    public class CreateStaffViewModel
    {
        /// <summary>
        /// The person's full name
        /// </summary>
        [Required]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Position title
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// The staff/person's phone number.
        /// </summary>
        public string? Phone { get; set; }

        /// <summary>
        /// The phone extension.
        /// </summary>
        public int? Extension { get; set; }

        /// <summary>
        /// The staff/person's email address.
        /// </summary>
        [Required]
        [DataType(DataType.EmailAddress)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Photo file upload
        /// </summary>
        [Display(Name = "Photo")]
        public IFormFile? PhotoFile { get; set; }

        /// <summary>
        /// Alternative: Photo URL (if not uploading a file)
        /// </summary>
        [DataType(DataType.Url)]
        [Display(Name = "Photo URL (if not uploading file)")]
        public string? ImageUrl { get; set; }

        /// <summary>
        /// Used to sort People by their display priority. Lower number
        /// is a higher priority
        /// </summary>
        [Required]
        [Display(Name = "Sort Priority")]
        public byte PriorityOrder { get; set; } = 10;
    }

    /// <summary>
    /// ViewModel for editing an existing staff member with file upload support
    /// </summary>
    public class EditStaffViewModel
    {
        public int ID { get; set; }

        /// <summary>
        /// The person's full name
        /// </summary>
        [Required]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Position title
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// The staff/person's phone number.
        /// </summary>
        public string? Phone { get; set; }

        /// <summary>
        /// The phone extension.
        /// </summary>
        public int? Extension { get; set; }

        /// <summary>
        /// The staff/person's email address.
        /// </summary>
        [Required]
        [DataType(DataType.EmailAddress)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Current photo URL
        /// </summary>
        public string? CurrentImageUrl { get; set; }

        /// <summary>
        /// New photo file upload
        /// </summary>
        [Display(Name = "New Photo")]
        public IFormFile? PhotoFile { get; set; }

        /// <summary>
        /// Alternative: Photo URL (if not uploading a file)
        /// </summary>
        [DataType(DataType.Url)]
        [Display(Name = "Photo URL (if not uploading file)")]
        public string? ImageUrl { get; set; }

        /// <summary>
        /// Whether to remove the current photo
        /// </summary>
        [Display(Name = "Remove current photo")]
        public bool RemovePhoto { get; set; }

        /// <summary>
        /// Used to sort People by their display priority. Lower number
        /// is a higher priority
        /// </summary>
        [Required]
        [Display(Name = "Sort Priority")]
        public byte PriorityOrder { get; set; } = 10;
    }
}