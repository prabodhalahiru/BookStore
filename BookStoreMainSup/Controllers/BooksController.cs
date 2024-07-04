using BookStoreMainSup.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookStoreMainSup.Models;

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
        [HttpPut("{id}")]
        public async Task<IActionResult> PutBook(int id, Books book)
        {

            book.Id = id;
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

        // POST: api/Books
        [HttpPost]
        public async Task<ActionResult<Books>> PostBook(Books book)
        {
            _db.Books.Add(book);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBook), new { id = book.Id }, book);
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

            var booksDto = new BooksDto();
            booksDto.Title = book.Title;
            booksDto.Fname = part[0];
            booksDto.Lname = part[1];
            booksDto.Price = book.Price;
            booksDto.DiscountPrice = book.Price - (book.Price * newPercentage / 100);
            booksDto.discount = newPercentage;
            booksDto.SellCount = book.SellCount;


            return booksDto;
            

        }

    }
}
