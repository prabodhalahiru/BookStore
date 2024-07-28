using BookStoreMainSup.Models;

public interface IBooksService
{
    Task AddBookAsync(Books book);
    Task<List<Books>> AdvancedSearchAsync(string? title, string? author, string? isbn);
    Task<bool> BookExistsAsync(string isbn);
    Task<bool> DeleteBookByIsbnAsync(string isbn);
    Task<Books> GetBookByIdAsync(int id);
    Task<List<Books>> GetBooksAsync();
    BooksDto MapBookToDto(Books book);
    Task<List<Books>> SearchBooksAsync(string query);
    Task<BookUpdateResult> UpdateBookAsync(int id, Books book);
    bool ValidateBook(Books book, out string validationMessage);
}