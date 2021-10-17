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
        /// Gets all Agencies that are distinct from the database
        /// </summary>
        public static async Task<List<Agency?>> GetDistinctAgenciesAsync(ApplicationDbContext context)
        {
            return await (from a in context.Agency
                                       select a).Include(nameof(Agency.AgencyCategories)).GroupBy(a => a.AgencyName)
                                       .Select(a => a.FirstOrDefault())
                                       .ToListAsync();
        }

        /// <summary>
        /// Gets all agencies from the database
        /// </summary>
        public static async Task<List<Agency>> GetAllAgenciesAsync(ApplicationDbContext context)
        {
            return await (from a in context.Agency
                          select a).Include(nameof(Agency.AgencyCategories)).ToListAsync();
        }

        /// <summary>
        /// Gets all agencies by page and page size
        /// </summary>
        /// <param name="pageSize">The number of agencies per page</param>
        /// <param name="pageNum">The page number</param>
        /// <returns></returns>
        public static async Task<List<Agency>> GetAllAgenciesAsync(ApplicationDbContext context, int pageSize, int pageNum)
        {
            List<Agency> agencies = await (from a in context.Agency
                          select a).OrderBy(a => a.AgencyName)
                          .Include(nameof(Agency.AgencyCategories)).ToListAsync();
            return agencies.Skip(pageSize * (pageNum - 1)).Take(pageSize).ToList();
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

        public static async Task<List<Agency>> GetSpecificAgenciesAsync(ApplicationDbContext context, string zipCode)
        {
            return await (from a in context.Agency
                          where a.Zip !=  null && a.Zip == zipCode
                          select a).Include(nameof(Agency.AgencyCategories)).ToListAsync();
        }

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

        public static async Task<List<string>> GetAllZipCode(ApplicationDbContext context)
        {
            List<Agency> list = await (from a in context.Agency
                                       where a.Zip != null
                                       select a).ToListAsync();
            List<string> result = new List<string>();
            for (int i = 0; i < list.Count; i++)
            {
                result.Add(list[i].Zip);
            }
            result = result.OrderBy(a => a).Distinct().ToList();
            return result;
        }
    }
}
