using System.ComponentModel.DataAnnotations;

namespace PC2.Models
{
    public class AgencyCategory
    {
        [Key]
        public int AgencyCategoryId {  get; set; }
        
        public string AgencyCategoryName {  get; set; }

        public List<Agency> Agencies {  get; set; } = new List<Agency>();
    }
}
