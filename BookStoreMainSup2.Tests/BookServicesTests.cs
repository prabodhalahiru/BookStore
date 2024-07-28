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
    //  private readonly ILogger<BooksController> _logger;
    //  private readonly BooksController _controller;
    private readonly BooksService _booksService;

    public BookServicesTests()
    {
        // Set up the in-memory database options
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "BookStoreTest")
            .Options;

        // Initialize the context with the in-memory options
        _context = new ApplicationDbContext(options);

        // Use a real logger or mock if needed
      //  _logger = new Mock<ILogger<BooksController>>().Object;
     //   _controller = new BooksController(_context, _logger);

        // Seed the database with initial data if necessary
        SeedDatabase();
        _booksService = new BooksService(_context);
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
    public async Task PutBook_ReturnsOkResult_WhenBookIsUpdated()
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
        var result = await _booksService.UpdateBookAsync(bookId, updatedBook);
        //var result = await _controller.PutBook(bookId, updatedBook);

        // Assert

        Assert.Multiple(() =>
        {
            Assert.NotNull(result);
            Assert.Equal(25, result.Book.Price);
        });
        //var okResult = Assert.IsType<OkObjectResult>(result);
        //var returnValue = Assert.IsType<Books>(okResult.Value);
        //Assert.Equal(updatedBook.Title, returnValue.Title);
    }
    // Additional tests...
}
