using Microsoft.EntityFrameworkCore;
using PC2.Models;

namespace PC2.Data
{
    /// <summary>
    /// Data access layer for resource links. Provides CRUD operations with automatic FirstLetter management.
    /// </summary>
    public static class ResourceLinksDB
    {
        /// <summary>
        /// Retrieves all resource links sorted alphabetically by LinkText.
        /// </summary>
        /// <param name="context">The DbContext instance.</param>
        /// <returns>List of ResourceLinksModel sorted A-Z. Empty list if none exist.</returns>
        public static async Task<List<ResourceLinksModel>> GetAllResourceLinks(ApplicationDbContext context)
        {
            return await (from r in context.ResourceLinks
                          orderby r.LinkText ascending
                          select r).ToListAsync();
        }

        /// <summary>
        /// Retrieves a specific resource by ID.
        /// </summary>
        /// <param name="context">The DbContext instance.</param>
        /// <param name="resourceId">The ResourceID to retrieve.</param>
        /// <returns>ResourceLinksModel if found; null otherwise.</returns>
        public static async Task<ResourceLinksModel?> GetResourceLink(ApplicationDbContext context, int resourceId)
        {
            return await (from r in context.ResourceLinks
                          where r.ResourceID == resourceId
                          select r).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Adds a new resource link. FirstLetter is auto-calculated from LinkText.
        /// </summary>
        /// <param name="context">The DbContext instance.</param>
        /// <param name="resourceLink">The resource to add.</param>
        public static async Task AddResourceLink(ApplicationDbContext context, ResourceLinksModel resourceLink)
        {
            context.ResourceLinks.Add(resourceLink);
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Updates an existing resource link. FirstLetter is recalculated from LinkText.
        /// </summary>
        /// <param name="context">The DbContext instance.</param>
        /// <param name="resourceLink">The resource with updated values (must have valid ResourceID).</param>
        public static async Task UpdateResourceLink(ApplicationDbContext context, ResourceLinksModel resourceLink)
        {
            context.Entry(resourceLink).State = EntityState.Modified;
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Deletes a resource link (internal use).
        /// </summary>
        /// <param name="context">The DbContext instance.</param>
        /// <param name="resourceLink">The resource to delete.</param>
        public static async Task DeleteResourceLink(ApplicationDbContext context, ResourceLinksModel resourceLink)
        {
            context.Entry(resourceLink).State = EntityState.Deleted;
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Deletes a resource link by ID. Null-safe (no error if ID doesn't exist).
        /// </summary>
        /// <param name="context">The DbContext instance.</param>
        /// <param name="resourceId">The ResourceID to delete.</param>
        public static async Task DeleteResourceLinkById(ApplicationDbContext context, int resourceId)
        {
            // First, attempt to retrieve the resource
            // This ensures we only delete if the resource actually exists
            var resourceLink = await GetResourceLink(context, resourceId);

            // Only proceed with deletion if the resource was found
            // Null-check protects against invalid IDs without throwing exceptions
            if (resourceLink != null)
            {
                await DeleteResourceLink(context, resourceLink);
            }
        }
    }
}
