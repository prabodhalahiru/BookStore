using BookStore_Practice2Web.Models;
using Microsoft.EntityFrameworkCore;

namespace BookStore_Practice2Web.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)

        {

        }

        public DbSet<BookCategory> BookCategories { get; set; }
    }
}
