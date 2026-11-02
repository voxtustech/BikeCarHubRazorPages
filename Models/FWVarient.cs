using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace BikeCarHub.Models
{
    [Table("FWVarients")]
    public class FWVarient
    {
        [Key]
        public int FWVarientId { get; set; }
        public int FourWheelerId { get; set; }
        [ForeignKey("FourWheelerId")]
        [ValidateNever]
        public virtual FourWheeler? FourWheeler { get; set; }
        [Required]
        [MaxLength(200)]
        public string Varients { get; set; } = null!;
        [MaxLength(100)]
        public string? VariantName { get; set; }
        [MaxLength(200)]
        public string? VariantSlug { get; set; }
        [MaxLength(30)]
        public string? FuelType { get; set; }
        [MaxLength(30)]
        public string? Transmission { get; set; }
        [MaxLength(20)]
        public string? GearBox { get; set; }
        public int? ExShowroomPrice { get; set; }
        public int? SeatingCapacity { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedDateTime { get; set; }
    }
}
