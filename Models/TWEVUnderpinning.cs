using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace BikeCarHub.Models
{
    public class TWEVUnderpinning
    {
        [Key]
        public int TWEVUnderpinningId { get; set; }
        public string? TWName { get; set; }
        public string? Varients { get; set; }
        public string? SuspensionFront { get; set; }
        public string? SuspensionRear { get; set; }
        [Column(TypeName = "varchar(20)")]
        public string? BrakesFront { get; set; }
        [Column(TypeName = "varchar(20)")]
        public string? BrakesRear { get; set; }
        [Column(TypeName = "varchar(50)")]
        public string? ABS { get; set; }
        public string? TyreSize { get; set; }
        public string? WheelSize { get; set; }
        [Column(TypeName = "varchar(50)")]
        public string? WheelType { get; set; }
        [Column(TypeName = "varchar(100)")]
        public string? Frame { get; set; }
        [Column(TypeName = "varchar(20)")]
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
