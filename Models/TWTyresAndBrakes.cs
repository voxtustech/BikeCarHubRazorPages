using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BikeCarHub.Models
{
    public class TWTyresAndBrakes
    {
        [Key]
        public int TWTyresAndBrakesId { get; set; }
        public string? TWName { get; set; }
        public string? Varients { get; set; }
        public decimal? FrontBrakeDiameter { get; set; }
        public decimal? RearBrakeDiameter { get; set; }
        public string? RadialTyre { get; set; }
        public string? FrontSuspension { get; set; }
        public string? RearSuspension { get; set; }
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
