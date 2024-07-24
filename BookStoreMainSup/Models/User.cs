using System.ComponentModel.DataAnnotations;

namespace BookStoreMainSup.Models
{
    public class User
    {
        
        public int Id { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        public bool IsLoggedIn { get; set; }
    }
}
