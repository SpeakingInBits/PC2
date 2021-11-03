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

        public static async Task Create(ApplicationDbContext context, SteeringCommittee steeringCommittee)
        {
            context.SteeringCommittee.Add(steeringCommittee);
            await context.SaveChangesAsync();
        }
    }
}
