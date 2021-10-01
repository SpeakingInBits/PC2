using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImportExcel
{
    internal class AgencyDB
    {
        /// <summary>
        /// Adds an Agency to the database
        /// </summary>
        /// <param name="context"></param>
        /// <param name="agency">The agency to be added</param>
        public static async Task AddAgencyAsync(Agency agency)
        {
            using (PC2Context context  = new PC2Context())
            {
                context.Agencies.Add(agency);
                await context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Gets all Agencies from the database
        /// </summary>
        public static async Task<List<Agency>> GetAllAgencyAsync(PC2Context context)
        {
            return await (from a in context.Agencies
                          select a).ToListAsync();
        }

        /// <summary>
        /// Updates the Agency with the categories that belong to it
        /// </summary>
        /// <param name="context"></param>
        /// <param name="agency">The Agency being updated</param>
        /// <param name="categories">The categories being added to the agency</param>
        public static async Task UpdateAgencyAsync(PC2Context context, Agency agency, List<AgencyCategory> categories)
        {
            for (int i = 0; i < categories.Count; i++)
            {
                context.AgencyCategories.Attach(agency.AgencyAgencyCategories.ElementAt(i).AgencyCategoriesAgencyCategory);
            }
            context.Entry(agency).State = EntityState.Modified;
            await context.SaveChangesAsync();
        }

        public static async Task UpdateRelationships(AgencyAgencyCategory agencyCategory)
        {
            using (PC2Context context = new PC2Context())
            {
                context.AgencyAgencyCategories.Add(agencyCategory);
                await context.SaveChangesAsync();
            }
        }
    }
}
