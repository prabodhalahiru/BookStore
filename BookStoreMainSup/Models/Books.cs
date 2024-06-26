using System;
using System.ComponentModel.DataAnnotations;

namespace BookStoreMainSup.Models
{
    public class Books
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string ISBN { get; set; }

        [DataType(DataType.Date)]
        public DateTime PublishedDate { get; set; }

        public double Price { get; set; }
        public double Discount { get; set; } // New field
    }
}
