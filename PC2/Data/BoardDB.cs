using Microsoft.EntityFrameworkCore;
using PC2.Models;

namespace PC2.Data
{
    public static class BoardDB
    {
        public static async Task<List<Board>> GetAllBoardMembers(ApplicationDbContext context)
        {
            return await (from b in context.BoardMembers
                          select b).ToListAsync();
        }

        public static async Task CreateBoardMember(ApplicationDbContext context, Board board)
        {
            context.BoardMembers.Add(board);
            await context.SaveChangesAsync();
        }

        public static async Task<Board?> GetBoardMember(ApplicationDbContext context, int id)
        {
            return await (from b in context.BoardMembers
                          where b.ID == id
                          select b).FirstOrDefaultAsync();
        }

        public static async Task EditBoardMember(ApplicationDbContext context, Board board)
        {
            context.Entry(board).State = EntityState.Modified;
            await context.SaveChangesAsync();
        }

        public static async Task Delete(ApplicationDbContext context, Board board)
        {
            context.Entry(board).State = EntityState.Deleted;
            await context.SaveChangesAsync();
        }
    }
}
