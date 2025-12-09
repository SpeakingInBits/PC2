using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PC2.Models;

namespace PC2.Data
{
    public static class AgencyDB
    {
        /// <summary>
        /// Adds an Agency to the database
        /// </summary>
        /// <param name="context"></param>
        /// <param name="agency">The agency to be added</param>
        public static async Task AddAgencyAsync(ApplicationDbContext context, Agency agency)
        {
            context.Agency.Add(agency);
            await context.SaveChangesAsync();
        }
        
        /// <summary>
        /// Gets all agencies in alphabetical order by agency name
        /// </summary>
        public async static Task<List<AgencyDisplayViewModel>> GetAgencySearchList(ApplicationDbContext context)
        {
            return await (from agency in context.Agency
                          select new AgencyDisplayViewModel
                          {
                              AgencyId = agency.AgencyId,
                              AgencyName = agency.AgencyName,
                              AgencyName2 = agency.AgencyName2,
                              City = agency.City
                          })
                          .OrderBy(agency  => agency.AgencyName)
                          .ToListAsync();
        }

        /// <summary>
        /// Gets a list of distinct agencies from the database sorted in alphabetical order by Agency Name.
        /// </summary>
        public static async Task<List<Agency>> GetDistinctAgenciesAsync(ApplicationDbContext context)
        {
            var query = context.Agency
                .Where(a => a.AgencyName != null)
                .Join(
                    context.Agency
                        .Where(x => x.AgencyName != null)
                        .GroupBy(x => x.AgencyName)
                        .Select(g => g.Min(x => x.AgencyId)),
                    a => a.AgencyId,
                    minId => minId,
                    (a, minId) => a
                )
                .OrderBy(a => a.AgencyName);

            return await query.ToListAsync();
        }

        /// <summary>
        /// Gets all agencies from the database in alphabetical order by Agency Name
        /// </summary>
        public static async Task<List<Agency>> GetAllAgenciesAsync(ApplicationDbContext context)
        {
            return await (from a in context.Agency
                          select a).Include(nameof(Agency.AgencyCategories))
                          .OrderBy(agency => agency.AgencyName)
                          .ToListAsync();
        }

        public static async Task<int> GetAgencyCountAsync(ApplicationDbContext context)
        {
            return await context.Agency.CountAsync();
        }

        /// <summary>
        /// Gets all agencies by page and page size
        /// </summary>
        /// <param name="pageSize">The number of agencies per page</param>
        /// <param name="pageNum">The page number</param>
        /// <returns></returns>
        public static async Task<List<Agency>> GetAllAgenciesAsync(ApplicationDbContext context, int pageSize, int pageNum)
        {
            List<Agency> agencies = await
                                    (from a in context.Agency
                                    select a)
                                    .OrderBy(a => a.AgencyName)
                                    .Include(nameof(Agency.AgencyCategories))
                                    .Skip(pageSize * (pageNum - 1))
                                    .Take(pageSize)
                                    .ToListAsync();
            return agencies;
        }

        /// <summary>
        /// Gets all agencies that have a category that matches the categoryID
        /// </summary>
        public static async Task<List<Agency>> GetSpecificAgenciesAsync(ApplicationDbContext context, int categoryID)
        {
            List<Agency> agencies = await GetAllAgenciesAsync(context);

            List<Agency> result = new List<Agency>();
            for (int i = 0; i < agencies.Count; i++)
            {
                for (int j = 0; j < agencies[i].AgencyCategories.Count; j++)
                {
                    if (agencies[i].AgencyCategories[j].AgencyCategoryId == categoryID)
                    {
                        result.Add(agencies[i]);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Returns a list of Agencies in a specific city in alphabetical order
        /// by AgencyName
        /// </summary>
        /// <param name="context"></param>
        /// <param name="city">City the user is searching by</param>
        /// <returns></returns>
        public static async Task<List<Agency>> GetSpecificAgenciesAsync(ApplicationDbContext context, string city)
        {
            return await (from a in context.Agency
                          where a.City != null && a.City == city
                          select a).Include(nameof(Agency.AgencyCategories))
                          .OrderBy(agency => agency.AgencyName)
                          .ToListAsync();
        }

        /// <summary>
        /// Gets a list of every agency with a matching category and city
        /// </summary>
        /// <param name="context"></param>
        /// <param name="categoryName">The category to search for</param>
        /// <param name="city">The city to search for</param>
        public static async Task<List<Agency>> GetAgenciesByCategoryAndCity(ApplicationDbContext context,
            string categoryName, string city)
        {
            IQueryable<Agency> agencyResults = context.Agency
                .Include(nameof(Agency.AgencyCategories))
                .Where(agency => agency.City == city);

            return await agencyResults
                .Where(agency => agency.AgencyCategories
                .Where(cat => cat.AgencyCategoryName == categoryName)
                .Any())
                .OrderBy(agency => agency.AgencyName)
                .ToListAsync();
        }

        /// <summary>
        /// Asynchronously retrieves an agency by its unique identifier, including its associated categories.
        /// </summary>
        /// <param name="id">The unique identifier of the agency to retrieve.</param>
        /// <returns>Result contains the agency with the specified
        /// identifier, or null if no such agency exists.</returns>
        public static async Task<Agency?> GetAgencyAsync(ApplicationDbContext context, int id)
        {
            return await (from a in context.Agency
                          where a.AgencyId == id
                          select a).Include(nameof(Agency.AgencyCategories)).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Updates the Agency with the categories that belong to it
        /// </summary>
        /// <param name="context"></param>
        /// <param name="agency">The Agency being updated</param>
        /// <param name="categories">The categories being added to the agency</param>
        public static async Task UpdateAgencyAsync(ApplicationDbContext context, Agency agency, 
            List<AgencyCategory> removedCategories)
        {
            for (int i = 0; i < removedCategories.Count; i++)
            {
                for (int j = 0; j < agency.AgencyCategories.Count; j++)
                {
                    if (removedCategories[i] == agency.AgencyCategories[j])
                    {
                        string query = "DELETE FROM AgencyAgencyCategory WHERE AgenciesAgencyId = @agencyId AND AgencyCategoriesAgencyCategoryId = @categoryId";
                        SqlParameter[] sqlParams = new SqlParameter[]
                        {
                            new SqlParameter { ParameterName = "agencyId", Value = agency.AgencyId},
                            new SqlParameter { ParameterName = "categoryId", Value = agency.AgencyCategories[j].AgencyCategoryId}
                        };
                        context.Database.ExecuteSqlRaw(query, sqlParams);
                        agency.AgencyCategories.Remove(removedCategories[i]);
                    }
                }
            }
            for (int i = 0; i < agency.AgencyCategories.Count; i++)
            {
                context.AgencyCategory.Attach(agency.AgencyCategories[i]);
            }
            context.Entry(agency).State = EntityState.Modified;
            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return;
            }
        }

        public static async Task<List<Agency>> GetAgenciesByName(ApplicationDbContext context, string name)
        {
            return await (from a in context.Agency
                          where a.AgencyName == name
                          select a).Include(nameof(Agency.AgencyCategories)).ToListAsync();
        }

        public static async Task<List<string>> GetAllCities(ApplicationDbContext context)
        {
            return await context.Agency
                .Where(agency => agency.City != null)
                .Select(agency => agency.City!)
                .Distinct()
                .OrderBy(city => city)
                .ToListAsync();
        }

        /// <summary>
        /// Helper method to get the data that populates the datalists
        /// in the resource guide.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="resourceGuide">The model to store the lists in.</param>
        public static async Task GetDataForDataLists(ApplicationDbContext context,
            ResourceGuideModel resourceGuide)
        {
            resourceGuide.AgencyCategoriesForDataList = await AgencyCategoryDB.GetAgencyCategoriesAsync(context);
            resourceGuide.AgenciesForDataList = await GetDistinctAgenciesAsync(context);
            resourceGuide.CitiesForDataList = await GetAllCities(context);
        }

        public static async Task Delete(ApplicationDbContext context, Agency agency)
        {
            context.Entry(agency).State = EntityState.Deleted;
            await context.SaveChangesAsync();
        }
    }
}
