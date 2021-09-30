using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImportExcel
{
    internal class AgencyCategoryDB
    {
        /// <summary>
        /// Adds a category to the database
        /// </summary>
        /// <param name="context"></param>
        /// <param name="agencyCategory">The category to be added</param>
        public static async Task AddCategoryAsync(AgencyCategory agencyCategory)
        {
            using (PC2Context context = new PC2Context())
            {
                context.AgencyCategories.Add(agencyCategory);
                await context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Gets all the categories in the database
        /// </summary>
        /// <param name="context"></param>
        public static async Task<List<AgencyCategory>> GetAgencyCategoriesAsync()
        {
            using (PC2Context context = new PC2Context())
            {
                return await (from a in context.AgencyCategories
                              select a).ToListAsync();
            }
        }
    }
}
