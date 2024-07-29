using BookStoreMainSup.Controllers;
using BookStoreMainSup.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace BookServiceTesting
{
    public class BookServiceTesting

    {
        private readonly Mock<ILogger<BooksController>> _loggerMock;
        private readonly Mock<IBooksService> _booksServiceMock;
        private readonly BooksController _controller;

        public BookServiceTesting()
        {
            _loggerMock = new Mock<ILogger<BooksController>>();
            _booksServiceMock = new Mock<IBooksService>();
            _controller = new BooksController(_loggerMock.Object, _booksServiceMock.Object);
        }

        [Fact]
        public async Task GetBooks_ReturnsOkResult_WithBooks()
        {
            // Arrange
            var books = new List<Books> { new Books() }; // Assuming Books is a model class
            _booksServiceMock.Setup(service => service.GetBooksAsync()).ReturnsAsync(books);

            // Act
            var result = await _controller.GetBooks();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnBooks = Assert.IsType<List<Books>>(okResult.Value);
            Assert.Equal(books, returnBooks);
        }

        [Fact]
        public async Task GetBooks_ReturnsNotFoundResult_WhenNoBooks()
        {
            // Arrange
            _booksServiceMock.Setup(service => service.GetBooksAsync()).ReturnsAsync(new List<Books>());

            // Act
            var result = await _controller.GetBooks();

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var message = notFoundResult.Value;
            Assert.NotNull(message);

            var messageProperty = message.GetType().GetProperty("message");
            Assert.NotNull(messageProperty);
            Assert.Equal("No books available in the database.", messageProperty.GetValue(message));
        }

        [Fact]
        public async Task GetBooks_ReturnsInternalServerError_OnException()
        {
            // Arrange
            _booksServiceMock.Setup(service => service.GetBooksAsync()).ThrowsAsync(new Exception("Database failure"));

            // Act
            var result = await _controller.GetBooks();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);

            // Using dynamic to access the properties of the anonymous type
            var errorMessage = statusCodeResult.Value;
            Assert.NotNull(errorMessage);
            Assert.Equal("The server encountered an error and could not complete your request", errorMessage.GetType().GetProperty("message").GetValue(errorMessage, null));
        }
    }
}