using System.Collections.Generic;
using System.Linq;
using System;
using BookStoreMainSup.Data;
using BookStoreMainSup.Models;

public class AdminService
{
    private readonly ApplicationDbContext _context;

    public AdminService(ApplicationDbContext context)
    {
        _context = context;
    }

    public IEnumerable<User> GetLoggedUsers()
    {
        // Implement logic to retrieve logged users
        return _context.Users.Where(u => u.IsLoggedIn).ToList();
    }

    //public void PurchaseBook(int userId, int bookId)
    //{
    //    var book = _context.Books.SingleOrDefault(b => b.Id == bookId);
    //    if (book == null)
    //    {
    //        throw new Exception("Book not found");
    //    }

    //    book.PurchaseCount++;
    //    _context.Books.Update(book);
    //    _context.SaveChanges();

    //    var purchase = new Purchase
    //    {
    //        UserId = userId,
    //        BookId = bookId,
    //        PurchaseDate = DateTime.Now
    //    };
    //    _context.Purchases.Add(purchase);
    //    _context.SaveChanges();
    //}
}