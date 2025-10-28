using AccountService.Business;
using AccountService.Data;
using AccountService.Dtos;
using AccountService.Models;
using AccountService.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private readonly AccountRepository _context;
        private readonly ProfileService service;
        private readonly BlobService _blobService;

        public ProfileController(AccountRepository context, BlobService blobService, ProfileService profileService)
        {
            _context = context;
            _blobService = blobService;
            service = profileService;
        }

        [HttpGet("GetProfiles")]
        public async Task<IActionResult> GetAllProfiles()
        {
            var result = await _context.Profile.Select(x => new Profile
            {
                Id = x.Id,
                Name = x.Name,
                Username = x.Username,
                Description = x.Description
            }).ToListAsync();

            return Ok(result);
        }

        [HttpPost("CreateProfile")]
        public async Task<ObjectResult> CreateProfile([FromBody] Profile profile)
        {
            if (profile == null)
            {
                return BadRequest("Profile data is missing.");
            }

            CreateProfileDto dto = new CreateProfileDto(profile.Username, profile.Name, profile.Description);

            int id = await service.CreateProfile(dto);
            return Ok(new { message = "Profile created successfully", id });
        }
        [HttpPost("SearchForProfile")]
        public ObjectResult SearchForProfile([FromBody] string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return BadRequest("Username input is missing.");
            }
            var results = service.SearchForAProfile(username);

            return Ok(new { message = "", results });
        }
        [HttpPut("UpdateProfileName")]
        public async Task<ObjectResult> UpdateProfileName([FromBody] UpdateProfileNameDto dto)
        {
            if (dto == null)
            {
                return BadRequest("Name data is missing.");
            }

            bool success = await service.UpdateProfileName(dto);
            return Ok(new { message = "Profile updated successfully" });
        }
        [HttpPut("UpdateProfileDescription")]
        public async Task<ObjectResult> UpdateProfileDescrition([FromBody] UpdateProfileDescDto dto)
        {
            if (dto == null)
            {
                return BadRequest("Profile data is missing.");
            }

            bool result = await service.UpdateProfileDescription(dto);
            return Ok(new { message = "Profile description upadted successfully" });
        }

        [HttpDelete("DeleteProfile/{id}")]
        public async Task<IActionResult> DeleteProfile(int id)
        {
            try
            {
                var deletedId = await service.DeleteProfile(id);
                return Ok(new { message = "Profile deleted successfully", id = deletedId });
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("UploadProfilePicture")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadProfilePicture([FromForm] UploadProfilePictureDto dto)
        {
            if (dto.File == null || dto.File.Length == 0)
                return BadRequest("No file uploaded.");

            var url = await _blobService.UploadProfilePictureAsync(dto);
            await service.UpdateProfilePicture(dto.Id, url);

            return Ok(new { message = "Uploaded successfully", url });
        }
    }
}
