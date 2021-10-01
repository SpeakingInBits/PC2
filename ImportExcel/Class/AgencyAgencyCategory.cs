using System;
using System.Collections.Generic;

#nullable disable

namespace ImportExcel
{
    public partial class AgencyAgencyCategory
    {
        public int AgenciesAgencyId { get; set; }
        public int AgencyCategoriesAgencyCategoryId { get; set; }

        public virtual Agency AgenciesAgency { get; set; }
        public virtual AgencyCategory AgencyCategoriesAgencyCategory { get; set; }
    }
}
