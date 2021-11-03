using Microsoft.EntityFrameworkCore;
using PC2.Models;

namespace PC2.Data
{
    public static class SteeringCommitteeDB
    {
        public static async Task<List<SteeringCommittee>> GetAllSteeringCommittee(ApplicationDbContext context)
        {
            return await (from s in context.SteeringCommittee
                          select s).ToListAsync();
        }
    }
}
