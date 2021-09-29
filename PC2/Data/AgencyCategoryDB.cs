using Microsoft.EntityFrameworkCore;
using PC2.Models;

namespace PC2.Data
{
    public static class AgencyCategoryDB
    {
        public static async Task AddCategoryAsync(ApplicationDbContext context, AgencyCategory agencyCategory)
        {
            context.AgencyCategory.Add(agencyCategory);
            await context.SaveChangesAsync();
        }

        public static async Task<List<AgencyCategory>> GetAgencyCategoriesAsync(ApplicationDbContext context)
        {
            return await (from a in context.AgencyCategory
                          select a).ToListAsync();
        }
    }
}
