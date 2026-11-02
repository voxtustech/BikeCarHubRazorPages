using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace BikeCarHub.Models
{
    [Table("FWBodyAndChassis")]
    public class FWBodyAndChassis
    {
        [Key]
        public int FWVarientId { get; set; }
        [ForeignKey("FWVarientId")]
        [ValidateNever]
        public virtual FWVarient? FWVarient { get; set; }
        public int? Length { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public int? Wheelbase { get; set; }
        public int? GroundClearance { get; set; }
        public int? KerbWeight { get; set; }
        public int? BootSpace { get; set; }
        public int? NumOfDoors { get; set; }
        public int? NumOfSeatingRows { get; set; }
        [Column(TypeName = "decimal(10,2)")]
        public decimal? TurningRadius { get; set; }
        [MaxLength(200)]
        public string? FrontSuspension { get; set; }
        [MaxLength(200)]
        public string? RearSuspension { get; set; }
        [MaxLength(200)]
        public string? SteeringType { get; set; }
        [MaxLength(200)]
        public string? SteeringAdjustment { get; set; }
        [MaxLength(200)]
        public string? FrontBrake { get; set; }
        [MaxLength(200)]
        public string? RearBrake { get; set; }
        [MaxLength(50)]
        public string? WheelType { get; set; }
        [Column(TypeName = "decimal(10,2)")]
        public decimal? WheelSize { get; set; }
        [MaxLength(200)]
        public string? TyreSize { get; set; }
        [MaxLength(50)]
        public string? SpareWheel { get; set; }
    }
}
