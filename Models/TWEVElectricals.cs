using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace BikeCarHub.Models
{
    public class TWEVElectricals
    {
        [Key]
        public int TWEVElectricalsId { get; set; }
        public string? TWName { get; set; }
        public string? Varients { get; set; }
        [Column(TypeName = "varchar(30)")]
        public string? Headlight { get; set; }
        [Column(TypeName = "varchar(30)")]
        public string? TailLight { get; set; }
        [Column(TypeName = "varchar(30)")]
        public string? TurnSignalLamp { get; set; }
        [Column(TypeName = "varchar(30)")]
        public string? LEDTailLights { get; set; }
        [Column(TypeName = "varchar(30)")]
        public string? LowBatteryIndicator { get; set; }
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
