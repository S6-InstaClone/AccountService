using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MassTransit;
using AccountService.Data;
using AccountService.Messages;
using AccountService.Business;

namespace AccountService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly AccountRepository _context;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly KeycloakService _keycloakService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            AccountRepository context,
            IPublishEndpoint publishEndpoint,
            KeycloakService keycloakService,
            ILogger<AccountController> logger)
        {
            _context = context;
            _publishEndpoint = publishEndpoint;
            _keycloakService = keycloakService;
            _logger = logger;
        }

        /// <summary>
        /// Get user ID from X-User-Id header (set by API Gateway)
        /// </summary>
        private string? GetUserId()
        {
            return Request.Headers["X-User-Id"].FirstOrDefault();
        }

        /// <summary>
        /// Get user email from X-User-Email header (set by API Gateway)
        /// </summary>
        private string? GetUserEmail()
        {
            return Request.Headers["X-User-Email"].FirstOrDefault();
        }

        /// <summary>
        /// Get username from X-User-Name header (set by API Gateway)
        /// </summary>
        private string? GetUserName()
        {
            return Request.Headers["X-User-Name"].FirstOrDefault();
        }

        /// <summary>
        /// Get current user's account info
        /// </summary>
        [HttpGet("me")]
        public async Task<IActionResult> GetMyAccount()
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            // Try to find profile by KeycloakUserId
            var profile = await _context.Profile
                .FirstOrDefaultAsync(p => p.KeycloakUserId == userId);

            if (profile == null)
            {
                // Return basic info from headers if no profile exists
                return Ok(new
                {
                    userId = userId,
                    email = GetUserEmail(),
                    username = GetUserName(),
                    hasProfile = false
                });
            }

            return Ok(new
            {
                userId = userId,
                profileId = profile.Id,
                email = GetUserEmail(),
                username = profile.Username,
                name = profile.Name,
                description = profile.Description,
                profilePicture = profile.ProfilePictureLink,
                hasProfile = true
            });
        }

        /// <summary>
        /// Delete current user's account - GDPR compliant
        /// This will:
        /// 1. Delete user from Keycloak
        /// 2. Delete user profile from this service
        /// 3. Publish message to notify other services to delete user data
        /// </summary>
        [HttpDelete("delete-me")]
        public async Task<IActionResult> DeleteMyAccount()
        {
            var userId = GetUserId();
            var userEmail = GetUserEmail();
            var userName = GetUserName();

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            _logger.LogInformation("GDPR Delete Request: Starting account deletion for user {UserId}", userId);

            try
            {
                // Step 1: Delete from Keycloak
                var keycloakDeleted = await _keycloakService.DeleteUserAsync(userId);
                if (!keycloakDeleted)
                {
                    _logger.LogWarning("Failed to delete user {UserId} from Keycloak, continuing with local deletion", userId);
                    // Continue anyway - user data should still be deleted from our services
                }
                else
                {
                    _logger.LogInformation("GDPR Delete: Deleted user {UserId} from Keycloak", userId);
                }

                // Step 2: Delete profile from local database (if exists)
                var profile = await _context.Profile
                    .FirstOrDefaultAsync(p => p.KeycloakUserId == userId);

                if (profile != null)
                {
                    _context.Profile.Remove(profile);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("GDPR Delete: Removed profile record for user {UserId}", userId);
                }

                // Step 3: Publish message to RabbitMQ for other services
                var message = new AccountDeletedEvent
                {
                    UserId = userId,
                    Username = userName ?? profile?.Username,
                    Email = userEmail,
                    DeletedAt = DateTime.UtcNow,
                    Reason = "GDPR_USER_REQUEST"
                };

                await _publishEndpoint.Publish(message);
                _logger.LogInformation("GDPR Delete: Published AccountDeletedEvent for user {UserId}", userId);

                return Ok(new
                {
                    message = "Account successfully deleted",
                    userId = userId,
                    deletedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GDPR Delete: Failed to delete account for user {UserId}", userId);
                return StatusCode(500, new { message = "Failed to delete account. Please contact support." });
            }
        }
    }
}