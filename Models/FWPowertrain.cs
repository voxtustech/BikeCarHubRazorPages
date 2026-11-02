using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace BikeCarHub.Models
{
    [Table("FWPowertrains")]
    public class FWPowertrain
    {
        [Key]
        public int FWVarientId { get; set; }
        [ForeignKey("FWVarientId")]
        [ValidateNever]
        public virtual FWVarient? FWVarient { get; set; }
        [MaxLength(200)]
        public string? EngineType { get; set; }
        public int? Displacement { get; set; }
        [MaxLength(200)]
        public string? MaxPower { get; set; }
        [MaxLength(200)]
        public string? MaxTorque { get; set; }
        [Column(TypeName = "decimal(10,2)")]
        public decimal? PowerBhp { get; set; }
        [Column(TypeName = "decimal(10,2)")]
        public decimal? TorqueNm { get; set; }
        public int? NumOfCylinders { get; set; }
        public int? ValvePerCylinder { get; set; }
        public bool? Turbocharger { get; set; }
        [MaxLength(50)]
        public string? FuelSupply { get; set; }
        [MaxLength(10)]
        public string? DriveType { get; set; }
        [MaxLength(50)]
        public string? EmissionType { get; set; }
        [Column(TypeName = "decimal(10,2)")]
        public decimal? ARAIMileage { get; set; }
        [MaxLength(10)]
        public string? MileageUnit { get; set; }
        [Column(TypeName = "decimal(10,2)")]
        public decimal? FuelCapacity { get; set; }
        [Column(TypeName = "decimal(10,2)")]
        public decimal? CNGTankCapacity { get; set; }
        public int? TopSpeed { get; set; }
        [Column(TypeName = "decimal(10,2)")]
        public decimal? ZeroTo100Kmph { get; set; }
    }
}
