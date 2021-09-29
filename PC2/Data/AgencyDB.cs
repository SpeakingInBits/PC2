using Microsoft.EntityFrameworkCore;
using PC2.Models;

namespace PC2.Data
{
    public static class AgencyDB
    {
        public static async Task AddAgencyAsync(ApplicationDbContext context, Agency agency)
        {
            context.Agency.Add(agency);
            await context.SaveChangesAsync();
        }

        public static async Task<List<Agency>> GetAllAgencyAsync(ApplicationDbContext context)
        {
            return await (from a in context.Agency
                          select a).ToListAsync();
        }

        public static async Task UpdateAgencyAsync(ApplicationDbContext context, Agency agency)
        {
            context.Entry(agency).State = EntityState.Modified;
            await context.SaveChangesAsync();
        }
    }
}
