using BookStoreMainSup.Helper;
using BookStoreMainSup.Models;

public interface IBooksService
{
    Task AddBookAsync(Books book);
    Task<List<Books>> AdvancedSearchAsync(string? title, string? author, string? isbn);
    Task<bool> BookExistsAsync(string isbn);
    Task<bool> DeleteBookByIsbnAsync(long isbn);
    Task<Books> GetBookByIdAsync(int id);
    Task<List<Books>> GetBooksAsync();
    BooksDto MapBookToDto(Books book);
    Task<List<Books>> SearchBooksAsync(string query);
    Task<List<Books>> GetBooksInRange(double? minPrice, double? maxPrice);

    Task<List<Books>> SortBooksByOrder(string? order, List<Books> booksInRange);
    Task<UpdateMessages> UpdateBookAsync(int id, Books book);
    bool ValidateBook(Books book, out string validationMessage);
}