namespace BookStoreMainSup.Models
{
    public class BookResponse
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string ISBN { get; set; }
        public string PublishedDate { get; set; }
        public double Price { get; set; }
        public double Discount { get; set; } // New field

        public BookResponse(Books book)
        {
            Id = book.Id;
            Title = book.Title;
            Author = book.Author;
            ISBN = book.ISBN;
            PublishedDate = book.PublishedDate.ToString("yyyy-MM-dd");
            Price = book.Price;
            Discount = book.Discount;
        }
    }
}
