using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookStoreMainSup.Controllers;
using BookStoreMainSup.Data;
using BookStoreMainSup.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

public class BookServicesTests
{
    private readonly ApplicationDbContext _context;
   // private readonly ILogger<BooksController> _logger;
    private readonly BooksService _bookservice;

    public BookServicesTests()
    {
        // Set up the in-memory database options
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "BookStoreTest")
            .Options;

        // Initialize the context with the in-memory options
        _context = new ApplicationDbContext(options);

        // Use a real logger or mock if needed
       // _logger = new Mock<ILogger<BooksController>>().Object;
      //  _controller = new BooksController(_context, _logger);

        // Seed the database with initial data if necessary
        SeedDatabase();
        _bookservice = new BooksService(_context);
    }

    private void SeedDatabase()
    {
        var books = new List<Books>
        {
            new Books { Id = 1, Title = "Test Book", Author = "Test Author", Price = 20, isbn = 1234567890 }
            // Add more books if needed
        };

        _context.Books.AddRange(books);
        _context.SaveChanges();
    }

    [Fact]
    public async Task UpdateBookAsync_ForValidBook_SavesInDatabase()
    {
        // Arrange
        var bookId = 1;
        var updatedBook = new Books
        {
            Id = bookId,
            Title = "Updated Test Book",
            Author = "Updated Test Author",
            Price = 25,
            isbn = 1234567890
        };

        // Act
        var result = await _bookservice.UpdateBookAsync(bookId, updatedBook);

        // Assert
        
        Assert.Multiple(() => {
            Assert.NotNull(result);
            Assert.Equal(25, result.Book.Price);
        });
    }
    
    [Fact]
    public async Task GetBooksAsync_ForExistingbooks_ReturnsAllBooks()
    {
        // Arrange
        

        // Act
        var result = await _bookservice.GetBooksAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
       // Assert.Contains(result, (item) => item.Id == 1);
    }

    
}