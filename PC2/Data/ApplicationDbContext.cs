using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PC2.Models;

namespace PC2.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Agency> Agency {  get; set; }
        public virtual DbSet<AgencyCategory> AgencyCategory {  get; set; }
        public virtual DbSet<CalendarEvent> CalendarEvents {  get; set; }
        public virtual DbSet<CalendarDate> CalendarDates {  get; set; }
        public virtual DbSet<Staff> StaffMembers { get; set; }
        public virtual DbSet<Board> BoardMembers { get; set; }
        public virtual DbSet<SteeringCommittee> SteeringCommittee { get; set; }
        public virtual DbSet<People> People { get; set; }
    }
}