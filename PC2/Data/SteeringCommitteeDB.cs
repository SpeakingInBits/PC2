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

        public static async Task<SteeringCommittee?> GetSteeringCommitteeMember(ApplicationDbContext context, int id)
        {
            return await (from s in context.SteeringCommittee
                          where s.ID == id
                          select s).FirstOrDefaultAsync();
        }

        public static async Task EditSteeringCommittee(ApplicationDbContext context, SteeringCommittee steeringCommittee)
        {
            context.Entry(steeringCommittee).State = EntityState.Modified;
            await context.SaveChangesAsync();
        }
    }
}
