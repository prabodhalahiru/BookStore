using BookStoreMainSup.Data;
using BookStoreMainSup.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

[Route("api/admin")]
[ApiController]
public class AdminController : ControllerBase
{
    private readonly AdminService _adminService;

    public AdminController(AdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet("loggedUsers")]
    public IActionResult GetLoggedUsers()
    {
        try
        {
            var users = _adminService.GetLoggedUsers();
            return Ok(users);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}
