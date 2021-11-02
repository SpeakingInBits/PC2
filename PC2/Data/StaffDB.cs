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
    }
}
