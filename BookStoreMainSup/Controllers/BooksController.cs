using BookStoreMainSup.Data;
using BookStoreMainSup.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace BookStoreMainSup.Controllers
{
    [ApiController]
    [Route("api/books")]
    public class BooksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public BooksController(ApplicationDbContext context)
        {
            _context = context;

        }

        [HttpGet]
        public ActionResult<IEnumerable<Book>> GetBooks()
        {
            IEnumerable<Book> objBookList = _context.Books.ToList();
            return Ok(objBookList);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Book>> GetBook(int id)
        {
            var book = _context.Books.Find(id);
            if (book == null)
            {
                return NotFound();
            }
            book.PurchasedCount = book.PurchasedCount + 1;
            //_context.Entry(book).State = EntityState.Modified;
            _context.Books.Update(book);
            await _context.SaveChangesAsync();

            var bookDto = ViewBook(book);

            return Ok(bookDto);
        }

        private BookDto ViewBook(Book book)
        {                      
            var(discountPercent, discountedPrice) = GetDiscounts(book);

            var bookDto = GetBookDto(book, discountPercent, discountedPrice);

            return bookDto;

        }

        private (double discountPercent, double discountedPrice) GetDiscounts(Book book)
        {
            double discountPercent = book.Discount + (5 * (book.PurchasedCount - 3));
            if (discountPercent < book.Discount)
            {
                discountPercent = book.Discount;
            }
            else if (discountPercent > 50)
            {
                discountPercent = 50;
            }

            double discountedPrice = book.Price - (book.Price * (discountPercent / 100));

            return (discountPercent, discountedPrice);
        }

        private BookDto GetBookDto(Book book, double discountPercent, double discountedPrice)
        {
            var authorName = book.Author.Split(' ');

            var bookDto = new BookDto()
            {
                Id = book.Id,
                Name = book.Name,
                IsbnNumber = book.IsbnNumber,
                Discount = discountPercent,
                Price = book.Price,
                PurchasedCount = book.PurchasedCount,

                DiscountedPrice = discountedPrice,
                FirstName = authorName[0],
                LastName = authorName[1]

            };

            return bookDto;
        }

        [HttpPost]
        public async Task<ActionResult<Book>> PostBook(Book book)
        {
            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            //    return CreatedAtAction("PostBook", new { id = book.Id }, book);
            return CreatedAtAction(nameof(PostBook), new { id = book.Id }, book);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBook(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null)
            {
                return NotFound();
            }

            _context.Books.Remove(book);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
