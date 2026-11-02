using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace BikeCarHub.Models
{
    [Table("FWEVBatteries")]
    public class FWEVBattery
    {
        [Key]
        public int FWVarientId { get; set; }
        [ForeignKey("FWVarientId")]
        [ValidateNever]
        public virtual FWVarient? FWVarient { get; set; }
        [Column(TypeName = "decimal(10,2)")]
        public decimal? BatteryCapacity { get; set; }
        [MaxLength(200)]
        public string? BatteryType { get; set; }
        public int? ClaimedRange { get; set; }
        [MaxLength(50)]
        public string? RangeTestCycle { get; set; }
        [MaxLength(200)]
        public string? MotorType { get; set; }
        [MaxLength(200)]
        public string? ACChargingTime { get; set; }
        [MaxLength(200)]
        public string? DCChargingTime { get; set; }
        [Column(TypeName = "decimal(10,2)")]
        public decimal? DCChargingPower { get; set; }
        [MaxLength(200)]
        public string? BatteryWarranty { get; set; }
        [MaxLength(50)]
        public string? RegenerativeBraking { get; set; }
    }
}
