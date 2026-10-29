using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace BikeCarHub.Models
{
    public class TWEngineAndTransmission
    {
        [Key]
        public int TWEngineAndTransmissionId { get; set; }
        public string? TWName { get; set; }
        public string? Varients { get; set; }
        public string? EngineType { get; set; }
        public decimal? Displacement { get; set; }
        public string? MaxTorque { get; set; }
        public int? NumOfCylinders { get; set; }
        [Column(TypeName = "varchar(30)")]
        public string? CoolingSystem { get; set; }
        public int? ValvePerCylinder { get; set; }
        [Column(TypeName = "varchar(30)")]
        public string? Starting { get; set; }
        [Column(TypeName = "varchar(40)")]
        public string? FuelSupply { get; set; }
        public string? Clutch { get; set; }
        [Column(TypeName = "varchar(10)")]
        public string? GearBox { get; set; }
        public string? EmissionType { get; set; }
        public string? CompressionRatio { get; set; }
        public string? Ignition { get; set; }
        public int? TwoWheelerId { get; set; }
        [ForeignKey("TwoWheelerId")]
        [ValidateNever]
        public virtual TwoWheeler TwoWheeler { get; set; }
        public int? TWVarientId { get; set; }
        [ForeignKey("TWVarientId")]
        [ValidateNever]
        public virtual TWVarient TWVarients { get; set; }
        public DateTime? CreatedDateTime { get; set; }
    }
}
