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

        // GET: api/Books/search?query=keyword
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Books>>> SearchBooks(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("Query parameter is required.");
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
                    EF.Functions.Like(b.Author.ToLower(), $"%{word}%") ||
                    EF.Functions.Like(b.isbn.ToLower(), $"%{word}%"));
            }

            var filteredBooks = await filteredBooksQuery.ToListAsync();

            return Ok(filteredBooks);
        }


      
        // GET: api/books/sortbyrange?minPrice=1000&maxPrice=2000&order=sales
        [HttpGet("sortbyrange")]
        public async Task<ActionResult<IEnumerable<Books>>> SortBooksByPriceRange(double minPrice, double maxPrice, string order = "asc")
        {
            if (minPrice < 0 || maxPrice < 0)
            {
                return BadRequest("Price should have a positive value");
            }
            else if (minPrice > maxPrice) 
            {
                return BadRequest("maxPrice should be greater than minPrice");
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
                else if (order.ToLower() == "sales")
                {
                    sortedBooks = booksInRange.OrderByDescending(b => b.SellCount).ToList();
                }
                else
                {
                    sortedBooks = booksInRange.OrderBy(b => b.Price).ToList();
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
            _db.Books.Add(book);
            await _db.SaveChangesAsync();

            // Return 201 Created with the book object
            return StatusCode(201, book);
        }

        // Increment the sell count of the book
        private void UpdateBookSellCount(Books book)
        {
            book.SellCount = book.SellCount + 1;
            _db.Entry(book).State = EntityState.Modified;
        }

        private bool BookExists(int id)
        {
            return _db.Books.Any(e => e.Id == id);
        }

        // Calculate the discount based on the number of books sold
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

        // Map the Books object to BooksDto object
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
