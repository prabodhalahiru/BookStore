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
using Microsoft.Extensions.Logging;
using BookStoreMainSup.Controllers;
using BookStoreMainSup.Services;

public class AuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger; // Add logger dependency
    private readonly ITokenRevocationService _tokenRevocationService;

    public AuthService(ApplicationDbContext context, IConfiguration configuration, ILogger<AuthService> logger, ITokenRevocationService tokenRevocationService)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _tokenRevocationService = tokenRevocationService;
    }

    public async Task<List<string>> GetUserTokensAsync(int userId)
    {
        var tokens = await _context.UserTokens.Where(ut => ut.UserId == userId).Select(ut => ut.Token).ToListAsync();
        return tokens;
    }

    public async Task AddTokenAsync(UserToken userToken)
    {
        _context.UserTokens.Add(userToken);
        await _context.SaveChangesAsync();
    }


    public async Task<bool> UserExistsByEmail(string email)
    {
        return await _context.Users.AnyAsync(u => u.Email == email);
    }

    public async Task UpdateUserAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task<List<User>> GetLoggedInUsersAsync()
    {
        return await _context.Users.Where(u => u.IsLoggedIn).ToListAsync();
    }


    public async Task<User> GetUserByTokenAsync(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            // Ensure the token has the expected claim
            var userIdClaim = jwtToken.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier || claim.Type == "nameid");
            if (userIdClaim == null)
            {
                throw new InvalidOperationException("Token does not contain user ID claim");
            }

            var userId = int.Parse(userIdClaim.Value);
            return await _context.Users.FindAsync(userId);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error parsing token: {ex.Message}", ex);
        }
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
        var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Email, user.Email)
    };

        if (user.IsAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        // Save the token in the database
        var userToken = new UserToken
        {
            UserId = user.Id,
            Token = tokenString,
            ExpiryDate = tokenDescriptor.Expires.Value
        };
        AddTokenAsync(userToken).Wait();

        return tokenString;
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

    public bool IsValidEmail(string email)
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

    public void LogTokenClaims(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        var claims = jwtToken.Claims.Select(claim => new { claim.Type, claim.Value });

        foreach (var claim in claims)
        {
            _logger.LogInformation($"Claim Type: {claim.Type}, Claim Value: {claim.Value}");
        }
    }

    public async Task<User> GetUserByIdAsync(int userId)
    {
        return await _context.Users.FindAsync(userId);
    }


    public bool ValidatePassword(string password, out string validationMessage)
    {
        validationMessage = string.Empty;

        if (password.Length < 5)
        {
            validationMessage = ErrorMessages.PasswordLength;
            return false;
        }

        if (!Regex.IsMatch(password, @"[a-z]"))
        {
            validationMessage = ErrorMessages.PasswordLowerCha;
            return false;
        }

        if (!Regex.IsMatch(password, @"[A-Z]"))
        {
            validationMessage = ErrorMessages.PasswordUpperCha;
            return false;
        }

        if (!Regex.IsMatch(password, @"\d"))
        {
            validationMessage = ErrorMessages.PasswordNumb;
            return false;
        }

        if (!Regex.IsMatch(password, @"[~!@#$%^&*()\-_=+\[\]{}|;:,.<>?/]"))
        {
            validationMessage = ErrorMessages.PasswordScpecialCha;
            return false;
        }

        return true;
    }


}
