using System.ComponentModel.DataAnnotations;

namespace HandshakesByDC_BEAssignment.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public UserFavorite Favorite { get; set; }
    }
}
