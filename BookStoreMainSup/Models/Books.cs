namespace BookStoreMainSup.Models
{
    public class Books
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public double Price { get; set; }
        public string isbn { get; set; }
        public double Discount { get; set; }
        public int SellCount { get; set; }

    }
}
