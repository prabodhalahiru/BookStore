using BookStoreMainSup.Data;
using BookStoreMainSup.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

public class BookUpdateResult
{
    public bool IsSuccess { get; set; }
    public Books Book { get; set; }
    public string ErrorMessage { get; set; }
    public string price;
}
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
            isbn = book.isbn,
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

    public async Task<List<Books>> GetBooksInRange (double? minPrice, double? maxPrice)
    {
        if (!minPrice.HasValue || !maxPrice.HasValue || minPrice < 0 || maxPrice < 0)
        {
            throw new ArgumentException("Price should have a positive value");
        }

        if (minPrice > maxPrice)
        {
            throw new ArgumentException("maxPrice should be greater than minPrice");
        }

        var allBooks = await _context.Books.ToListAsync();

        var booksInRange = allBooks.Where(b => b.Price >= minPrice && b.Price <= maxPrice).ToList();

        return booksInRange;

    }

    public async Task<List<Books>> SortBooksByOrder(string? order, List<Books> booksInRange)
    {
        if (string.IsNullOrEmpty(order))
        {
            order = "asc";
        }

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
            throw new ArgumentException("Invalid order method");
        }
                     
        return sortedBooks;

    }

    public async Task<BookUpdateResult> UpdateBookAsync(int id, Books book)
    {
        // Setting the ID from the URL to book object
        book.Id = id;

        if (id != book.Id)
        {
            return new BookUpdateResult { IsSuccess = false, ErrorMessage = "The ID in the URL does not match the ID in the body." };
        }

        if (string.IsNullOrEmpty(book.isbn.ToString()))
        {
            return new BookUpdateResult { IsSuccess = false, ErrorMessage = "You should enter ISBN Number" };
        }

        if (!IsPositive(book.Price))
        {
            return new BookUpdateResult { IsSuccess = false, ErrorMessage = "Price should be greater than 0" };
        }

        if (book.isbn.ToString().Length < 10 || book.isbn.ToString().Length > 13)
        {
            return new BookUpdateResult { IsSuccess = false, ErrorMessage = "The length of ISBN should be greater than 10 and less than 13" };
        }

        // Retrieve existing book data
        var existingBook = await _context.Books.FindAsync(id);
        if (existingBook == null)
        {
            return new BookUpdateResult { IsSuccess = false, ErrorMessage = "Book not found." };
        }

        // Checking whether same ISBN number is updating
        var availableISBN = await _context.Books.AnyAsync(b => b.isbn == book.isbn && b.Id != id);
        if (availableISBN)
        {
            return new BookUpdateResult { IsSuccess = false, ErrorMessage = "This ISBN is available. Please update a unique ISBN" };
        }

        // Detach the existing entity to avoid tracking issues
        _context.Entry(existingBook).State = EntityState.Detached;

        _context.Entry(book).State = EntityState.Modified;

        await _context.SaveChangesAsync();

        return new BookUpdateResult { IsSuccess = true, Book = book };
    }

    public async Task<bool> DeleteBookByIsbnAsync(long isbn)
    {
        var book = await _context.Books.FirstOrDefaultAsync(b => b.isbn == isbn);
        if (book == null)
        {
            return false;
        }

        _context.Books.Remove(book);
        await _context.SaveChangesAsync();

        return true;
    }

    //public bool ValidatePrice(double price, out string validationMessage)
    //{
    //    validationMessage = string.Empty;

    //    // Ensure the price is a positive double
    //    if (price.GetType() == typeof(string))
    //    {
    //        validationMessage = "Price must be a positive double value.";
    //        return false;
    //    }

    //    return true;
    //}

    private bool IsPositive(double value) => value > 0;
    //private bool IsValidISBN(int isbn) => isbn.ToString().Length >= 10 && isbn.ToString().Length <= 13;
}