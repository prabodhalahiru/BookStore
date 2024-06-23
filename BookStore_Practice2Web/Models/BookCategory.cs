using System.ComponentModel.DataAnnotations;

namespace BookStore_Practice2Web.Models
{
    public class BookCategory
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }

        public int DisplayOrder { get; set; }

        public DateTime CreatedDateTime { get; set; } = DateTime.Now;
    }
}
