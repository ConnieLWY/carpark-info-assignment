using System.ComponentModel.DataAnnotations.Schema;

namespace HandshakesByDC_BEAssignment.Models
{
    public class UserFavorite
    {
        [ForeignKey("User")]
        public int UserId { get; set; }  // FK to users table

        [ForeignKey("Carpark")]
        public string CarparkNo { get; set; } = string.Empty;  // FK to carparks table

        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public virtual User User { get; set; }
        public virtual Carpark? Carpark { get; set; }
    }
}
