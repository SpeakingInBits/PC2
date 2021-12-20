using Microsoft.EntityFrameworkCore;
using PC2.Models;

namespace PC2.Data
{
    public static class BoardDB
    {
        /// <summary>
        /// Returns a list of board members sorted by <seealso cref="People.PriorityOrder"/>
        /// Matching priority levels are then sorted A - Z
        /// </summary>
        public static async Task<List<Board>> GetAllBoardMembers(ApplicationDbContext context)
        {
            return await (from b in context.BoardMembers
                          orderby b.PriorityOrder ascending, b.Name ascending
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
