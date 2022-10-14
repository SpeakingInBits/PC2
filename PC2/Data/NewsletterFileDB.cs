using Microsoft.EntityFrameworkCore;
using PC2.Models;

namespace PC2.Data
{
    public class NewsletterFileDB
    {
        public static async Task AddAsync(ApplicationDbContext context, NewsletterFile newsletterFile)
        {
            context.NewsletterFile.Add(newsletterFile);
            await context.SaveChangesAsync();
        }

        public static async Task <List<NewsletterFile>> GetAllAsync(ApplicationDbContext context)
        {
            return await (from nf in context.NewsletterFile
                          select nf).ToListAsync();
        }
        
        public static async Task<List<string>> GetAllNamesAsync(ApplicationDbContext context)
        {
            return await (from nf in context.NewsletterFile
                          select nf.Name).ToListAsync();
        }

        public static async Task DeleteAsync(ApplicationDbContext context, int id)
        {
            NewsletterFile? newsletterFile = await context.NewsletterFile.FindAsync(id);
            if (newsletterFile != null)
            {
                context.NewsletterFile.Remove(newsletterFile);
                await context.SaveChangesAsync();
            }
        }

        public static async Task<NewsletterFile> GetFileAsync(ApplicationDbContext context, int id)
        {
            NewsletterFile? newsletterFile = await context.NewsletterFile.FindAsync(id);
            if (newsletterFile != null)
            {
                return newsletterFile;
            }
            else
            {
                throw new FileNotFoundException("NewsletterFileId not found within database");
            }

        }

        public static async Task RenameFileAsync(ApplicationDbContext context, int id, string newName)
        {
            NewsletterFile? newsletterFile = await context.NewsletterFile.FindAsync(id);
            if (newsletterFile != null)
            {
                newsletterFile.Name = newName;
                await context.SaveChangesAsync();
            }
            else
            {
                throw new FileNotFoundException("NewsletterFileId not found within database");
            }
        }
    }
}
