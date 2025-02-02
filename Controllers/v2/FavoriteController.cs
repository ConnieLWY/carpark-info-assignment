using Microsoft.AspNetCore.Mvc;
using HandshakesByDC_BEAssignment.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace HandshakesByDC_BEAssignment.Controllers.v2
{
    [Authorize]
    [ApiController]
    [Route("api/v2/[controller]")]
    public class FavoriteController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<FavoriteController> _logger;

        public FavoriteController(AppDbContext context, ILogger<FavoriteController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                throw new UnauthorizedAccessException("Invalid user ID in token");
            }
            return userId;
        }

        // Set favorite carpark (adds new or updates existing)
        [HttpPost("set/{carparkNo}")]
        public async Task<IActionResult> SetFavorite(string carparkNo)
        {
            try
            {
                int userId = GetCurrentUserId();

                // Check if user exists
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound("User not found");
                }

                // Check if carpark exists
                var carpark = await _context.Carparks.FindAsync(carparkNo);
                if (carpark == null)
                {
                    return NotFound("Carpark not found");
                }

                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // Remove existing favorite if any
                        var existingFavorite = await _context.UserFavorites
                            .FirstOrDefaultAsync(f => f.UserId == userId);

                        if (existingFavorite != null)
                        {
                            if (existingFavorite.CarparkNo == carparkNo)
                            {
                                return BadRequest("This carpark is already set as favorite");
                            }
                            _context.UserFavorites.Remove(existingFavorite);
                        }

                        // Add new favorite
                        var favorite = new UserFavorite
                        {
                            UserId = userId,
                            CarparkNo = carparkNo,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.UserFavorites.Add(favorite);
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        return Ok("Favorite carpark set successfully");
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error setting favorite carpark: {ex.Message}");
                return StatusCode(500, "Internal server error while setting favorite");
            }
        }

        // Remove favorite carpark
        [HttpDelete("remove")]
        public async Task<IActionResult> RemoveFavorite()
        {
            try
            {
                int userId = GetCurrentUserId();

                var favorite = await _context.UserFavorites
                    .FirstOrDefaultAsync(f => f.UserId == userId);

                if (favorite == null)
                {
                    return NotFound("No favorite carpark found");
                }

                _context.UserFavorites.Remove(favorite);
                await _context.SaveChangesAsync();

                return Ok("Favorite carpark removed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error removing favorite carpark: {ex.Message}");
                return StatusCode(500, "Internal server error while removing favorite");
            }
        }

        // Get user's favorite carpark
        [HttpGet]
        public async Task<ActionResult<Carpark>> GetFavorite()
        {
            try
            {
                int userId = GetCurrentUserId();

                var favorite = await _context.UserFavorites
                    .Where(f => f.UserId == userId)
                    .Include(f => f.Carpark)
                    .Select(f => f.Carpark)
                    .FirstOrDefaultAsync();

                if (favorite == null)
                {
                    return NotFound("No favorite carpark found");
                }

                return Ok(favorite);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving favorite: {ex.Message}");
                return StatusCode(500, "Internal server error while retrieving favorite");
            }
        }
    }
}