using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookStoreMainSup.Controllers;
using BookStoreMainSup.Data;
using BookStoreMainSup.Models;

namespace BookStoreMainSup.Tests
{
    public class BooksControllerTests
    {
        // Mock for ApplicationDbContext
        private readonly Mock<ApplicationDbContext> _mockDbContext;

        // Mock for ILogger
        private readonly Mock<ILogger<BooksController>> _mockLogger;

        // Instance of BooksController
        private readonly BooksController _controller;

        public BooksControllerTests()
        {

            //Initialize the mocks
            _mockDbContext = new Mock<ApplicationDbContext>(new DbContextOptions<ApplicationDbContext>());
            _mockLogger = new Mock<ILogger<BooksController>>();
            _controller = new BooksController(_mockDbContext.Object, _mockLogger.Object, _mockDbContext.Object);

        }

        //Indicates a test method
        [Fact]
        public async Task PutBook_ReturnsOkResult()
        {
            //Arrange
            var bookId = 1;
            var book = new Books
            {
                Id = bookId,
                Title = "Test Book",
                Author = "Test Author",
                Price = 20,
                Discount = 5,
                isbn = 1234567890
            };

            // Set up the mock behavior for FindAsync, AnyAsync, and SaveChangesAsync methods
            _mockDbContext.Setup(db => db.Books.FindAsync(bookId)).ReturnsAsync(book);
            // _mockDbContext.Setup(db => db.Books.AnyAsync(b => b.isbn == book.isbn && b.Id != bookId)).ReturnsAsync(false);
            _mockDbContext.Setup(db => db.SaveChangesAsync(default)).ReturnsAsync(1);

            //Act
            var result = await _controller.PutBook(bookId, book);

            //Assert

            //check if the result is of type OkObjectResult
            var okResult = Assert.IsType<OkObjectResult>(result);

            //check if the value of the result is of type books
            var returnValue = Assert.IsType<Books>(okResult.Value);

            //verify if the returned book is the same as the input
            Assert.Equal(book, returnValue);
        }
            


        }
}
