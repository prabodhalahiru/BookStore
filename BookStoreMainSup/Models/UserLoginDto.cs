namespace BookStoreMainSup.Models
{
    public class UserLoginDto
    {
        public string Identifier { get; set; }  // This can be either Username or Email
        public string Password { get; set; }
    }
}
