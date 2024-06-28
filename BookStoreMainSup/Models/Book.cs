using System.ComponentModel.DataAnnotations;

namespace BookStoreMainSup.Models
{
    public class Book
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string Author { get; set; }
        public double Price { get; set; }
        public string IsbnNumber { get; set; }
        public double Discount { get; set; }


        public int PurchasedCount { get; set; }
    }
}
