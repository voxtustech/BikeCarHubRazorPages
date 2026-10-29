using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace BikeCarHub.Models
{
    public class TWUnderpinning
    {
        [Key]
        public int TWUnderpinningId { get; set; }
        public string? TWName { get; set; }
        public string? Varients { get; set; }
        public string? SuspensionFront { get; set; }
        public string? SuspensionRear { get; set; }
        [Column(TypeName = "varchar(20)")]
        public string? BrakesFront { get; set; }
        [Column(TypeName = "varchar(20)")]
        public string? BrakesRear { get; set; }
        public string? TyreSize { get; set; }
        public string? WheelSize { get; set; }
        public string? WheelType { get; set; }
        [Column(TypeName = "varchar(10)")]
        public string? TubelessTyre { get; set; }
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
