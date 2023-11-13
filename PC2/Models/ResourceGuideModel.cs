namespace PC2.Models
{
    public class ResourceGuideModel
    {
        /// <summary>
        /// A list of the agency data
        /// </summary>
        public List<Agency> AgenciesForDataList = new List<Agency>();
        /// <summary>
        /// A list of the agency category data
        /// </summary>
        public List<AgencyCategory> AgencyCategoriesForDataList = new List<AgencyCategory>();
        /// <summary>
        /// A list of the city data
        /// </summary>
        public List<string> CitiesForDataList = new List<string>();
        /// <summary>
        /// A list of agencies
        /// </summary>
        public List<Agency> Agencies = new List<Agency>();
        /// <summary>
        /// The Category that the user is searching by
        /// </summary>
        public AgencyCategory? Category {  get; set; }
        /*public string? AgencyCategoryName {  get; set; }*/
        /*public int YPos { get; set; } = 0;*/
        /// <summary>
        /// The city that the user is searching by
        /// </summary>
        public string? CurrentCity {  get; set; }
        /// <summary>
        /// The agency the user searched for
        /// </summary>
        public string? SearchedAgency { get; set; }
        /// <summary>
        /// The agency category the user searched for
        /// </summary>
        public string? SearchedCategory { get; set; }
        /// <summary>
        /// The city the user searched for
        /// </summary>
        public string? SearchedCity { get; set; }

        /// <summary>
        /// A string that tells us if the user searched by agency.
        /// If this value is null, the user searched by city or service.
        /// </summary>
        public string? UserSearchedByAgency { get; set; }

        /// <summary>
        /// A string that tells us if the user searched by city or service.
        /// If this value is null, the user searched by agency.
        /// </summary>
        public string? UserSearchedByCityOrService { get; set; }
    }
}
