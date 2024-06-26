using Microsoft.AspNetCore.Mvc;
using BookStoreMainSup.Data;
using BookStoreMainSup.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace BookStoreMainSup.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BooksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BooksController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Books
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BookResponse>>> GetBooks()
        {
            var books = await _context.Books.ToListAsync();
            var bookResponses = books.Select(book => new BookResponse(book)).ToList();
            return Ok(bookResponses);
        }

        // POST: api/Books
        [HttpPost]
        public async Task<ActionResult<BookResponse>> PostBook(Books book)
        {
            book.PublishedDate = book.PublishedDate.Date;
            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            var bookResponse = new BookResponse(book);
            return CreatedAtAction(nameof(GetBooks), new { id = book.Id }, bookResponse);
        }
    }
}
