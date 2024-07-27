using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BookStoreMainSup.Models;
using BookStoreMainSup.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BookStoreMainSup.Controllers
{
    [Route("api/admin")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly AdminService _adminService;
        private readonly AuthService _authService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(AdminService adminService, AuthService authService, ILogger<AdminController> logger)
        {
            _adminService = adminService;
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterAdmin(UserDto request)
        {
            try
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

                var admin = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    IsAdmin = true
                };

                await _adminService.AddAdminAsync(admin);

                return Created("", new { message = "Admin registered successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in RegisterAdmin method.");
                return StatusCode(500, new { message = $"Internal server error in RegisterAdmin method: {ex.Message}" });
            }
        }

        [HttpGet("loggedinusers")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetLoggedInUsers()
        {
            try
            {
                var loggedInUsers = await _adminService.GetLoggedInUsersAsync();
                return Ok(loggedInUsers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetLoggedInUsers method.");
                return StatusCode(500, new { message = $"Internal server error in GetLoggedInUsers method: {ex.Message}" });
            }
        }
    }
}
