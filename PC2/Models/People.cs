using System.ComponentModel.DataAnnotations;

namespace PC2.Models
{
    public class People
    {
        [Key]
        public int ID { get; set; }
        public string Name { get; set; }

        public string? Title { get; set; }
    }
}
