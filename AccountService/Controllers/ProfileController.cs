using AccountService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProfileController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("GetProfiles")]
        public async Task<IActionResult> GetAllProfiles()
        {
            var result = await _context.Profile.Select(x => new ProfileData
            {
                Id = x.Id,
                Name = x.Name,
                Username = x.Username,
                Description = x.Description
            }).ToListAsync();

            return Ok(result);
        }
    }
}
