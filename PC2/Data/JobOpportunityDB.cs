using Microsoft.EntityFrameworkCore;
using PC2.Models;

namespace PC2.Data;

public class JobOpportunityDB
{
    /// <summary>
    /// Adds a job opportunity to the database
    /// </summary>
    /// <param name="context"></param>
    /// <param name="job"></param>
    /// <returns></returns>
    public static async Task AddAsync(ApplicationDbContext context, JobOpportunity job)
    {
        context.JobOpportunities.Add(job);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Gets all job opportunities with open jobs listed first
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static async Task<List<JobOpportunity>> GetAllAsync(ApplicationDbContext context)
    {
        List<JobOpportunity> jobs = await (from j in context.JobOpportunities
                                           select j).ToListAsync();

        return jobs
            .OrderByDescending(j => j.IsOpen)
            .ThenBy(j => j.ClosingDate == null)
            .ThenBy(j => j.ClosingDate)
            .ThenBy(j => j.Title)
            .ToList();
    }

    /// <summary>
    /// Gets the job opportunities that are open: not manually closed and
    /// the closing date (if any) has not passed
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static async Task<List<JobOpportunity>> GetOpenAsync(ApplicationDbContext context)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);

        return await (from j in context.JobOpportunities
                      where !j.IsClosed && (j.ClosingDate == null || j.ClosingDate >= today)
                      orderby j.ClosingDate == null, j.ClosingDate, j.Title
                      select j).ToListAsync();
    }

    /// <summary>
    /// Gets a job opportunity based on ID
    /// </summary>
    /// <param name="context"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    public static async Task<JobOpportunity?> GetJobAsync(ApplicationDbContext context, int id)
    {
        return await context.JobOpportunities.FindAsync(id);
    }

    /// <summary>
    /// Updates a job opportunity entry
    /// </summary>
    /// <param name="context"></param>
    /// <param name="job"></param>
    /// <returns></returns>
    public static async Task UpdateAsync(ApplicationDbContext context, JobOpportunity job)
    {
        context.JobOpportunities.Update(job);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Deletes a job opportunity based on ID
    /// </summary>
    /// <param name="context"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    public static async Task DeleteAsync(ApplicationDbContext context, int id)
    {
        JobOpportunity? job = await context.JobOpportunities.FindAsync(id);
        if (job != null)
        {
            context.JobOpportunities.Remove(job);
            await context.SaveChangesAsync();
        }
    }
}
