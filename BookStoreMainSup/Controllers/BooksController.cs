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
using static System.Reflection.Metadata.BlobBuilder;

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

        //Get: api/Books
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
                return StatusCode(500, new { message = "Internal server error. Please try again later." });
            }
        }

        //Get: api/Books/{id}
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
                return StatusCode(500, new { message = "Internal server error. Please try again later." });
            }
        }

        //PUT: API/Books/{id}
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutBook(int id, Books book)
        {
            _logger.LogInformation($"Updating book with ID {id}");

            //Setting the ID from the URL to book object
            book.Id = id;

            if (id != book.Id)
            {
                return BadRequest("The ID in the URL does not match the ID in the body.");
            }

            if (string.IsNullOrEmpty(book.isbn.ToString()))
            {
                return BadRequest("You should enter ISBN Number");
            }
            
            if(!(book.Price > 0))
            {
                return BadRequest("Price should be greater than 0");
            }

            if (!(book.Discount > 0))
            {
                return BadRequest("Discount should be greater than 0");
            }

            if (book.Discount > book.Price)
            {
                return BadRequest("Price must be greater than Discount.");
            }

            if (book.isbn.ToString().Length < 10 || book.isbn.ToString().Length > 13)
            {
                return BadRequest("The length of ISBN should greater than 10 and less than 13");
            }

            //Retrieve excisting book data
            var existingBook = await _db.Books.FindAsync(id);
            if ((existingBook == null))
            {
                return NotFound();
            }

            //Checking whether same isbn number is updating
            var availableISBN = await _db.Books.AnyAsync(b => b.isbn == book.isbn && b.Id != id);
            if (availableISBN)
            {
                return BadRequest("This ISBN is available. ISBN should be unique");
            }

            // Detach the existing entity to avoid tracking issues
            _db.Entry(existingBook).State = EntityState.Detached;

            _db.Entry(book).State = EntityState.Modified;

            try
            {
                await _db.SaveChangesAsync();
            }
            catch
            {
                if (!BookExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return Ok(book);
        }

        private bool BookExists(int id)
        {
            return _db.Books.Any(e => e.Id == id);
        }

        // GET: api/Books/search?query=keyword
        [HttpGet("search")]
        [DisableRequestSizeLimit]
        public async Task<ActionResult<IEnumerable<Books>>> SearchBooks()
        {
            var query = HttpContext.Request.Query["query"].ToString();

            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(new { message = ErrorMessages.KeywordRequired });
            }

            if (!Regex.IsMatch(query, @"^[a-zA-Z0-9\s]+$"))
            {
                return BadRequest(new { message = ErrorMessages.InvalidKeywordFormat });
            }

            // Split the query into individual words and convert to lowercase
            var words = query.Split(new char[] { ' ', '\u200E' }, StringSplitOptions.RemoveEmptyEntries)
                             .Select(word => word.ToLower());

            // Build the query to filter the books in the database
            var filteredBooksQuery = _db.Books.AsQueryable();

            foreach (var word in words)
            {
                // Filter the books based on the title, author, and ISBN using the LIKE operator
                filteredBooksQuery = filteredBooksQuery.Where(b =>
                    EF.Functions.Like(b.Title.ToLower(), $"%{word}%") ||
                    EF.Functions.Like(b.Author.ToLower(), $"%{word}%"));
            }

            var filteredBooks = await filteredBooksQuery.ToListAsync();

            if (filteredBooks.Count == 0)
            {
                return NotFound(new { message = "No Records found" });
            }

            return Ok(filteredBooks);
        }

        // GET: api/books/sortbyrange?minPrice=1000&maxPrice=2000&order=sales
        [HttpGet("sortbyrange")]
        public async Task<ActionResult<IEnumerable<Books>>> SortBooksByPriceRange(double? minPrice, double? maxPrice, string? order)
        {
            if (!minPrice.HasValue || !maxPrice.HasValue)
            {
                return BadRequest("minPrice or maxPrice cannot be Null");
            }
            if (minPrice < 0 || maxPrice < 0)
            {
                return BadRequest("Price should have a positive value");
            }
            else if (minPrice > maxPrice) 
            {
                return BadRequest("maxPrice should be greater than minPrice");
            }     
            if (string.IsNullOrEmpty(order))
            {
                order = "asc";
            } 

            try
            {
                var allBooks = await _db.Books.ToListAsync();

                //Filter books by price range
                var booksInRange = allBooks.Where(b => b.Price >= minPrice && b.Price <= maxPrice).ToList();

                //Sort books by order
                List<Books> sortedBooks;
                if (order.ToLower() == "desc")
                {
                    sortedBooks = booksInRange.OrderByDescending(b => b.Price).ToList();
                }
                else if (order.ToLower() == "asc")
                {
                    sortedBooks = booksInRange.OrderBy(b => b.Price).ToList();
                }
                else
                {
                    return BadRequest("Invalid order method");
                }

                return Ok(sortedBooks);
            }
            catch (Exception ex) 
            { 
                return StatusCode(500, ex.Message);
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

        //Delete Function
        [Authorize]
        [HttpDelete("{isbn}")]
        public async Task<IActionResult> DeleteBook(string isbn)
        {
            var result = await _db.Database.ExecuteSqlRawAsync("DELETE FROM Books WHERE isbn = {0}", isbn);

            if (result == 0)
            {
                return BadRequest("Please Enter the Correct ISBN");
            }

        return Ok(new { Message = "Book deleted successfully", Isbn = isbn });
        }

    }
}
