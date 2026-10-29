using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace BikeCarHub.Models
{
    public class TWDimensionsAndCapacity
    {
        [Key]
        public int TWDimensionsAndCapacityId { get; set; }
        public string? TWName { get; set; }
        public string? Varients { get; set; }
        public decimal? Width { get; set; }
        public decimal? Length { get; set; }
        public decimal? Height { get; set; }
        public decimal? FuelCapacity { get; set; }
        public decimal? GroundClearance { get; set; }
        public decimal? Wheelbase { get; set; }
        public decimal? KerbWeight { get; set; }
        public decimal? FuelReserve { get; set; }
        public decimal? SaddleHeight { get; set; }
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

