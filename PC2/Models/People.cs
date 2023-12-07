using System.ComponentModel.DataAnnotations;

namespace PC2.Models
{
    /// <summary>
    /// Represents a person with basic information.
    /// </summary>
    public class People
    {
        [Key]
        public int ID { get; set; }

        /// <summary>
        /// The persons full name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Position title
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Used to sort People by their display priority. Lower number
        /// is a higher priority
        /// </summary>
        public byte PriorityOrder { get; set; }
    }

    /// <summary>
    /// Represents a staff member, inheriting basic information from <see cref="People"/>.
    /// </summary>
    public class Staff : People
    {
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
        public string Email { get; set; }

        /// <summary>
        /// Gets a formatted display of the phone number and extension.
        /// </summary>
        public string? PhoneDisplay
        {
            get
            {
                if (Phone != null && Extension != null)
                {
                    return $"{Phone} ext. {Extension}";
                }
                else if (Phone != null)
                {
                    return Phone;
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
