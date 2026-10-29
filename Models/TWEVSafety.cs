using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace BikeCarHub.Models
{
    public class TWEVSafety
    {
        [Key]
        public int TWEVSafetyId { get; set; }
        public string? TWName { get; set; }
        public string? Varients { get; set; }
        [Column(TypeName = "varchar(50)")]
        public string? BrakingType { get; set; }
        [Column(TypeName = "varchar(10)")]
        public string? ChargingPoint { get; set; }
        public string? FastCharging { get; set; }
        [Column(TypeName = "varchar(10)")]
        public string? MobileApplication { get; set; }
        [Column(TypeName = "varchar(10)")]
        public string? InternetConnectivity { get; set; }
        [Column(TypeName = "varchar(30)")]
        public string? OperatingSystem { get; set; }
        [Column(TypeName = "varchar(30)")]
        public string? Processor { get; set; }
        public decimal? Gradeability { get; set; }
        [Column(TypeName = "varchar(10)")]
        public string? ServiceDueIndicator { get; set; }
        [Column(TypeName = "varchar(10)")]
        public string? RidingModes { get; set; }
        public string? Display { get; set; }
        [Column(TypeName = "varchar(10)")]
        public string? SwitchableABS { get; set; }
        [Column(TypeName = "varchar(10)")]
        public string? EBS { get; set; }
        [Column(TypeName = "varchar(10)")]
        public string? SeatOpeningSwitch { get; set; }
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
