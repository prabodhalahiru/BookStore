using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BookStoreMainSup.Models;
using BookStoreMainSup.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using static System.Reflection.Metadata.BlobBuilder;

namespace BookStoreMainSup.Controllers
{
    [Route("api/admin")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly AdminService _adminService;
        private readonly AuthService _authService;
        private readonly ILogger<AdminController> _logger;
        private readonly ITokenRevocationService _tokenRevocationService;

        public AdminController(AdminService adminService, AuthService authService, ILogger<AdminController> logger, ITokenRevocationService tokenRevocationService)
        {
            _adminService = adminService;
            _authService = authService;
            _logger = logger;
            _tokenRevocationService = tokenRevocationService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterAdmin(UserDto request)
        {
            try
            {
                var validationResult = await ValidateRegisterAdminAsync(request);
                if (validationResult != null)
                {
                    return validationResult;
                }

                var admin = CreateAdminUser(request);
                await _adminService.AddAdminAsync(admin);

                return Created("", new { message = "Admin registered successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in RegisterAdmin method.");
                return StatusCode(500, new { message = $"Internal server error in RegisterAdmin method: {ex.Message}" });
            }
        }

        private async Task<IActionResult> ValidateRegisterAdminAsync(UserDto request)
        {
            if (!_authService.ValidateUserDto(request, out string validationMessage))
            {
                return BadRequest(new { message = validationMessage });
            }

            if (await _authService.UserExistsByEmail(request.Email) || await _authService.UserExistsByUsername(request.Username))
            {
                return BadRequest(new { message = "Email or Username already exists" });
            }

            if (await _adminService.AdminExists())
            {
                return BadRequest(new { message = "An admin account already exists" });
            }

            return null;
        }

        private User CreateAdminUser(UserDto request)
        {
            return new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                IsAdmin = true
            };
        }


        [HttpGet("loggedinusers")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetLoggedInUsers()
        {
            try
            {
                var userResponses = await GetLoggedInUserResponsesAsync();
                if (userResponses == null || userResponses.Count == 0)
                {
                    return NotFound(new { message = "Currently no logged in users" });
                }

                return Ok(userResponses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetLoggedInUsers method.");
                return StatusCode(500, new { message = $"Internal server error in GetLoggedInUsers method: {ex.Message}" });
            }
        }

        private async Task<List<UserResponseDto>> GetLoggedInUserResponsesAsync()
        {
            var loggedInUsers = await _adminService.GetLoggedInUsersAsync();
            var books = await _adminService.GetBooksCountByUserAsync();

            return loggedInUsers.Select(user => new UserResponseDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                IsLoggedIn = user.IsLoggedIn,
                IsAdmin = user.IsAdmin,
                BooksCreated = books.ContainsKey(user.Id) ? books[user.Id] : 0
            }).ToList();
        }




        [HttpGet("books-count-by-user")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetBooksCountByUser()
        {
            try
            {
                var booksCountByUser = await _adminService.GetBooksCountByUserAsync();
                return Ok(booksCountByUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetBooksCountByUser method.");
                return StatusCode(500, new { message = $"Internal server error in GetBooksCountByUser method: {ex.Message}" });
            }
        }

        [HttpGet("books-by-user/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetBooksByUser(string userId)
        {
            if (!int.TryParse(userId, out int parsedUserId))
            {
                return BadRequest(new { message = "Enter a valid User ID" });
            }

            try
            {
                var validationResult = await ValidateGetBooksByUserAsync(parsedUserId);
                if (validationResult != null)
                {
                    return validationResult;
                }

                var books = await _adminService.GetBooksByUserAsync(parsedUserId);
                if (books == null || books.Count == 0)
                {
                    return NotFound(new { message = "No books found for the given user" });
                }

                return Ok(books);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetBooksByUser method.");
                return StatusCode(500, new { message = $"Internal server error in GetBooksByUser method: {ex.Message}" });
            }
        }

        private async Task<IActionResult> ValidateGetBooksByUserAsync(int userId)
        {
            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "No users matched the id" });
            }

            return null;
        }



        [HttpGet("registered-users")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllRegisteredUsers()
        {
            try
            {
                var users = await _adminService.GetAllRegisteredUsersAsync();
                if (users == null || users.Count == 0)
                {
                    return NotFound(new { message = "No registered users available" });
                }

                var books = await _adminService.GetBooksCountByUserAsync();

                var userResponses = users.Select(user => new UserResponseDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    IsLoggedIn = user.IsLoggedIn,
                    IsAdmin = user.IsAdmin,
                    BooksCreated = books.ContainsKey(user.Id) ? books[user.Id] : 0  // Count of books created
                }).ToList();

                return Ok(userResponses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetAllRegisteredUsers method.");
                return StatusCode(500, new { message = $"Internal server error in GetAllRegisteredUsers method: {ex.Message}" });
            }
        }


        [HttpPost("deactivate-user/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeactivateUser(string userId, [FromBody] UserActivationDto request)
        {
            if (!int.TryParse(userId, out int parsedUserId))
            {
                return BadRequest(new { message = "Enter a valid user id" });
            }

            try
            {
                var result = await DeactivateUserAsync(parsedUserId, request.IsActive);
                if (!result.Success)
                {
                    return result.Result;
                }

                return Ok(new { message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in DeactivateUser method.");
                return StatusCode(500, new { message = $"Internal server error in DeactivateUser method: {ex.Message}" });
            }
        }

        private async Task<(bool Success, IActionResult Result, string Message)> DeactivateUserAsync(int userId, bool isActive)
        {
            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return (false, NotFound(new { message = "No users matched the id" }), null);
            }

            user.IsActive = isActive;
            await _authService.UpdateUserAsync(user);

            if (!isActive && user.IsLoggedIn)
            {
                var userTokens = await _authService.GetUserTokensAsync(user.Id);
                foreach (var token in userTokens)
                {
                    _tokenRevocationService.RevokeToken(token);
                }

                user.IsLoggedIn = false;
                await _authService.UpdateUserAsync(user);
            }

            string status = isActive ? "activated" : "deactivated";
            return (true, null, $"User successfully {status}.");
        }


    }
}
