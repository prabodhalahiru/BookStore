public class UserResponseDto
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public bool IsLoggedIn { get; set; }
    public bool IsAdmin { get; set; }
    public int BooksCreated { get; set; }
}
