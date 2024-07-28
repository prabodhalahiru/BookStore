using BookStoreMainSup.Models;

namespace BookStoreMainSup.Helper
{
    public class UpdateMessages
    {
        public bool IsSuccess { get; set; }
        public Books Book { get; set; }
        public string ErrorMessage { get; set; }

        public const string idMatch = "The ID in the URL does not match the ID in the body.";
        public const string sameISBN = "This ISBN is already available. Please update with a unique ISBN";
        public const string bookNotFound = "Book not found.";
        public const string isbnLength = "The length of ISBN should be greater than 10 and less than 13";
        public const string priceCheck = "Price should be greater than 0";
        public const string isbnCheck = "You should enter ISBN Number";
        public const string titleCheck = "Title Should be less than or equal to 15 characters!";
        public const string authorCheck = "Arthor Name Should be less than or equal to 15 characters!";
    }
}
