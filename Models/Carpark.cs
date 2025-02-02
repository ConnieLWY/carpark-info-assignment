using System.ComponentModel.DataAnnotations;

namespace HandshakesByDC_BEAssignment.Models
{
    public class Carpark
    {
        public Carpark()
        {
            FavoritedBy = new List<UserFavorite>();
        }

        [Key]
        public string CarparkNo { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public float XCoord { get; set; }
        public float YCoord { get; set; }
        public string CarParkType { get; set; } = string.Empty;
        public string TypeOfParkingSystem { get; set; } = string.Empty;
        public string FreeParking { get; set; } = string.Empty;
        public bool NightParking { get; set; }
        public string ShortTermParking { get; set; } = string.Empty;
        public string CarParkBasement { get; set; } = string.Empty;
        public float GantryHeight { get; set; }
        public int CarParkDecks { get; set; }
        public DateTime LastUpdated { get; set; }
        public ICollection<UserFavorite> FavoritedBy { get; set; }
    }

    public class CarparkImportLog
    {
        public int Id { get; set; }
        public DateTime ImportDate { get; set; }
        public int TotalRecords { get; set; }
        public int SuccessfulRecords { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
