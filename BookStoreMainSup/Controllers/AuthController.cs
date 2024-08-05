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
                var validationResult = await ValidateRegisterAsync(request);
                if (validationResult != null)
                {
                    return validationResult;
                }

                var user = CreateUser(request);
                await _authService.AddUserAsync(user);

                return Created("", new { message = "User registered successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in Register method.");
                return StatusCode(500, new { message = $"Internal server error in Register method: {ex.Message}" });
            }
        }

        private async Task<IActionResult> ValidateRegisterAsync(UserDto request)
        {
            if (!_authService.ValidateUserDto(request, out string validationMessage))
            {
                return BadRequest(new { message = validationMessage });
            }

            if (await _authService.UserExistsByEmail(request.Email))
            {
                return BadRequest(new { message = ErrorMessages.EmailExists });
            }

            if (await _authService.UserExistsByUsername(request.Username))
            {
                return BadRequest(new { message = ErrorMessages.UsernameExists });
            }

            return null;
        }

        private User CreateUser(UserDto request)
        {
            return new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
            };
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLoginDto request)
        {
            try
            {
                var validationResult = await ValidateLoginAsync(request);
                if (validationResult != null)
                {
                    return validationResult;
                }

                var user = await _authService.GetUserByIdentifierAsync(request.Identifier);
                user.IsLoggedIn = true;
                await _authService.UpdateUserAsync(user);

                var token = _authService.GenerateJwtToken(user);

                return Ok(new { token });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in Login method.");
                return StatusCode(500, new { message = $"Internal server error in Login method: {ex.Message}" });
            }
        }

        private async Task<IActionResult> ValidateLoginAsync(UserLoginDto request)
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

            if (!user.IsActive)
            {
                return Unauthorized(new { message = "Your account is temporarily deactivated. Please contact the admin." });
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = ErrorMessages.InvalidCredentials });
            }

            return null;
        }



        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var validationResult = await ValidateLogoutAsync();
                if (validationResult != null)
                {
                    return validationResult;
                }

                return Ok(new { message = "User logged out successfully" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Error occurred in Logout method.");
                return StatusCode(500, new { message = $"Internal server error in Logout method: {ex.Message}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred in Logout method.");
                return StatusCode(500, new { message = $"Internal server error in Logout method: {ex.Message}" });
            }
        }

        private async Task<IActionResult> ValidateLogoutAsync()
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();
                _logger.LogInformation($"Attempting to log out user with token: {token}");

                var user = await _authService.GetUserByTokenAsync(token);
                if (user != null)
                {
                    _logger.LogInformation($"User found: {user.Username}. Updating IsLoggedIn status to false.");

                    user.IsLoggedIn = false;
                    await _authService.UpdateUserAsync(user);

                    _tokenRevocationService.RevokeToken(token);
                }
                else
                {
                    _logger.LogWarning("Invalid token or user not found");
                    return BadRequest(new { message = "Invalid token or user not found" });
                }
            }
            else
            {
                _logger.LogWarning("User not logged in");
                return BadRequest(new { message = "User not logged in" });
            }

            return null;
        }


        [HttpPut("update-details")]
        [Authorize]
        public async Task<IActionResult> UpdateUserDetails([FromBody] UpdateUserDto request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var user = await _authService.GetUserByIdAsync(userId);

                var validationResult = await ValidateUpdateUserDetailsAsync(request, user);
                if (validationResult != null)
                {
                    return validationResult;
                }

                await _authService.UpdateUserAsync(user);

                return Ok(new { message = "User details updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in UpdateUserDetails method.");
                return StatusCode(500, new { message = $"Internal server error in UpdateUserDetails method: {ex.Message}" });
            }
        }

        private async Task<IActionResult> ValidateUpdateUserDetailsAsync(UpdateUserDto request, User user)
        {
            if (request.Username == null)
            {
                return BadRequest(new { message = "Updating Username cannot be null" });
            }
            if (request.Email == null)
            {
                return BadRequest(new { message = "Updating Email cannot be null" });
            }
            if (request.Username != null)
            {
                if (string.IsNullOrWhiteSpace(request.Username))
                {
                    return BadRequest(new { message = "Username cannot be empty" });
                }

                if (!Regex.IsMatch(request.Username, @"^[a-zA-Z0-9]{3,20}$"))
                {
                    return BadRequest(new { message = "Username must be between 3 and 20 characters and contain only letters and numbers." });
                }

                if (await _authService.UserExistsByUsername(request.Username))
                {
                    return BadRequest(new { message = ErrorMessages.UsernameExists });
                }

                user.Username = request.Username;
            }

            if (request.Email != null)
            {
                if (string.IsNullOrWhiteSpace(request.Email) || !_authService.IsValidEmail(request.Email))
                {
                    return BadRequest(new { message = ErrorMessages.EmailEmpty });
                }

                if (!_authService.IsValidEmail(request.Email))
                {
                    return BadRequest(new { message = ErrorMessages.InvalidEmailFormat });
                }

                if (await _authService.UserExistsByEmail(request.Email))
                {
                    return BadRequest(new { message = ErrorMessages.EmailExists });
                }

                user.Email = request.Email;
            }

            return null;
        }



        [HttpPut("update-password")]
        [Authorize]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordDto request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var user = await _authService.GetUserByIdAsync(userId);

                if (string.IsNullOrWhiteSpace(request.OldPassword))
                {
                    return BadRequest(new { message = ErrorMessages.OldPasswordEmpty });
                }

                if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
                {
                    return BadRequest(new { message = "Old password is incorrect" });
                }

                if (string.IsNullOrWhiteSpace(request.NewPassword))
                {
                    return BadRequest(new { message = ErrorMessages.NewPasswordEmpty });
                }

                if (!_authService.ValidatePassword(request.NewPassword, out string validationMessage))
                {
                    return BadRequest(new { message = validationMessage });
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                user.IsLoggedIn = false; // Set IsLoggedIn to false

                await _authService.UpdateUserAsync(user);

                // Revoke the user's token
                var authHeader = Request.Headers["Authorization"].ToString();
                if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    var token = authHeader.Substring("Bearer ".Length).Trim();
                    _tokenRevocationService.RevokeToken(token);
                }

                return Ok(new { message = "Password changed, please login again" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in UpdatePassword method.");
                return StatusCode(500, new { message = $"Internal server error in UpdatePassword method: {ex.Message}" });
            }
        }



    }
}
