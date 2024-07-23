using BookStoreMainSup.Data;
using BookStoreMainSup.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

public class BooksService
{
    private readonly ApplicationDbContext _context;

    public BooksService(ApplicationDbContext db)
    {
        _context = db;
    }

    public async Task<List<Books>> GetBooksAsync()
    {
        return await _context.Books.ToListAsync();
    }

    public async Task<Books> GetBookByIdAsync(int id)
    {
        return await _context.Books.FindAsync(id);
    }

    public BooksDto MapBookToDto(Books book)
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

    public async Task<bool> BookExistsAsync(string isbn)
    {
        return await _context.Books.AnyAsync(b => b.isbn.ToString() == isbn);
    }

    public async Task AddBookAsync(Books book)
    {
        _context.Books.Add(book);
        await _context.SaveChangesAsync();
    }

    public bool ValidateBook(Books book, out string validationMessage)
    {
        validationMessage = string.Empty;

        if (string.IsNullOrEmpty(book.Title) || string.IsNullOrEmpty(book.Author))
        {
            validationMessage = "Title or Author fields are required.";
            return false;
        }
        if (book.Price <= 0)
        {
            validationMessage = "Price must be greater than zero.";
            return false;
        }
        if (book.Discount <= 0)
        {
            validationMessage = "Discount should be greater than 0.";
            return false;
        }
        if (book.Discount > book.Price)
        {
            validationMessage = "Price must be greater than Discount.";
            return false;
        }
        if (book.isbn.ToString().Length <= 10)
        {
            validationMessage = "The length of ISBN should be greater than 10.";
            return false;
        }
        if (book.isbn.ToString().Length >= 13)
        {
            validationMessage = "The length of ISBN should be less than 13.";
            return false;
        }
        if (!Regex.IsMatch(book.isbn.ToString(), @"^[0-9]+$"))
        {
            validationMessage = "ISBN should be a number.";
            return false;
        }

        return true;
    }
}