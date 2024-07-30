using BookStoreMainSup.Controllers;
using BookStoreMainSup.Helper;
using BookStoreMainSup.Models;
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

        //[Fact]
        //public async Task PostBook_ReturnsBadRequest_WhenISBNAlreadyExists()
        //{
        //    // Arrange
        //    var book = new Books { isbn = 1234567890, Title = "Existing ISBN Book" };
        //    _booksServiceMock.Setup(service => service.BookExistsAsync(book.isbn.ToString())).ReturnsAsync(true);

        //    // Act
        //    var result = await _controller.PostBook(book);

        //    // Assert
        //    var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);

        //    // Ensure the result is not null before accessing its properties
        //    Assert.NotNull(badRequestResult.Value);

        //    var message = badRequestResult.Value.GetType().GetProperty("message")?.GetValue(badRequestResult.Value, null)?.ToString();
        //    Assert.Equal("A book with this ISBN already exists.", message);
        //}

        [Fact]
        public async Task PostBook_ReturnsStatusCode201_WhenBookIsCreated()
        {
            // Arrange
            var book = new Books
            {
                isbn = 1234567890,
                Title = "New Book",
                Author = "Author",
                Price = 19.99
            };

            var bookResponse = new BookResponseDto
            {
                Id = 1, // Assuming the ID is assigned after creation
                Title = book.Title,
                Author = book.Author,
                Price = book.Price,
                Isbn = book.isbn
            };

            // Mock the services
            _booksServiceMock.Setup(service => service.BookExistsAsync(book.isbn.ToString())).ReturnsAsync(false);
            _booksServiceMock.Setup(service => service.AddBookAsync(book)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.PostBook(book);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(201, objectResult.StatusCode);

            var returnedBook = Assert.IsType<BookResponseDto>(objectResult.Value);
            Assert.Equal(bookResponse.Id, returnedBook.Id);
            Assert.Equal(bookResponse.Title, returnedBook.Title);
            Assert.Equal(bookResponse.Author, returnedBook.Author);
            Assert.Equal(bookResponse.Price, returnedBook.Price);
            Assert.Equal(bookResponse.Isbn, returnedBook.Isbn);
        }




    }
}