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
        private readonly ApplicationDbContext _context;

        public BooksController(ApplicationDbContext db, ILogger<BooksController> logger, ApplicationDbContext context)
        {
            _db = db;
            _logger = logger;
            _context = context;
        }

        //Get: api/Books
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Books>>> GetBooks()
        {
            var books = await _context.Books.ToListAsync();

            if (books == null || books.Count == 0)
            {
                return NotFound("No books available in the database.");
            }

            return await _db.Books.ToListAsync();
        }

        //Get: api/Books/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<BooksDto>> GetBook(int id)
        {
            var book = await _db.Books.FindAsync(id);

            if (book == null)
            {
                return NotFound();
            }

            await _db.SaveChangesAsync();

            var booksDto = MapBooksdto(book);

            return booksDto;
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

            if (book.isbn.ToString().Length <= 10)
            {
                return BadRequest("The length of ISBN should greater than 10");
            }

            if (book.isbn.ToString().Length >= 13)
            {
                return BadRequest("The length of ISBN should less than 13");
            }

            if (!Regex.IsMatch(book.isbn.ToString(), @"^[0-9]+$"))
            {
                return BadRequest("ISBN should be a number");
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
            // Check if the model state is valid
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (string.IsNullOrEmpty(book.Title) || string.IsNullOrEmpty(book.Author))
            {
                return BadRequest("Title or Author fields are required.");
            }
            if (book.Price <= 0)
            {
                return BadRequest("Price must be greater than zero.");
            }
            if (!(book.Discount > 0))
            {
                return BadRequest("Discount should be greater than 0");
            }
            if (book.Discount > book.Price)
            {
                return BadRequest("Price must be greater than Discount.");
            }
            if (book.isbn.ToString().Length <= 10)
            {
                return BadRequest("The length of ISBN should greater than 10");
            }

            if (book.isbn.ToString().Length >= 13)
            {
                return BadRequest("The length of ISBN should less than 13");
            }

            if (!Regex.IsMatch(book.isbn.ToString(), @"^[0-9]+$"))
            {
                return BadRequest("ISBN should be a number");
            }

            var existingBook = await _db.Books.FirstOrDefaultAsync(b => b.isbn == book.isbn);
            if (existingBook != null)
            {
                return BadRequest("A book with this ISBN already exists.");
            }

            try
            {
                _db.Books.Add(book);
                await _db.SaveChangesAsync();

                // Return 201 Created with the book object
                return StatusCode(201, book);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while saving your book.");
            }
        }

        private bool isValidISBN(string isbn)
        {
            return !string.IsNullOrEmpty(isbn);
        }

        private bool BookExists(int id)
        {
            return _db.Books.Any(e => e.Id == id);
        }

        // Map the Books object to BooksDto object
        private BooksDto MapBooksdto(Books book)
        {
            var part = book.Author.Split(" ");

            var booksDto = new BooksDto
            {
                Title = book.Title,
                Fname = part.Length > 0 ? part[0] : "",
                Lname = part.Length > 1 ? part[1] : "",
                Price = book.Price,
                DiscountPrice = book.Price - (book.Price * book.Discount / 100),
                discount = book.Discount,
            };

            return booksDto;
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
