public class UserToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Token { get; set; }
    public DateTime ExpiryDate { get; set; }
}
