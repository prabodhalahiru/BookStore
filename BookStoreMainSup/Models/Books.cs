namespace BookStoreMainSup.Models
{
    public class Books
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty; // Initialize to avoid null warnings
        public string Author { get; set; } = string.Empty; // Initialize to avoid null warnings
        public decimal Price { get; set; }
        public DateTime PublishDate { get; set; }
    }
}
