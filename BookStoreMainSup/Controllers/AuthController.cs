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
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;

namespace BookStoreMainSup.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly ITokenRevocationService _tokenRevocationService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(AuthService authService, ITokenRevocationService tokenRevocationService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _tokenRevocationService = tokenRevocationService;
            _logger = logger;
        }

        [HttpPost("register")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> Register(UserDto request)
        {
            try
            {
                if (!_authService.ValidateUserDto(request, out string validationMessage))
                {
                    return BadRequest(new { message = validationMessage });
                }

                if (await _authService.UserExistsByEmail(request.Email) && await _authService.UserExistsByUsername(request.Username))
                {
                    return BadRequest(new { message = ErrorMessages.EmailAndUsernameExists });
                }

                if (await _authService.UserExistsByEmail(request.Email))
                {
                    return BadRequest(new { message = ErrorMessages.EmailExists });
                }

                if (await _authService.UserExistsByUsername(request.Username))
                {
                    return BadRequest(new { message = ErrorMessages.UsernameExists });
                }

                var user = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
                };

                await _authService.AddUserAsync(user);

                return Created("", new { message = "User registered successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in Register method.");
                return StatusCode(500, new { message = $"Internal server error in Register method: {ex.Message}" });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLoginDto request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest(new { message = ErrorMessages.RequiredFieldsPassword });
                }

                if (string.IsNullOrEmpty(request.Identifier))
                {
                    return BadRequest(new { message = ErrorMessages.RequiredFieldsIdentifier });
                }

                var user = await _authService.GetUserByIdentifierAsync(request.Identifier);

                if (user == null)
                {
                    return Unauthorized(new { message = ErrorMessages.InvalidCredentials });
                }

                if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    return Unauthorized(new { message = ErrorMessages.InvalidCredentials });
                }

                var token = _authService.GenerateJwtToken(user);

                return Ok(new { token });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in Login method.");
                return StatusCode(500, new { message = $"Internal server error in Login method: {ex.Message}" });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in Logout method.");
                return StatusCode(500, new { message = $"Internal server error in Logout method: {ex.Message}" });
            }
        }

        [HttpPut("update/{username}")]
        [Authorize]
        public async Task<IActionResult> UpdateUser([FromQuery] string username, [FromBody] UserDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    return BadRequest(new { message = "The username query parameter is required." });
                }

                var user = await _authService.GetUserByUsernameAsync(username);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                if (!_authService.ValidateUserDto(request, out string validationMessage))
                {
                    _logger.LogWarning("Validation failed for update request: {ValidationMessage}", validationMessage);
                    return BadRequest(new { message = validationMessage });
                }

                bool isUpdated = false;
                if (!string.IsNullOrEmpty(request.Username) && request.Username != user.Username)
                {
                    user.Username = request.Username;
                    isUpdated = true;
                }
                if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
                {
                    user.Email = request.Email;
                    isUpdated = true;
                }
                if (!string.IsNullOrEmpty(request.Password))
                {
                    string newPasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                    if (newPasswordHash != user.PasswordHash)
                    {
                        user.PasswordHash = newPasswordHash;
                        isUpdated = true;
                    }
                }

                if (!isUpdated)
                {
                    return Ok(new { message = "No updates were made as the details are the same" });
                }

                await _authService.UpdateUserAsync(user);

                _logger.LogInformation("User updated successfully for username: {Username}", username);
                return Ok(new { message = "User updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in UpdateUser method.");
                return StatusCode(500, new { message = $"Internal server error in UpdateUser method: {ex.Message}" });
            }
        }

        [HttpGet("GetAllUsers")]
        [Authorize]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _authService.GetAllUsersAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetAllUsers method.");
                return StatusCode(500, new { message = $"Internal server error in GetAllUsers method: {ex.Message}" });
            }
        }

        [HttpDelete("DeletUser/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var user = await _authService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                await _authService.DeleteUserAsync(id);

                return Ok(new { message = "User deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in DeleteUser method.");
                return StatusCode(500, new { message = $"Internal server error in DeleteUser method: {ex.Message}" });
            }
        }

    }
}
