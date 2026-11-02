using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace BikeCarHub.Models
{
    // Read-only: a database view that replaces the FWSpec sheet by joining the other car tables.
    [Table("vw_FWKeySpecs")]
    public class FWKeySpecs
    {
        [Key]
        public int FWVarientId { get; set; }
        public int FourWheelerId { get; set; }
        public string? FuelType { get; set; }
        public decimal? Mileage { get; set; }
        public string? MileageUnit { get; set; }
        public int? Displacement { get; set; }
        public string? MaxPower { get; set; }
        public string? MaxTorque { get; set; }
        public string? Transmission { get; set; }
        public int? SeatingCapacity { get; set; }
        public int? BootSpace { get; set; }
        public int? GroundClearance { get; set; }
        public decimal? FuelCapacity { get; set; }
        public string? BodyType { get; set; }
        public int? Airbags { get; set; }
        public int? NCAPRating { get; set; }
        public decimal? BatteryCapacity { get; set; }
        public int? ClaimedRange { get; set; }
    }
}
