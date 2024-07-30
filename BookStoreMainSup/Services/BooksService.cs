using BookStoreMainSup.Data;
using BookStoreMainSup.Models;
using BookStoreMainSup.Helper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

public class BooksService: IBooksService
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
        if (!IsValidISBN(book.isbn))
        {
            validationMessage = "The length of ISBN should be greater than or Equal to 10 and less than or equal to 13.";
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

    public async Task<UpdateMessages> UpdateBookAsync(int id, Books book)
    {
        // Setting the ID from the URL to book object
        book.Id = id;

        if (id != book.Id)
        {
            return new UpdateMessages { IsSuccess = false, ErrorMessage = UpdateMessages.idMatch };
        }

        if (book.isbn == 0) // Assuming ISBN is a long or int and not provided as a string
        {
            return new UpdateMessages { IsSuccess = false, ErrorMessage = UpdateMessages.isbnCheck };
        }

        if (string.IsNullOrEmpty(book.isbn.ToString()))
        {
            return new UpdateMessages { IsSuccess = false, ErrorMessage = UpdateMessages.isbnCheck };
        }

        if (!IsPositive(book.Price))
        {
            return new UpdateMessages { IsSuccess = false, ErrorMessage = UpdateMessages.priceCheck };
        }

        if (!IsValidISBN(book.isbn))
        {
            return new UpdateMessages { IsSuccess = false, ErrorMessage = UpdateMessages.isbnLength };
        }

        if (!IsValidTitle(book.Title))
        {
            return new UpdateMessages { IsSuccess = false, ErrorMessage = UpdateMessages.titleCheck };
        }

        if (!IsValidTitle(book.Author))
        {
            return new UpdateMessages { IsSuccess = false, ErrorMessage = UpdateMessages.authorCheck };
        }

        // Retrieve existing book data
        var existingBook = await _context.Books.FindAsync(id);
        if (existingBook == null)
        {
            return new UpdateMessages { IsSuccess = false, ErrorMessage = UpdateMessages.bookNotFound };
        }

        // Checking whether same ISBN number is updating
        var availableISBN = await _context.Books.AnyAsync(b => b.isbn == book.isbn && b.Id != id);
        if (availableISBN)
        {
            return new UpdateMessages { IsSuccess = false, ErrorMessage = UpdateMessages.sameISBN };
        }

        // Detach the existing entity to avoid tracking issues
        _context.Entry(existingBook).State = EntityState.Detached;

        _context.Entry(book).State = EntityState.Modified;

        await _context.SaveChangesAsync();

        return new UpdateMessages { IsSuccess = true, Book = book };
    }

    public bool IsValidIsbn(long isbn)
    {
        // Add your ISBN validation logic here
        // For example, check if it is 10 or 13 characters long and consists only of digits
        if (isbn.ToString().Length == 10 || isbn.ToString().Length == 13)
        {
            return isbn.ToString().All(char.IsDigit);
        }
        return false;
    }

    public async Task<bool> DeleteBookByIsbnAsync(long isbn)
    {
        var result = await _context.Database.ExecuteSqlRawAsync("DELETE FROM Books WHERE isbn = {0}", isbn);
        return result > 0;
    }

    private bool IsPositive(double value) => value > 0;
    private bool IsValidISBN(long isbn) => isbn.ToString().Length >= 10 && isbn.ToString().Length <= 13;
    private bool IsValidTitle(string title) => title.Length <= 25;
}