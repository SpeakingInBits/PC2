using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PC2.Models
{
    public class HousingProgram
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Display(Name = "Household Size")]
        public int HouseHoldSize { get; set; }

        [Display(Name="Maximum Income")]
        [Range(0, double.MaxValue)]
        [DataType(DataType.Currency)]
        public double MaximumIncome { get; set; }

        [DataType(DataType.Date)]
        public DateTime LastUpdated { get; set; }
    }
}
