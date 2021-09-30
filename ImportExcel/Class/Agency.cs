﻿using System;
using System.Collections.Generic;

#nullable disable

namespace ImportExcel
{
    public partial class Agency
    {
        public Agency()
        {
            AgencyAgencyCategories = new HashSet<AgencyAgencyCategory>();
        }

        public int AgencyId { get; set; }
        public string AgencyName { get; set; }
        public string AgencyName2 { get; set; }
        public string Contact { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string MailingAddress { get; set; }
        public string Phone { get; set; }
        public string TollFree { get; set; }
        public string Tty { get; set; }
        public string Tdd { get; set; }
        public string CrisisHelpHotline { get; set; }
        public string Fax { get; set; }
        public string Email { get; set; }
        public string Website { get; set; }
        public string Description { get; set; }

        public virtual ICollection<AgencyAgencyCategory> AgencyAgencyCategories { get; set; }
    }
}
