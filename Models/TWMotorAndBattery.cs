using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace BikeCarHub.Models
{
    public class TWMotorAndBattery
    {
        [Key]
        public int TWMotorAndBatteryId { get; set; }
        public string? TWName { get; set; }
        public string? Varients { get; set; }
        public string? PeakPower { get; set; }
        [Column(TypeName = "varchar(20)")]
        public string? DriveType { get; set; }
        [Column(TypeName = "varchar(20)")]
        public string? Transmission { get; set; }
        public string? BatteryCapacity { get; set; }
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
