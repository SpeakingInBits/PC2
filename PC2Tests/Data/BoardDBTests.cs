using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;
using PC2.Data;
using PC2.Models;

namespace PC2Tests.Data;

[TestClass]
public class BoardDBTests
{
    private ApplicationDbContext _context = null!;
    private DbContextOptions<ApplicationDbContext> _options = null!;

    [TestInitialize]
    public void Setup()
    {
        // Create a new in-memory database for each test
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                      .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                      .Options;

        _context = new ApplicationDbContext(_options);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region GetAllBoardMembers Tests

    [TestMethod]
    public async Task GetAllBoardMembers_WithNoMembers_ReturnsEmptyList()
    {
        // Act
        var result = await BoardDB.GetAllBoardMembers(_context);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public async Task GetAllBoardMembers_WithMultipleMembers_ReturnsSortedByPriorityThenName()
    {
        // Arrange
        var board1 = new Board
        {
            Name = "Charlie Brown",
            Title = "Secretary",
            PriorityOrder = 2,
            MembershipStart = "2020-01-01"
        };
        var board2 = new Board
        {
            Name = "Alice Smith",
            Title = "President",
            PriorityOrder = 1,
            MembershipStart = "2019-06-15"
        };
        var board3 = new Board
        {
            Name = "Bob Jones",
            Title = "Treasurer",
            PriorityOrder = 1,
            MembershipStart = "2021-03-20"
        };

        _context.BoardMembers.AddRange(board1, board3, board2);
        await _context.SaveChangesAsync();

        // Act
        var result = await BoardDB.GetAllBoardMembers(_context);

        // Assert
        Assert.HasCount(3, result);
        // Should be sorted by PriorityOrder (1, 1, 2), then by Name (Alice, Bob, Charlie)
        Assert.AreEqual("Alice Smith", result[0].Name);
        Assert.AreEqual("Bob Jones", result[1].Name);
        Assert.AreEqual("Charlie Brown", result[2].Name);
    }

    [TestMethod]
    public async Task GetAllBoardMembers_WithSamePriority_SortsAlphabetically()
    {
        // Arrange
        var board1 = new Board
        {
            Name = "Zoe Wilson",
            Title = "Member",
            PriorityOrder = 5,
            MembershipStart = "2020-01-01"
        };
        var board2 = new Board
        {
            Name = "Adam Davis",
            Title = "Member",
            PriorityOrder = 5,
            MembershipStart = "2020-01-01"
        };
        var board3 = new Board
        {
            Name = "Mike Taylor",
            Title = "Member",
            PriorityOrder = 5,
            MembershipStart = "2020-01-01"
        };

        _context.BoardMembers.AddRange(board1, board2, board3);
        await _context.SaveChangesAsync();

        // Act
        var result = await BoardDB.GetAllBoardMembers(_context);

        // Assert
        Assert.HasCount(3, result);
        Assert.AreEqual("Adam Davis", result[0].Name);
        Assert.AreEqual("Mike Taylor", result[1].Name);
        Assert.AreEqual("Zoe Wilson", result[2].Name);
    }

    #endregion

    #region GetAllBoardMembersForEditing Tests

    [TestMethod]
    public async Task GetAllBoardMembersForEditing_WithNoMembers_ReturnsEmptyList()
    {
        // Act
        var result = await BoardDB.GetAllBoardMembersForEditing(_context);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public async Task GetAllBoardMembersForEditing_WithMultipleMembers_ReturnsSortedAlphabetically()
    {
        // Arrange
        var board1 = new Board
        {
            Name = "Zoe Wilson",
            Title = "President",
            PriorityOrder = 1,
            MembershipStart = "2020-01-01"
        };
        var board2 = new Board
        {
            Name = "Adam Davis",
            Title = "Secretary",
            PriorityOrder = 3,
            MembershipStart = "2020-01-01"
        };
        var board3 = new Board
        {
            Name = "Mike Taylor",
            Title = "Treasurer",
            PriorityOrder = 2,
            MembershipStart = "2020-01-01"
        };

        _context.BoardMembers.AddRange(board1, board2, board3);
        await _context.SaveChangesAsync();

        // Act
        var result = await BoardDB.GetAllBoardMembersForEditing(_context);

        // Assert
        Assert.HasCount(3, result);
        // Should ignore priority and sort alphabetically by name
        Assert.AreEqual("Adam Davis", result[0].Name);
        Assert.AreEqual("Mike Taylor", result[1].Name);
        Assert.AreEqual("Zoe Wilson", result[2].Name);
    }

    #endregion

    #region CreateBoardMember Tests

    [TestMethod]
    public async Task CreateBoardMember_WithValidBoard_AddsToDatabase()
    {
        // Arrange
        var newBoard = new Board
        {
            Name = "John Doe",
            Title = "Vice President",
            PriorityOrder = 2,
            MembershipStart = "2023-01-15"
        };

        // Act
        await BoardDB.CreateBoardMember(_context, newBoard);

        // Assert
        var result = await _context.BoardMembers.ToListAsync();
        Assert.HasCount(1, result);
        Assert.AreEqual("John Doe", result[0].Name);
        Assert.AreEqual("Vice President", result[0].Title);
        Assert.AreEqual(2, result[0].PriorityOrder);
        Assert.AreEqual("2023-01-15", result[0].MembershipStart);
    }

    [TestMethod]
    public async Task CreateBoardMember_WithMultipleMembers_AddsAllToDatabase()
    {
        // Arrange
        var board1 = new Board
        {
            Name = "John Doe",
            Title = "President",
            PriorityOrder = 1,
            MembershipStart = "2023-01-15"
        };
        var board2 = new Board
        {
            Name = "Jane Smith",
            Title = "Secretary",
            PriorityOrder = 2,
            MembershipStart = "2023-02-20"
        };

        // Act
        await BoardDB.CreateBoardMember(_context, board1);
        await BoardDB.CreateBoardMember(_context, board2);

        // Assert
        var result = await _context.BoardMembers.ToListAsync();
        Assert.HasCount(2, result);
    }

    #endregion

    #region GetBoardMember Tests

    [TestMethod]
    public async Task GetBoardMember_WithValidId_ReturnsBoardMember()
    {
        // Arrange
        var board = new Board
        {
            Name = "John Doe",
            Title = "President",
            PriorityOrder = 1,
            MembershipStart = "2023-01-15"
        };
        _context.BoardMembers.Add(board);
        await _context.SaveChangesAsync();

        // Act
        var result = await BoardDB.GetBoardMember(_context, board.ID);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(board.ID, result.ID);
        Assert.AreEqual(board.PriorityOrder, result.PriorityOrder);
        Assert.AreEqual("John Doe", result.Name);
        Assert.AreEqual("President", result.Title);
    }

    [TestMethod]
    public async Task GetBoardMember_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var board = new Board
        {
            Name = "John Doe",
            Title = "President",
            PriorityOrder = 1,
            MembershipStart = "2023-01-15"
        };
        _context.BoardMembers.Add(board);
        await _context.SaveChangesAsync();

        // Act
        var result = await BoardDB.GetBoardMember(_context, 999);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetBoardMember_WithEmptyDatabase_ReturnsNull()
    {
        // Act
        var result = await BoardDB.GetBoardMember(_context, 1);

        // Assert
        Assert.IsNull(result);
    }

    #endregion

    #region EditBoardMember Tests

    [TestMethod]
    public async Task EditBoardMember_WithValidChanges_UpdatesDatabase()
    {
        // Arrange
        var board = new Board
        {
            Name = "John Doe",
            Title = "President",
            PriorityOrder = 1,
            MembershipStart = "2023-01-15"
        };
        _context.BoardMembers.Add(board);
        await _context.SaveChangesAsync();

        // Detach the entity to simulate a new context
        _context.Entry(board).State = EntityState.Detached;

        // Modify the board member
        board.Name = "John Smith";
        board.Title = "Vice President";
        board.PriorityOrder = 2;

        // Act
        await BoardDB.EditBoardMember(_context, board);

        // Assert
        var result = await _context.BoardMembers.FindAsync(board.ID);
        Assert.IsNotNull(result);
        Assert.AreEqual("John Smith", result.Name);
        Assert.AreEqual("Vice President", result.Title);
        Assert.AreEqual(2, result.PriorityOrder);
    }

    [TestMethod]
    public async Task EditBoardMember_UpdatesOnlySpecifiedMember()
    {
        // Arrange
        var board1 = new Board
        {
            Name = "John Doe",
            Title = "President",
            PriorityOrder = 1,
            MembershipStart = "2023-01-15"
        };
        var board2 = new Board
        {
            Name = "Jane Smith",
            Title = "Secretary",
            PriorityOrder = 2,
            MembershipStart = "2023-02-20"
        };
        _context.BoardMembers.AddRange(board1, board2);
        await _context.SaveChangesAsync();

        // Detach the entity
        _context.Entry(board1).State = EntityState.Detached;

        // Modify only board1
        board1.Title = "Updated Title";

        // Act
        await BoardDB.EditBoardMember(_context, board1);

        // Assert
        var result1 = await _context.BoardMembers.FindAsync(board1.ID);
        var result2 = await _context.BoardMembers.FindAsync(board2.ID);

        Assert.AreEqual("Updated Title", result1!.Title);
        Assert.AreEqual("Secretary", result2!.Title); // Unchanged
    }

    #endregion

    #region Delete Tests

    [TestMethod]
    public async Task Delete_WithValidBoard_RemovesFromDatabase()
    {
        // Arrange
        var board = new Board
        {
            Name = "John Doe",
            Title = "President",
            PriorityOrder = 1,
            MembershipStart = "2023-01-15"
        };
        _context.BoardMembers.Add(board);
        await _context.SaveChangesAsync();

        // Detach to simulate deletion from a fresh context
        _context.Entry(board).State = EntityState.Detached;

        // Act
        await BoardDB.Delete(_context, board);

        // Assert
        var result = await _context.BoardMembers.ToListAsync();
        Assert.HasCount(0, result);
    }

    [TestMethod]
    public async Task Delete_DeletesOnlySpecifiedMember()
    {
        // Arrange
        var board1 = new Board
        {
            Name = "John Doe",
            Title = "President",
            PriorityOrder = 1,
            MembershipStart = "2023-01-15"
        };
        var board2 = new Board
        {
            Name = "Jane Smith",
            Title = "Secretary",
            PriorityOrder = 2,
            MembershipStart = "2023-02-20"
        };
        _context.BoardMembers.AddRange(board1, board2);
        await _context.SaveChangesAsync();

        // Detach board1
        _context.Entry(board1).State = EntityState.Detached;

        // Act
        await BoardDB.Delete(_context, board1);

        // Assert
        var result = await _context.BoardMembers.ToListAsync();
        Assert.HasCount(1, result);
        Assert.AreEqual("Jane Smith", result[0].Name);
    }

    [TestMethod]
    public async Task Delete_AfterDeletion_CannotRetrieveDeletedMember()
    {
        // Arrange
        var board = new Board
        {
            Name = "John Doe",
            Title = "President",
            PriorityOrder = 1,
            MembershipStart = "2023-01-15"
        };
        _context.BoardMembers.Add(board);
        await _context.SaveChangesAsync();
        int deletedId = board.ID;

        // Detach to simulate deletion from a fresh context
        _context.Entry(board).State = EntityState.Detached;

        // Act
        await BoardDB.Delete(_context, board);

        // Assert
        var result = await BoardDB.GetBoardMember(_context, deletedId);
        Assert.IsNull(result);
    }

    #endregion

    #region Integration Tests

    [TestMethod]
    public async Task IntegrationTest_CreateEditDelete_WorksCorrectly()
    {
        // Create
        var board = new Board
        {
            Name = "John Doe",
            Title = "President",
            PriorityOrder = 1,
            MembershipStart = "2023-01-15"
        };
        await BoardDB.CreateBoardMember(_context, board);

        // Verify created
        var created = await BoardDB.GetBoardMember(_context, board.ID);
        Assert.IsNotNull(created);
        Assert.AreEqual("John Doe", created.Name);

        // Edit
        _context.Entry(created).State = EntityState.Detached;
        created.Name = "John Smith";
        await BoardDB.EditBoardMember(_context, created);

        // Verify edited
        var edited = await BoardDB.GetBoardMember(_context, board.ID);
        Assert.AreEqual("John Smith", edited!.Name);

        // Delete
        _context.Entry(edited).State = EntityState.Detached;
        await BoardDB.Delete(_context, edited);

        // Verify deleted
        var deleted = await BoardDB.GetBoardMember(_context, board.ID);
        Assert.IsNull(deleted);
    }

    [TestMethod]
    public async Task IntegrationTest_MultipleOperations_MaintainsDataIntegrity()
    {
        // Create multiple board members
        var boards = new List<Board>
        {
            new() { Name = "Alice", Title = "President", PriorityOrder = 1, MembershipStart = "2020-01-01" },
            new() { Name = "Bob", Title = "Vice President", PriorityOrder = 2, MembershipStart = "2020-02-01" },
            new() { Name = "Charlie", Title = "Secretary", PriorityOrder = 3, MembershipStart = "2020-03-01" }
        };

        foreach (var board in boards)
        {
            await BoardDB.CreateBoardMember(_context, board);
        }

        // Verify all created
        var allMembers = await BoardDB.GetAllBoardMembers(_context);
        Assert.HasCount(3, allMembers);

        // Edit one
        var toEdit = await BoardDB.GetBoardMember(_context, boards[1].ID);
        _context.Entry(toEdit!).State = EntityState.Detached;
        toEdit!.PriorityOrder = 1;
        await BoardDB.EditBoardMember(_context, toEdit);

        // Delete one
        var toDelete = await BoardDB.GetBoardMember(_context, boards[2].ID);
        _context.Entry(toDelete!).State = EntityState.Detached;
        await BoardDB.Delete(_context, toDelete!);

        // Verify final state
        var finalMembers = await BoardDB.GetAllBoardMembers(_context);
        Assert.HasCount(2, finalMembers);

        // Verify sorting after edit (both have priority 1, should be alphabetical)
        Assert.AreEqual("Alice", finalMembers[0].Name);
        Assert.AreEqual("Bob", finalMembers[1].Name);
    }

    #endregion
}
