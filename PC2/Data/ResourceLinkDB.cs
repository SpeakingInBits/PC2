using Microsoft.EntityFrameworkCore;
using PC2.Models;

namespace PC2.Data;

public class ResourceLinkDB
{
    public static async Task AddAsync(ApplicationDbContext context, ResourceLink resourceLink)
    {
        context.ResourceLinks.Add(resourceLink);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Gets all resource links in alphabetical order
    /// </summary>
    public static async Task<List<ResourceLink>> GetAllAsync(ApplicationDbContext context)
    {
        return await (from rl in context.ResourceLinks
                      orderby rl.Name
                      select rl).ToListAsync();
    }

    public static async Task<ResourceLink?> GetLinkAsync(ApplicationDbContext context, int id)
    {
        return await context.ResourceLinks.FindAsync(id);
    }

    public static async Task UpdateAsync(ApplicationDbContext context, ResourceLink resourceLink)
    {
        context.ResourceLinks.Update(resourceLink);
        await context.SaveChangesAsync();
    }

    public static async Task DeleteAsync(ApplicationDbContext context, int id)
    {
        ResourceLink? resourceLink = await context.ResourceLinks.FindAsync(id);
        if (resourceLink != null)
        {
            context.ResourceLinks.Remove(resourceLink);
            await context.SaveChangesAsync();
        }
    }
}
