using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PC2.Models;

namespace PC2.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder builder)
        {
            // Store DateOnly values as 'date' columns
            builder.Properties<DateOnly>()
                .HaveConversion<DateOnlyConverter>()
                .HaveColumnType("date");

            builder.Properties<DateOnly?>()
                .HaveConversion<NullableDateOnlyConverter>()
                .HaveColumnType("date");

            builder.Properties<TimeOnly>()
                .HaveConversion<TimeOnlyConverter>()
                .HaveColumnType("time");

            builder.Properties<TimeOnly?>()
                .HaveConversion<NullableTimeOnlyConverter>()
                .HaveColumnType("time");
        }

        public virtual DbSet<Agency> Agency {  get; set; }
        public virtual DbSet<AgencyCategory> AgencyCategory {  get; set; }
        public virtual DbSet<CalendarEvent> CalendarEvents {  get; set; }
        public virtual DbSet<Staff> StaffMembers { get; set; }
        public virtual DbSet<Board> BoardMembers { get; set; }
        public virtual DbSet<SteeringCommittee> SteeringCommittee { get; set; }
        public virtual DbSet<People> People { get; set; }
        public virtual DbSet<NewsletterFile> NewsletterFile { get; set; }
    
        public virtual DbSet<HousingProgram> HousingProgram { get; set; }
        public virtual DbSet<Feedback> Feedback { get; set; }
    }

    internal class DateOnlyConverter : ValueConverter<DateOnly, DateTime>
    {
        public DateOnlyConverter() : base(d => d.ToDateTime(TimeOnly.MinValue), d => DateOnly.FromDateTime(d)) { }
    }

    internal class TimeOnlyConverter : ValueConverter<TimeOnly, TimeSpan>
    {
        public TimeOnlyConverter() : base(t => t.ToTimeSpan(), t => TimeOnly.FromTimeSpan(t)) { }
    }

    /// <summary>
    /// Converts <see cref="DateOnly?" /> to <see cref="DateTime?"/> and vice versa.
    /// </summary>
    internal class NullableDateOnlyConverter : ValueConverter<DateOnly?, DateTime?>
    {
        /// <summary>
        /// Creates a new instance of this converter.
        /// </summary>
        public NullableDateOnlyConverter() : base(
            d => d == null
                ? null
                : new DateTime?(d.Value.ToDateTime(TimeOnly.MinValue)),
            d => d == null
                ? null
                : new DateOnly?(DateOnly.FromDateTime(d.Value)))
        { }
    }

    internal class NullableTimeOnlyConverter : ValueConverter<TimeOnly?, TimeSpan?>
    {
        public NullableTimeOnlyConverter() : base(
            t => t == null ? null : new TimeSpan?(t.Value.ToTimeSpan()),
            t => t == null ? null : new TimeOnly?(TimeOnly.FromTimeSpan(t.Value))) { }
    }
}