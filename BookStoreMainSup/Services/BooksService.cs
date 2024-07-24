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

    public async Task<List<Books>> SearchBooksAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return await GetBooksAsync();
        }

        if (!Regex.IsMatch(query, @"^[a-zA-Z0-9\s]+$"))
        {
            throw new ArgumentException("Invalid keyword format.");
        }

        // Split the query into individual words and convert to lowercase
        var words = query.Split(new char[] { ' ', '\u200E' }, StringSplitOptions.RemoveEmptyEntries)
                         .Select(word => word.ToLower());

        // Build the query to filter the books in the database
        var filteredBooksQuery = _context.Books.AsQueryable();

        foreach (var word in words)
        {
            // Filter the books based on the title, author, and ISBN using the LIKE operator
            filteredBooksQuery = filteredBooksQuery.Where(b =>
                EF.Functions.Like(b.Title.ToLower(), $"%{word}%") ||
                EF.Functions.Like(b.Author.ToLower(), $"%{word}%") ||
                EF.Functions.Like(b.isbn.ToString(), $"%{word}%"));
        }

        return await filteredBooksQuery.ToListAsync();
    }

    public async Task<List<Books>> AdvancedSearchAsync(string? title, string? author, string? isbn)
    {
        var query = _context.Books.AsQueryable();

        if (!string.IsNullOrWhiteSpace(title))
        {
            query = query.Where(b => EF.Functions.Like(b.Title, $"%{title}%"));
        }

        if (!string.IsNullOrWhiteSpace(author))
        {
            query = query.Where(b => EF.Functions.Like(b.Author, $"%{author}%"));
        }

        if (!string.IsNullOrWhiteSpace(isbn))
        {
            query = query.Where(b => EF.Functions.Like(b.isbn.ToString(), $"%{isbn}%"));
        }

        return await query.ToListAsync();
    }
}