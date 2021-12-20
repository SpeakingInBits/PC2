using System.ComponentModel.DataAnnotations;

namespace PC2.Models
{
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
}
