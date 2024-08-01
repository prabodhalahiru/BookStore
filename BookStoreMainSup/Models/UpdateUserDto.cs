public class UpdateUserDto
{
    public string? Username { get; set; }
    public string? Email { get; set; }
}


public class UpdatePasswordDto
{
    public string OldPassword { get; set; }
    public string NewPassword { get; set; }
}
