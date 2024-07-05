using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookStoreMainSup.Data;
using BookStoreMainSup.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookStoreMainSup.Controllers
{
    [Route("api/books")]
    [ApiController]
    public class BooksController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public BooksController(ApplicationDbContext db)
        {
            _db = db;
        }

        //Get: api/Books
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Books>>> GetBooks()
        {
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

            UpdateBookSellCount(book);
            await _db.SaveChangesAsync();

            double newPercentage = CalculateDiscount(book);
            var booksDto = MapBooksdto(book, newPercentage);

            return booksDto;
        }

        //PUT: API/Books/{id}
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutBook(int id, Books book)
        {
            if (id != book.Id)
            {
                return BadRequest();
            }

            _db.Entry(book).State = EntityState.Modified;

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
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

        //// GET: api/Books/search?query=keyword
        //[HttpGet("search")]
        //public async Task<ActionResult<IEnumerable<Books>>> SearchBooks(string query)
        //{
        //    if (string.IsNullOrEmpty(query))
        //    {
        //        return BadRequest("Query parameter is required.");
        //    }

        //    // Split the query into individual words
        //    var words = query.Split(' ');

        //    // Build the query to search for each word in the title or author or else isbn
        //    var booksQuery = _db.Books.AsQueryable();

        //    var predicate = PredicateBuilder.False<Books>();
        //    foreach (var word in words)
        //    {
        //        var temp = word;
        //        predicate = predicate.Or(b => b.Title.Contains(temp) || b.Author.Contains(temp) || b.isbn.Contains(temp));
        //    }

        //    booksQuery = booksQuery.Where(predicate);

        //    var books = await booksQuery.ToListAsync();

        //    return Ok(books);
        //}

        // GET: api/Books/search?query=keyword
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Books>>> SearchBooks(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return BadRequest("Query parameter is required.");
            }

            // Fetch all books from the database
            var allBooks = await _db.Books.ToListAsync();

            // Split the query into individual words
            var words = query.Split(' ');

            // Filter the books in memory
            var filteredBooks = allBooks
                .Where(b => words.Any(word => b.Title.Contains(word) || b.Author.Contains(word)))
                .ToList();

            return Ok(filteredBooks);
        }

        // POST: api/Books
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<Books>> PostBook(Books book)
        {
            _db.Books.Add(book);
            await _db.SaveChangesAsync();

            // Return 201 Created with the book object
            return StatusCode(201, book);
        }

        private void UpdateBookSellCount(Books book)
        {
            book.SellCount = book.SellCount + 1;
            _db.Entry(book).State = EntityState.Modified;
        }

        private bool BookExists(int id)
        {
            return _db.Books.Any(e => e.Id == id);
        }

        private double CalculateDiscount(Books book)
        {
            double newPercentage = book.Discount + (5 * (book.SellCount - 3));

            if (newPercentage < book.Discount)
            {
                newPercentage = book.Discount;
            }
            else if (newPercentage > 50)
            {
                newPercentage = 50;
            }

            return newPercentage;
        }

        private BooksDto MapBooksdto(Books book, double newPercentage)
        {
            var part = book.Author.Split(" ");

            var booksDto = new BooksDto
            {
                Title = book.Title,
                Fname = part.Length > 0 ? part[0] : "",
                Lname = part.Length > 1 ? part[1] : "",
                Price = book.Price,
                DiscountPrice = book.Price - (book.Price * newPercentage / 100),
                discount = newPercentage,
                SellCount = book.SellCount
            };

            return booksDto;
        }
        //Delete Function
        [Authorize]
        [HttpDelete("{isbn}")]
        public async Task<IActionResult> DeleteBook(string isbn)
        {
            var book = await _db.Books.FirstOrDefaultAsync(b => b.isbn == isbn);

            if (book == null)
            {
                return NotFound();
            }

            _db.Books.Remove(book);
            await _db.SaveChangesAsync();

            return Ok(book);
        }

    }
}
