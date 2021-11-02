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
    }
}
