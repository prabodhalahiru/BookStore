using BookStoreMainSup.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace BookStoreMainSup.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Books> Books { get; set; }
    }
}
