using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace BikeCarHub.Models
{
    public class TWEVCharging
    {
        [Key]
        public int TWEVChargingId { get; set; }
        public string? TWName { get; set; }
        public string? Varients { get; set; }
        [Column(TypeName = "varchar(30)")]
        public string? ChargingAtHome { get; set; }
        [Column(TypeName = "varchar(30)")]
        public string? ChargingAtChargingStation { get; set; }
        [Column(TypeName = "varchar(30)")]
        public string? ChargingTimeZeroTo80Percent { get; set; }
        [Column(TypeName = "varchar(30)")]
        public string? ChargingTimeZeroTo100Percent { get; set; }
        [Column(TypeName = "varchar(20)")]
        public string? ChargingNetworkBatterySwappingNetwork { get; set; }
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
