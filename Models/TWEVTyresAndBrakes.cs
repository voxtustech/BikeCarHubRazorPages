using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace BikeCarHub.Models
{
    public class TWEVTyresAndBrakes
    {
        [Key]
        public int TWEVTyresAndBrakesId { get; set; }
        public string? TWName { get; set; }
        public string? Varients { get; set; }
        public decimal? FrontBrakeDiameter { get; set; }
        public decimal? RearBrakeDiameter { get; set; }
        public decimal? FrontTyrePressureRider { get; set; }
        public decimal? FrontTyrePressureRiderAndPillion { get; set; }
        public decimal? RearTyrePressureRider { get; set; }
        public decimal? RearTyrePressureRiderAndPillion { get; set; }
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
