using Microsoft.EntityFrameworkCore;
using PC2.Models;

namespace PC2.Data
{
    public static class StaffDB
    {
        public static async Task AddStaff(ApplicationDbContext context, Staff staff)
        {
            context.StaffMembers.Add(staff);
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Returns a list of staff sorted by <seealso cref="People.PriorityOrder"/>
        /// Matching priority levels are then sorted A - Z
        /// </summary>
        public static async Task<List<Staff>> GetAllStaff(ApplicationDbContext context)
        {
            return await (from s in context.StaffMembers
                          orderby s.PriorityOrder ascending, s.Name ascending
                          select s).ToListAsync();
        }

        public static async Task<Staff?> GetStaffMember(ApplicationDbContext context, int id)
        {
            return await (from s in context.StaffMembers
                          where s.ID == id
                          select s).FirstOrDefaultAsync();
        }

        public static async Task SaveChanges(ApplicationDbContext context, Staff staff)
        {
            context.Entry(staff).State = EntityState.Modified;
            await context.SaveChangesAsync();
        }

        public static async Task Delete(ApplicationDbContext context, Staff staff)
        {
            context.Entry(staff).State = EntityState.Deleted;
            await context.SaveChangesAsync();
        }
    }
}
