namespace PC2.Models
{
    public class ResourceGuideModel
    {
        public List<Agency> AgenciesForDataList = new List<Agency>();
        public List<AgencyCategory> AgencyCategories = new List<AgencyCategory>();
        public List<string> City = new List<string>();
        public List<Agency> Agencies = new List<Agency>();
        public AgencyCategory? Category {  get; set; }
        public string? AgencyCategoryName {  get; set; }
        public int YPos { get; set; } = 0;
        public string? CurrentCity {  get; set; }
        public string? SearchedAgency { get; set; }
        public string? SearchedCategory { get; set; }
        public string? SearchedCity { get; set; }
    }
}
