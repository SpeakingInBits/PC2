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
        /// Gets all the categories in the database in alphabetical order
        /// </summary>
        /// <param name="context"></param>
        public static async Task<List<AgencyCategory>> GetAgencyCategoriesAsync(ApplicationDbContext context)
        {
            return await (from a in context.AgencyCategory
                          select a).OrderBy(agencyCat => agencyCat.AgencyCategoryName).ToListAsync();
        }

        /// <summary>
        /// Gets Category based off of an ID
        /// </summary>
        /// <param name="context"></param>
        /// <param name="categoryID"></param>
        /// <returns></returns>
        public static async Task<AgencyCategory?> GetAgencyCategory(ApplicationDbContext context, int categoryID)
        {
            return await (from a in context.AgencyCategory
                          where a.AgencyCategoryId == categoryID
                          select a).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Gets category based off of category name
        /// </summary>
        /// <param name="context"></param>
        /// <param name="category"></param>
        /// <returns></returns>
        public static async Task<AgencyCategory?> GetAgencyCategory(ApplicationDbContext context, string category)
        {
            return await (from a in context.AgencyCategory
                          where a.AgencyCategoryName == category
                          select a).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Gets all the categories in alphabetical order with the number of agencies in each
        /// </summary>
        /// <param name="context"></param>
        public static async Task<List<AgencyCategoryDisplayViewModel>> GetCategoriesWithAgencyCountsAsync(ApplicationDbContext context)
        {
            return await (from a in context.AgencyCategory
                          orderby a.AgencyCategoryName
                          select new AgencyCategoryDisplayViewModel
                          {
                              AgencyCategoryId = a.AgencyCategoryId,
                              AgencyCategoryName = a.AgencyCategoryName,
                              AgencyCount = a.Agencies.Count
                          }).ToListAsync();
        }

        /// <summary>
        /// Gets the number of agencies listed under a category
        /// </summary>
        /// <param name="context"></param>
        /// <param name="categoryID"></param>
        public static async Task<int> GetAgencyCountAsync(ApplicationDbContext context, int categoryID)
        {
            return await (from a in context.AgencyCategory
                          where a.AgencyCategoryId == categoryID
                          select a.Agencies.Count).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Returns true when another category already uses the name, ignoring case and surrounding spaces.
        /// Categories are looked up by name in the Resource Guide and the Agency pages, so names must be unique.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="name">The name to check</param>
        /// <param name="excludeCategoryID">The category being edited, which is allowed to keep its own name</param>
        public static async Task<bool> NameExistsAsync(ApplicationDbContext context, string name, int excludeCategoryID = 0)
        {
            string normalizedName = name.Trim().ToLower();
            return await context.AgencyCategory
                .AnyAsync(a => a.AgencyCategoryId != excludeCategoryID
                            && a.AgencyCategoryName.Trim().ToLower() == normalizedName);
        }

        /// <summary>
        /// Saves changes to a category
        /// </summary>
        /// <param name="context"></param>
        /// <param name="agencyCategory">The category to be updated</param>
        public static async Task UpdateCategoryAsync(ApplicationDbContext context, AgencyCategory agencyCategory)
        {
            context.AgencyCategory.Update(agencyCategory);
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Deletes a category. The agencies in the category are not deleted,
        /// they are only removed from the category.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="categoryID">The category to be deleted</param>
        public static async Task DeleteCategoryAsync(ApplicationDbContext context, int categoryID)
        {
            AgencyCategory? agencyCategory = await context.AgencyCategory
                .Include(a => a.Agencies)
                .FirstOrDefaultAsync(a => a.AgencyCategoryId == categoryID);

            if (agencyCategory != null)
            {
                // Only removes the rows linking the agencies to the category
                agencyCategory.Agencies.Clear();
                context.AgencyCategory.Remove(agencyCategory);
                await context.SaveChangesAsync();
            }
        }
    }
}
