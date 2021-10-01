using System;
using System.Collections.Generic;

#nullable disable

namespace ImportExcel
{
    public partial class AgencyCategory
    {
        public AgencyCategory()
        {
            AgencyAgencyCategories = new HashSet<AgencyAgencyCategory>();
        }

        public int AgencyCategoryId { get; set; }
        public string AgencyCategoryName { get; set; }

        public virtual ICollection<AgencyAgencyCategory> AgencyAgencyCategories { get; set; }
    }
}
