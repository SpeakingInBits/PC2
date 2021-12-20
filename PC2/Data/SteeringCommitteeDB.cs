using Microsoft.EntityFrameworkCore;
using PC2.Models;

namespace PC2.Data
{
    public static class SteeringCommitteeDB
    {
        /// <summary>
        /// Returns a list of committee members sorted by <seealso cref="People.PriorityOrder"/>
        /// Matching priority levels are then sorted A - Z
        /// </summary>
        public static async Task<List<SteeringCommittee>> GetAllSteeringCommittee(ApplicationDbContext context)
        {
            return await (from s in context.SteeringCommittee
                          orderby s.PriorityOrder ascending, s.Name ascending
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

        public static async Task Delete(ApplicationDbContext context, SteeringCommittee steeringCommittee)
        {
            context.Entry(steeringCommittee).State = EntityState.Deleted;
            await context.SaveChangesAsync();
        }
    }
}
