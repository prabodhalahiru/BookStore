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
}