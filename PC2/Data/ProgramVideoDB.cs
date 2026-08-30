using Microsoft.EntityFrameworkCore;
using PC2.Models;

namespace PC2.Data;

public class ProgramVideoDB
{
    public static async Task AddAsync(ApplicationDbContext context, ProgramVideo programVideo)
    {
        context.ProgramVideos.Add(programVideo);
        await context.SaveChangesAsync();
    }

    public static async Task<List<ProgramVideo>> GetAllAsync(ApplicationDbContext context)
    {
        return await (from pv in context.ProgramVideos
                      select pv).ToListAsync();
    }

    public static async Task<ProgramVideo?> GetVideoAsync(ApplicationDbContext context, int id)
    {
        return await context.ProgramVideos.FindAsync(id);
    }

    public static async Task UpdateAsync(ApplicationDbContext context, ProgramVideo programVideo)
    {
        context.ProgramVideos.Update(programVideo);
        await context.SaveChangesAsync();
    }

    public static async Task DeleteAsync(ApplicationDbContext context, int id)
    {
        ProgramVideo? programVideo = await context.ProgramVideos.FindAsync(id);
        if (programVideo != null)
        {
            context.ProgramVideos.Remove(programVideo);
            await context.SaveChangesAsync();
        }
    }
}
