using Microsoft.EntityFrameworkCore;
using PC2.Models;

namespace PC2.Data
{
    public class NewsletterFileDB
    {
        public static async Task AddNewsletterFileAsync(ApplicationDbContext context, NewsletterFile newsletterFile)
        {
            context.NewsletterFile.Add(newsletterFile);
            await context.SaveChangesAsync();
        }

        public static async Task <List<NewsletterFile>> GetAllNewsletterFilesAsync(ApplicationDbContext context)
        {
            return await (from nf in context.NewsletterFile
                          select nf).ToListAsync();
        }
        
        public static async Task<List<string>> GetAllNewsletterNamesAsync(ApplicationDbContext context)
        {
            return await (from nf in context.NewsletterFile
                          select nf.Name).ToListAsync();
        }
    }
}
