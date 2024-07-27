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

                user.IsLoggedIn = true;
                await _authService.UpdateUserAsync(user);

                var token = _authService.GenerateJwtToken(user);

                // Log the claims in the token for debugging
                _authService.LogTokenClaims(token);

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
        public async Task<IActionResult> Logout()
        {
            try
            {
                var authHeader = Request.Headers["Authorization"].ToString();
                if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    var token = authHeader.Substring("Bearer ".Length).Trim();
                    _logger.LogInformation($"Attempting to log out user with token: {token}");

                    // Log the token claims for debugging
                    _authService.LogTokenClaims(token);

                    // Get the user by token
                    var user = await _authService.GetUserByTokenAsync(token);
                    if (user != null)
                    {
                        _logger.LogInformation($"User found: {user.Username}. Updating IsLoggedIn status to false.");

                        // Update the user's IsLoggedIn status
                        user.IsLoggedIn = false;
                        await _authService.UpdateUserAsync(user);

                        // Revoke the token
                        _tokenRevocationService.RevokeToken(token);

                        return Ok(new { message = "User logged out successfully" });
                    }

                    _logger.LogWarning("Invalid token or user not found");
                    return BadRequest(new { message = "Invalid token or user not found" });
                }

                _logger.LogWarning("User not logged in");
                return BadRequest(new { message = "User not logged in" });
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





    }
}
