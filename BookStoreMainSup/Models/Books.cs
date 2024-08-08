using System.ComponentModel.DataAnnotations;

namespace BookStoreMainSup.Models
{
    public class Books
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Author { get; set; }

        [Required]
        public double Price { get; set; }

        [Required]
        public long isbn { get; set; }
        public int CreatedBy { get; set; }
    }
}
