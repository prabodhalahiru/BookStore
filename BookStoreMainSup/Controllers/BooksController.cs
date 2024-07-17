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

            //if (!ModelState.IsValid)
            //{
            //    return BadRequest(ModelState);
            //}

            //if (!isValidISBN(book.isbn))
            //{
            //    return BadRequest("Invalid ISBN");
            //}

            //Setting the ID from the URL to book object
            book.Id = id;

            if (id != book.Id)
            {
                return BadRequest("The ID in the URL does not match the ID in the body.");
            }

            if (string.IsNullOrEmpty(book.isbn))
            {
                return BadRequest("You should enter ISBN Number");
            }
            
            if(!(book.Price > 0))
            {
                return BadRequest("Price should be greater than 0");
            }

            //Retrieve excisting book data
            var existingBook = await _db.Books.FindAsync(id);
            if ((existingBook == null))
            {
                return NotFound();
            }

            //Checking whether sellcount has modified
            if(existingBook.SellCount != book.SellCount)
            {
                return BadRequest("You cannot update the sellcount");
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
                    EF.Functions.Like(b.Author.ToLower(), $"%{word}%") ||
                    EF.Functions.Like(b.isbn.ToLower(), $"%{word}%"));
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

        private bool isValidISBN(string isbn)
        {
            return !string.IsNullOrEmpty(isbn);
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
            var result = await _db.Database.ExecuteSqlRawAsync("DELETE FROM Books WHERE isbn = {0}", isbn);

            if (result == 0)
            {
                return BadRequest("Please Enter the Correct ISBN");
            }

        return Ok(new { Message = "Book deleted successfully", Isbn = isbn });
        }

    }
}
