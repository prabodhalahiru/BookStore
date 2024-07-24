using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using BookStoreMainSup.Data;
using BookStoreMainSup.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BookStoreMainSup.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using System.Text.RegularExpressions;
using BookStoreMainSup.Resources;

namespace BookStoreMainSup.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _configuration;

        //Injecting the TokenRevocationService
        public AuthController(ApplicationDbContext db, IConfiguration configuration, ITokenRevocationService tokenRevocationService)
        {
            _db = db;
            _configuration = configuration;
            _tokenRevocationService = tokenRevocationService;
        }

        private readonly ITokenRevocationService _tokenRevocationService;

        //Register a new user
        [HttpPost("register")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> Register(UserDto request)
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { message = ErrorMessages.RequiredFields });
            }

            if (!Regex.IsMatch(request.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                return BadRequest(new { message = ErrorMessages.InvalidEmailFormat });
            }

            if (request.Username.Length < 3 || request.Username.Length > 20 || !Regex.IsMatch(request.Username, @"^[a-zA-Z0-9]+$"))
            {
                return BadRequest(new { message = ErrorMessages.InvalidUsernameFormat });
            }

            if (request.Password.Length < 5)
            {
                return BadRequest(new { message = ErrorMessages.PasswordLength });
            }

            if (!Regex.IsMatch(request.Password, @"[a-z]"))
            {
                return BadRequest(new { message = ErrorMessages.PasswordLowerCha });
            }

            if (!Regex.IsMatch(request.Password, @"[A-Z]"))
            {
                return BadRequest(new { message = ErrorMessages.PasswordUpperCha });
            }

            if (!Regex.IsMatch(request.Password, @"\d"))
            {
                return BadRequest(new { message = ErrorMessages.PasswordNumb });
            }

            if (!Regex.IsMatch(request.Password, @"[~!@#$%^&*()\-_=+\[\]{}|;:,.<>?/`]"))
            {
                return BadRequest(new { message = ErrorMessages.PasswordScpecialCha });
            }


            var emailExists = await _db.Users.AnyAsync(u => u.Email == request.Email);
            var usernameExists = await _db.Users.AnyAsync(u => u.Username == request.Username);

            if (emailExists && usernameExists)
            {
                return BadRequest(new { message = ErrorMessages.EmailAndUsernameExists });
            }
            if (emailExists)
            {
                return BadRequest(new { message = ErrorMessages.EmailExists });
            }
            if (usernameExists)
            {
                return BadRequest(new { message = ErrorMessages.UsernameExists });
            }

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return Created("", new { message = "User registered successfully" });
        }

        //user login api
        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLoginDto request)
        {
            if (string.IsNullOrEmpty(request.Identifier) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { message = ErrorMessages.RequiredFields });
            }

            var user = await _db.Users
                .FirstOrDefaultAsync(u => (u.Email == request.Identifier || u.Username == request.Identifier));

            if (user == null)
            {
                return Unauthorized(new { message = ErrorMessages.InvalidCredentials});
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = ErrorMessages.InvalidCredentials });
            }

            var token = GenerateJwtToken(user);

            return Ok(new { token });
        }

        //generate jwt token
        private string GenerateJwtToken(User user)
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
                //Expires = DateTime.UtcNow.AddSeconds(20),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        //logout user
        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();
                _tokenRevocationService.RevokeToken(token);
                return Ok(new { message = "User logged out successfully" });
            }

            return BadRequest(new { message = "User not logged in" });
        }
    }
}
