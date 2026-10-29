using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BikeCarHub.Models
{
    public class Wishlist
    {
        [Key]
        public int WishlistID { get; set; }

        public int TwoWheelerID { get; set; }

        public string? Username { get; set; }

        //public string? UserID { get; set; }
        public int? UserID { get; set; }

        public DateTime CreatedAt { get; set; }

        [ForeignKey("TwoWheelerID")]
        public virtual TwoWheeler? TwoWheelers { get; set; }
    }
}