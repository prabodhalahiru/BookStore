using BookStoreMainSup.Data;
using BookStoreMainSup.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace BookStoreMainSup.Services
{
    public class AdminService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AdminService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<bool> AdminExists()
        {
            return await _context.Users.AnyAsync(u => u.IsAdmin);
        }

        public async Task AddAdminAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        public async Task<List<User>> GetLoggedInUsersAsync()
        {
            return await _context.Users.Where(u => u.IsLoggedIn).ToListAsync();
        }
    }
}
