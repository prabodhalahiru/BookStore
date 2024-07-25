using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookStoreMainSup.Data;
using BookStoreMainSup.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookStoreMainSup.Resources;
using System.Text.RegularExpressions;
using System;
using Microsoft.Extensions.Logging;

namespace BookStoreMainSup.Controllers
{
    [Route("api/books")]
    [ApiController]
    public class BooksController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<BooksController> _logger;
        private readonly BooksService _booksService;

        public BooksController(ApplicationDbContext db, ILogger<BooksController> logger)
        {
            _db = db;
            _logger = logger;
            _booksService = new BooksService(db);
        }

        // Get: api/Books
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Books>>> GetBooks()
        {
            try
            {
                var books = await _booksService.GetBooksAsync();

                if (books == null || books.Count == 0)
                {
                    _logger.LogWarning("No books available in the database.");
                    return NotFound(new { message = "No books available in the database." });
                }

                return Ok(books);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting the books.");
                return StatusCode(500, new { message = "Oops! Looks like we tripped over a cable. We'll get back up and running in no time." });
            }
        }

        // Get: api/Books/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<BooksDto>> GetBook(int id)
        {
            try
            {
                var book = await _booksService.GetBookByIdAsync(id);

                if (book == null)
                {
                    _logger.LogWarning($"Book with ID {id} not found.");
                    return NotFound(new { message = $"Book with ID {id} not found." });
                }

                var bookDto = _booksService.MapBookToDto(book);
                return Ok(bookDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting the book.");
                return StatusCode(500, new { message = "Oops! Looks like we tripped over a cable. We'll get back up and running in no time." });
            }
        }

        // PUT: API/Books/{id}
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutBook(int id, Books book)
        {
            _logger.LogInformation($"Updating book with ID {id}");

            try
            {
                var result = await _booksService.UpdateBookAsync(id, book);
                if (result.IsSuccess)
                {
                    return Ok(result.Book);
                }
                else
                {
                    return BadRequest(new { message = result.ErrorMessage });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating the book.");
                return StatusCode(500, new { message = "Oops! Something went Wrong!" });
            }
        }


        // GET: api/Books/search?query=keyword
        [HttpGet("search")]
        [DisableRequestSizeLimit]
        public async Task<ActionResult<IEnumerable<Books>>> SearchBooks()
        {
            try
            {
                var query = HttpContext.Request.Query["query"].ToString();
                var books = await _booksService.SearchBooksAsync(query);

                if (books.Count == 0)
                {
                    return NotFound(new { message = "No records found" });
                }

                return Ok(books);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while searching for books.");
                return StatusCode(500, new { message = "Oops! Looks like we tripped over a cable. We'll get back up and running in no time." });
            }
        }


        // GET: api/books/advancedsearch
        [HttpGet("advancedsearch")]
        [DisableRequestSizeLimit]
        public async Task<ActionResult<IEnumerable<Books>>> AdvancedSearch([FromQuery] string? title, [FromQuery] string? author, [FromQuery] string? isbn)
        {
            try
            {
                var books = await _booksService.AdvancedSearchAsync(title, author, isbn);

                if (books.Count == 0)
                {
                    return NotFound(new { message = "No records found." });
                }

                return Ok(books);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while performing an advanced search for books.");
                return StatusCode(500, new { message = "Oops! Looks like we tripped over a cable. We'll get back up and running in no time." });
            }
        }



        // GET: api/books/sortbyrange?minPrice=1000&maxPrice=2000&order=sales
        [HttpGet("sortbyrange")]
        public async Task<ActionResult<IEnumerable<Books>>> SortBooksByPriceRange(double? minPrice, double? maxPrice, string? order)
        {
            try
            {
                // Filter books by price range
                var booksInRangeResult = await _booksService.GetBooksInRange(minPrice, maxPrice);
                
                if (booksInRangeResult.Count == 0)
                {
                    return NotFound(new { message = "No Books available in Range" });
                }

                // Sort books by order
                var sortedBooks = await _booksService.SortBooksByOrder(order, booksInRangeResult);
                
                return Ok(sortedBooks);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while sorting books by price range.");
                return StatusCode(500, new { message = "Oops! Looks like we tripped over a cable. We'll get back up and running in no time." });
            }
        }

        // POST: api/Books
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<Books>> PostBook(Books book)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!_booksService.ValidateBook(book, out string validationMessage))
            {
                return BadRequest(new { message = validationMessage });
            }

            if (await _booksService.BookExistsAsync(book.isbn.ToString()))
            {
                return BadRequest(new { message = "A book with this ISBN already exists." });
            }

            try
            {
                await _booksService.AddBookAsync(book);
                return StatusCode(201, book);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while saving the book.");
                return StatusCode(500, new { message = "An error occurred while saving your book." });
            }
        }

        // DELETE: api/Books/{isbn}
        [Authorize]
        [HttpDelete("{isbn}")]
        public async Task<IActionResult> DeleteBook(string isbn)
        {
            try
            {
                var isDeleted = await _booksService.DeleteBookByIsbnAsync(isbn);

                if (!isDeleted)
                {
                    return BadRequest(new { message = "Please enter the correct ISBN" });
                }

                return Ok(new { message = "Book deleted successfully", isbn });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting the book.");
                return StatusCode(500, new { message = "Something went wrong!" });
            }
        }
    }
}
