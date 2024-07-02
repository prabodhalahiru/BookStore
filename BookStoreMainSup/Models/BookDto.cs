using System.ComponentModel.DataAnnotations;

namespace BookStoreMainSup.Models
{
    public class BookDto
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public double Price { get; set; }
        public string IsbnNumber { get; set; }
        public double Discount { get; set; }

        public double DiscountedPrice { get; set; }
        public int PurchasedCount { get; set; }
    }
}
