using Microsoft.EntityFrameworkCore;
using PC2.Models;

namespace PC2.Data
{
    public static class AgencyCategoryDB
    {
        /// <summary>
        /// Adds a category to the database
        /// </summary>
        /// <param name="context"></param>
        /// <param name="agencyCategory">The category to be added</param>
        public static async Task AddCategoryAsync(ApplicationDbContext context, AgencyCategory agencyCategory)
        {
            context.AgencyCategory.Add(agencyCategory);
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Gets all the categories in the database
        /// </summary>
        /// <param name="context"></param>
        public static async Task<List<AgencyCategory>> GetAgencyCategoriesAsync(ApplicationDbContext context)
        {
            return await (from a in context.AgencyCategory
                          select a).ToListAsync();
        }

        public static async Task<AgencyCategory> GetAgencyCategory(ApplicationDbContext context, int categoryID)
        {
            return await (from a in context.AgencyCategory
                          where a.AgencyCategoryId == categoryID
                          select a).FirstOrDefaultAsync();
        }
    }
}
