namespace PC2.Models
{
    /// <summary>
    /// View model for the About Us page
    /// </summary>
    public class AboutUsViewModel
    {
        /// <summary>
        /// Collection of Staff members
        /// </summary>
        public List<Staff> Staff { get; set; } = [];

        /// <summary>
        /// Collection of Board members
        /// </summary>
        public List<Board> Board { get; set; } = [];

        /// <summary>
        /// Collection of Steering Committee members. Currently unused by the client
        /// but reserved for future use.
        /// </summary>
        public List<SteeringCommittee> SteeringCommittee { get; set; } = [];
    }
}
