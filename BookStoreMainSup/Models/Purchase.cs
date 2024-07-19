namespace BookStoreMainSup.Models
{
    public class Purchase
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int BookId { get; set; }
        public DateTime PurchaseDate { get; set; }

        public User User { get; set; }
        public Books Book { get; set; }
    }
}
