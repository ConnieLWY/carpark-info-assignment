using Microsoft.AspNetCore.Mvc;
using HandshakesByDC_BEAssignment.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace HandshakesByDC_BEAssignment.Controllers.v1
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UserController> _logger;

        public UserController(AppDbContext context, ILogger<UserController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // DTO for user registration
        public class CreateUserRequest
        {
            public string Username { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        // Hash password
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            try
            {
                // Validate request
                if (string.IsNullOrWhiteSpace(request.Username) ||
                    string.IsNullOrWhiteSpace(request.Email) ||
                    string.IsNullOrWhiteSpace(request.Password))
                {
                    return BadRequest("Username, email, and password are required");
                }

                // Check if username already exists
                if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                {
                    return BadRequest("Username already exists");
                }

                // Check if email already exists
                if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                {
                    return BadRequest("Email already exists");
                }

                // Create new user
                var user = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    PasswordHash = HashPassword(request.Password),
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Return user info without password hash
                return Ok(new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating user: {ex.Message}");
                return StatusCode(500, "Internal server error while creating user");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                // Find user
                var user = await _context.Users
                    .Include(u => u.Favorite)  // Include favorites to delete them too
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    return NotFound("User not found");
                }

                // Start transaction to ensure all related data is deleted
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // Remove all favorites
                        if (user.Favorite != null)
                        {
                            _context.UserFavorites.Remove(user.Favorite);
                        }

                        // Remove user
                        _context.Users.Remove(user);
                        await _context.SaveChangesAsync();

                        await transaction.CommitAsync();
                        return Ok($"User {user.Username} and all related data deleted successfully");
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
                _logger.LogError($"Error deleting user: {ex.Message}");
                return StatusCode(500, "Internal server error while deleting user");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            try
            {
                var user = await _context.Users
                    .Select(u => new
                    {
                        u.Id,
                        u.Username,
                        u.Email,
                        u.CreatedAt
                    })
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    return NotFound("User not found");
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving user: {ex.Message}");
                return StatusCode(500, "Internal server error while retrieving user");
            }
        }

        [HttpGet("list")]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            try
            {
                var users = await _context.Users
                    .Select(u => new
                    {
                        u.Id,
                        u.Username,
                        u.Email,
                        u.PasswordHash,
                        u.CreatedAt,
                        Favorite = u.Favorite != null ? new
                        {
                            UserId = u.Favorite.UserId,
                            CarparkNo = u.Favorite.CarparkNo,
                            CreatedAt = u.Favorite.CreatedAt,
                            User = u.Username,
                            Carpark = new
                            {
                                u.Favorite.Carpark.CarparkNo,
                                u.Favorite.Carpark.Address,
                                u.Favorite.Carpark.XCoord,
                                u.Favorite.Carpark.YCoord,
                                u.Favorite.Carpark.CarParkType,
                                u.Favorite.Carpark.TypeOfParkingSystem,
                                u.Favorite.Carpark.FreeParking,
                                u.Favorite.Carpark.NightParking,
                                u.Favorite.Carpark.ShortTermParking,
                                u.Favorite.Carpark.CarParkBasement,
                                u.Favorite.Carpark.GantryHeight,
                                u.Favorite.Carpark.CarParkDecks,
                                u.Favorite.Carpark.LastUpdated,
                                FavoritedBy = _context.UserFavorites
                                    .Where(f => f.CarparkNo == u.Favorite.CarparkNo)
                                    .Select(f => f.User.Username)
                                    .ToList()
                            }
                        } : null
                    })
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving users: {ex.Message}");
                return StatusCode(500, "Internal server error while retrieving users");
            }
        }
    }
}