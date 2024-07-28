using BookStoreMainSup.Data;
using BookStoreMainSup.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System;
using System.Threading.Tasks;
using BookStoreMainSup.Resources;
using System.Net.Mail;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;

public class AuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<bool> UserExistsByEmail(string email)
    {
        return await _context.Users.AnyAsync(u => u.Email == email);
    }

    public async Task<bool> UserExistsByUsername(string username)
    {
        return await _context.Users.AnyAsync(u => u.Username == username);
    }

    public async Task AddUserAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
    }

    public async Task<User> GetUserByIdentifierAsync(string identifier)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == identifier || u.Username == identifier);
    }

    public string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email)
            }),
            Expires = DateTime.UtcNow.AddMonths(1),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public bool ValidateUserDto(UserDto request, out string validationMessage)
    {
        validationMessage = string.Empty;

        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
        {
            validationMessage = ErrorMessages.RequiredFields;
            return false;
        }

        if (!IsValidEmail(request.Email))
        {
            validationMessage = ErrorMessages.InvalidEmailFormat;
            return false;
        }

        if (request.Username.Length < 3 || request.Username.Length > 20 || !Regex.IsMatch(request.Username, @"^[a-zA-Z0-9]+$"))
        {
            validationMessage = ErrorMessages.InvalidUsernameFormat;
            return false;
        }

        if (request.Password.Length < 5)
        {
            validationMessage = ErrorMessages.PasswordLength;
            return false;
        }

        if (!Regex.IsMatch(request.Password, @"[a-z]"))
        {
            validationMessage = ErrorMessages.PasswordLowerCha;
            return false;
        }

        if (!Regex.IsMatch(request.Password, @"[A-Z]"))
        {
            validationMessage = ErrorMessages.PasswordUpperCha;
            return false;
        }

        if (!Regex.IsMatch(request.Password, @"\d"))
        {
            validationMessage = ErrorMessages.PasswordNumb;
            return false;
        }

        if (!Regex.IsMatch(request.Password, @"[~!@#$%^&*()\-_=+\[\]{}|;:,.<>?/]"))
        {
            validationMessage = ErrorMessages.PasswordScpecialCha;
            return false;
        }

        return true;
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new MailAddress(email);

            if (addr.Address != email)
            {
                return false;
            }

            // Custom invalid email scenarios
            if (email.Contains("..") || email.Contains(",") || email.Contains("#") || email.Contains("*") || email.Contains("~") || email.Contains("$") || email.Contains("\""))
            {
                return false;
            }

            string domainPart = email.Split('@')[1];
            if (domainPart.StartsWith("-") || domainPart.EndsWith("-") || domainPart.StartsWith(".") || domainPart.EndsWith(".") || domainPart.Contains(" "))
            {
                return false;
            }

            if (domainPart.Split('.').Last().Length < 2)
            {
                return false;
            }

            // Using MimeKit for additional validation
            var emailAddress = new MimeKit.MailboxAddress(string.Empty, email);
            if (emailAddress.Address != email)
            {
                return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<User> GetUserByUsernameAsync(string username)
    {
        return await _context.Users.SingleOrDefaultAsync(u => u.Username == username);
    }

    public async Task UpdateUserAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        return await _context.Users.ToListAsync();
    }

    public async Task<User> GetUserByIdAsync(int id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task DeleteUserAsync(int id)
    {
        var user = await GetUserByIdAsync(id);
        if (user != null)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
    }

}
