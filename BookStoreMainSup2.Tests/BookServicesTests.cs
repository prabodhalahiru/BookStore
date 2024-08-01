using BookStoreMainSup.Controllers;
using BookStoreMainSup.Helper;
using BookStoreMainSup.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace BookStoreMainSup2.Tests
{
    public class BookServicesTests

    {
        private readonly Mock<ILogger<BooksController>> _loggerMock;
        private readonly Mock<IBooksService> _booksServiceMock;
        private readonly BooksController _controller;

        public BookServicesTests()
        {
            _loggerMock = new Mock<ILogger<BooksController>>();
            _booksServiceMock = new Mock<IBooksService>();
            _controller = new BooksController(_loggerMock.Object, _booksServiceMock.Object);
        }

        //get method unit tests

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

        //update method unit tests

        [Fact]
        public async Task PutBook_ReturnsOkResult_WhenUpdateIsSuccessful()
        {
            // Arrange
            var bookId = 1;
            var book = new Books { Id = bookId, Title = "Updated Book" };
            var serviceResult = new UpdateMessages { IsSuccess = true, Book = book };

            _booksServiceMock.Setup(service => service.UpdateBookAsync(bookId, book)).ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.PutBook(bookId, book);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedBook = Assert.IsType<Books>(okResult.Value);
            Assert.Equal(book, returnedBook);
        }

        [Fact]
        public async Task PutBook_ReturnsBadRequest_WhenUpdateFails()
        {
            // Arrange
            var bookId = 1;
            var book = new Books { Id = bookId, Title = "Updated Book" };
            var serviceResult = new UpdateMessages { IsSuccess = false, ErrorMessage = UpdateMessages.idMatch };

            _booksServiceMock.Setup(service => service.UpdateBookAsync(bookId, book)).ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.PutBook(bookId, book);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var badRequestValue = badRequestResult.Value;

            // Access the anonymous type's properties
            var messageProperty = badRequestValue.GetType().GetProperty("message");
            var messageValue = messageProperty.GetValue(badRequestValue, null).ToString();

            Assert.Equal(UpdateMessages.idMatch, messageValue);
        }

        [Fact]
        public async Task PutBook_ReturnsStatusCode500_OnException()
        {
            // Arrange
            var bookId = 1;
            var book = new Books { Id = bookId, Title = "Updated Book" };

            _booksServiceMock.Setup(service => service.UpdateBookAsync(bookId, book))
                             .ThrowsAsync(new Exception("Database failure"));

            // Act
            var result = await _controller.PutBook(bookId, book);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);

            var statusCodeValue = statusCodeResult.Value;

            // Access the anonymous type's properties
            var messageProperty = statusCodeValue.GetType().GetProperty("message");
            var messageValue = messageProperty.GetValue(statusCodeValue, null).ToString();

            Assert.Equal("Oops! Something went Wrong!", messageValue);
        }

        //post method unit tests

        [Fact]
        public async Task PostBook_ReturnsBadRequest_WhenModelStateIsInvalid()
        {
            // Arrange
            var book = new Books { Title = "Invalid Book" };
            _controller.ModelState.AddModelError("Title", "Title is required");

            // Act
            var result = await _controller.PostBook(book);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var modelState = Assert.IsType<SerializableError>(badRequestResult.Value);
            Assert.True(modelState.ContainsKey("Title"));
        }

        [Fact]
        public async Task PostBook_ReturnsBadRequest_WhenValidationFails()
        {
            // Arrange
            var book = new Books { Title = "Invalid Book" };
            _booksServiceMock.Setup(service => service.ValidateBook(book, out It.Ref<string>.IsAny)).Returns(false)
                .Callback((Books b, out string msg) => { msg = "Validation failed"; });

            // Act
            var result = await _controller.PostBook(book);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var message = badRequestResult.Value.GetType().GetProperty("message").GetValue(badRequestResult.Value, null).ToString();
            Assert.Equal("Validation failed", message);
        }

        //New check

        [Fact]
        public async Task SearchBooks_ReturnsOkResult_WithBooks()
        {
            // Arrange
            var query = "TestQuery";
            var books = new List<Books>
        {
            new Books { Id = 1, Title = "Test Book 1", Author = "Author 1", Price = 10.0, isbn = 1234567890123 },
            new Books { Id = 2, Title = "Test Book 2", Author = "Author 2", Price = 15.0, isbn = 1234567890124 }
        };

            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?query={query}");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            _booksServiceMock.Setup(service => service.SearchBooksAsync(query)).ReturnsAsync(books);

            // Act
            var result = await _controller.SearchBooks();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedBooks = Assert.IsType<List<Books>>(okResult.Value);
            Assert.Equal(books.Count, returnedBooks.Count);
            Assert.Equal(books[0].Title, returnedBooks[0].Title);
            Assert.Equal(books[1].Title, returnedBooks[1].Title);
        }

        [Fact]
        public async Task SearchBooks_ReturnsNotFound_WhenNoRecordsFound()
        {
            // Arrange
            var query = "TestQuery";
            var books = new List<Books>();

            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?query={query}");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            _booksServiceMock.Setup(service => service.SearchBooksAsync(query)).ReturnsAsync(books);

            // Act
            var result = await _controller.SearchBooks();

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var messageProperty = notFoundResult.Value.GetType().GetProperty("message");
            Assert.NotNull(messageProperty);
            var message = messageProperty.GetValue(notFoundResult.Value).ToString();
            Assert.Equal("No records found", message);
        }

        [Fact]
        public async Task SearchBooks_ReturnsBadRequest_OnArgumentException()
        {
            // Arrange
            var query = "TestQuery";

            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?query={query}");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            _booksServiceMock.Setup(service => service.SearchBooksAsync(query))
                .ThrowsAsync(new ArgumentException("Invalid query parameter"));

            // Act
            var result = await _controller.SearchBooks();

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var messageProperty = badRequestResult.Value.GetType().GetProperty("message");
            Assert.NotNull(messageProperty);
            var message = messageProperty.GetValue(badRequestResult.Value).ToString();
            Assert.Equal("Invalid query parameter", message);
        }

        [Fact]
        public async Task SearchBooks_ReturnsStatusCode500_OnException()
        {
            // Arrange
            var query = "TestQuery";

            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?query={query}");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            _booksServiceMock.Setup(service => service.SearchBooksAsync(query))
                .ThrowsAsync(new Exception("Internal server error"));

            // Act
            var result = await _controller.SearchBooks();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            var messageProperty = statusCodeResult.Value.GetType().GetProperty("message");
            Assert.NotNull(messageProperty);
            var message = messageProperty.GetValue(statusCodeResult.Value).ToString();
            Assert.Equal("The server encountered an error and could not complete your request", message);
        }

        [Fact]
        public async Task DeleteBookByIsbn_ReturnsBadRequest_WhenIsbnIsInvalid()
        {
            // Arrange
            var invalidIsbn = "invalidIsbn";

            // Act
            var result = await _controller.DeleteBookByIsbn(invalidIsbn);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var messageProperty = badRequestResult.Value.GetType().GetProperty("message");
            Assert.NotNull(messageProperty);
            var message = messageProperty.GetValue(badRequestResult.Value).ToString();
            Assert.Equal("Invalid ISBN number. It must be a positive numeric value.", message);
        }

        [Fact]
        public async Task DeleteBookByIsbn_ReturnsBadRequest_WhenIsbnNotFound()
        {
            // Arrange
            var validIsbn = "1234567890";
            _booksServiceMock.Setup(service => service.DeleteBookByIsbnAsync(long.Parse(validIsbn))).ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteBookByIsbn(validIsbn);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var messageProperty = badRequestResult.Value.GetType().GetProperty("message");
            Assert.NotNull(messageProperty);
            var message = messageProperty.GetValue(badRequestResult.Value).ToString();
            Assert.Equal($"Book with ISBN {validIsbn} not found.", message);
        }

        [Fact]
        public async Task DeleteBookByIsbn_ReturnsOk_WhenBookIsDeleted()
        {
            // Arrange
            var validIsbn = "1234567890";
            _booksServiceMock.Setup(service => service.DeleteBookByIsbnAsync(long.Parse(validIsbn))).ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteBookByIsbn(validIsbn);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var messageProperty = okResult.Value.GetType().GetProperty("Message");
            Assert.NotNull(messageProperty);
            var message = messageProperty.GetValue(okResult.Value).ToString();
            Assert.Equal($"Book with ISBN {validIsbn} was successfully deleted.", message);
        }

        [Fact]
        public async Task DeleteBookByIsbn_ReturnsStatusCode500_OnException()
        {
            // Arrange
            var validIsbn = "1234567890";
            _booksServiceMock.Setup(service => service.DeleteBookByIsbnAsync(long.Parse(validIsbn)))
                .ThrowsAsync(new Exception("Internal server error"));

            // Act
            var result = await _controller.DeleteBookByIsbn(validIsbn);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            var messageProperty = statusCodeResult.Value.GetType().GetProperty("message");
            Assert.NotNull(messageProperty);
            var message = messageProperty.GetValue(statusCodeResult.Value).ToString();
            Assert.Equal("The server encountered an error and could not complete your request.", message);
        }

        [Fact]
        public async Task SortBooksByPriceRange_ReturnsNotFound_WhenNoBooksInRange()
        {
            // Arrange
            double? minPrice = 10.0;
            double? maxPrice = 50.0;
            string order = "asc";

            _booksServiceMock.Setup(service => service.GetBooksInRange(minPrice, maxPrice)).ReturnsAsync(new List<Books>());

            // Act
            var result = await _controller.SortBooksByPriceRange(minPrice, maxPrice, order);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var messageProperty = notFoundResult.Value.GetType().GetProperty("message");
            Assert.NotNull(messageProperty);
            var message = messageProperty.GetValue(notFoundResult.Value).ToString();
            Assert.Equal("No Books available in Range", message);
        }

        [Fact]
        public async Task SortBooksByPriceRange_ReturnsOk_WithSortedBooks()
        {
            // Arrange
            double? minPrice = 10.0;
            double? maxPrice = 50.0;
            string order = "asc";
            var booksInRange = new List<Books> { new Books { Title = "Book1", Price = 20 }, new Books { Title = "Book2", Price = 30 } };
            var sortedBooks = new List<Books> { new Books { Title = "Book1", Price = 20 }, new Books { Title = "Book2", Price = 30 } };

            _booksServiceMock.Setup(service => service.GetBooksInRange(minPrice, maxPrice)).ReturnsAsync(booksInRange);
            _booksServiceMock.Setup(service => service.SortBooksByOrder(order, booksInRange)).ReturnsAsync(sortedBooks);

            // Act
            var result = await _controller.SortBooksByPriceRange(minPrice, maxPrice, order);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedBooks = Assert.IsType<List<Books>>(okResult.Value);
            Assert.Equal(sortedBooks, returnedBooks);
        }

        [Fact]
        public async Task SortBooksByPriceRange_ReturnsBadRequest_OnArgumentException()
        {
            // Arrange
            double? minPrice = 10.0;
            double? maxPrice = 50.0;
            string order = "asc";

            _booksServiceMock.Setup(service => service.GetBooksInRange(minPrice, maxPrice)).ThrowsAsync(new ArgumentException("Invalid argument"));

            // Act
            var result = await _controller.SortBooksByPriceRange(minPrice, maxPrice, order);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var messageProperty = badRequestResult.Value.GetType().GetProperty("message");
            Assert.NotNull(messageProperty);
            var message = messageProperty.GetValue(badRequestResult.Value).ToString();
            Assert.Equal("Invalid argument", message);
        }

        [Fact]
        public async Task SortBooksByPriceRange_ReturnsStatusCode500_OnException()
        {
            // Arrange
            double? minPrice = 10.0;
            double? maxPrice = 50.0;
            string order = "asc";

            _booksServiceMock.Setup(service => service.GetBooksInRange(minPrice, maxPrice)).ThrowsAsync(new Exception("Internal server error"));

            // Act
            var result = await _controller.SortBooksByPriceRange(minPrice, maxPrice, order);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            var messageProperty = statusCodeResult.Value.GetType().GetProperty("message");
            Assert.NotNull(messageProperty);
            var message = messageProperty.GetValue(statusCodeResult.Value).ToString();
            Assert.Equal("The server encountered an error and could not complete your request", message);
        }




    }
}