using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace BikeCarHub.Models
{
    public class TWFeatures
    {
        [Key]
        public int TWFeaturesId { get; set; }
        public string? TWName { get; set; }
        public string? Varients { get; set; }
        [Column(TypeName = "varchar(50)")]
        public string? ABS { get; set; }
        [Column(TypeName = "varchar(20)")]
        public string? Speedometer { get; set; }
        [Column(TypeName = "varchar(20)")]
        public string? Tripmeter { get; set; }
        [Column(TypeName = "varchar(20)")]
        public string? Tachometer { get; set; }
        public string? LEDTailLight { get; set; }
        [Column(TypeName = "varchar(20)")]
        public string? Odometer { get; set; }
        [Column(TypeName = "varchar(10)")]
        public string? FuelGauge { get; set; }
        [Column(TypeName = "varchar(30)")]
        public string? InstrumentConsole { get; set; }
        [Column(TypeName = "varchar(20)")]
        public string? SeatType { get; set; }
        [Column(TypeName = "varchar(20)")]
        public string? BodyGraphics { get; set; }
        [Column(TypeName = "varchar(20)")]
        public string? Clock { get; set; }
        [Column(TypeName = "varchar(10)")]
        public string? PassengerFootrest { get; set; }
        public int? TwoWheelerId { get; set; }
        [ForeignKey("TwoWheelerId")]
        [ValidateNever]
        public virtual TwoWheeler TwoWheeler { get; set; }
        public string? AdditionalFeaturesOfVariant { get; set; }
        public string? DistanceToEmptyIndicator { get; set; }
        public string? AdjustableWindshield { get; set; }
        public int? TWVarientId { get; set; }
        [ForeignKey("TWVarientId")]
        [ValidateNever]
        public virtual TWVarient TWVarients { get; set; }
        public DateTime? CreatedDateTime { get; set; }
    }
}
