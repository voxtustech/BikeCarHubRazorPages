using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace BikeCarHub.Models
{
    [Table("BrandProfiles")]
    public class BrandProfile
    {
        [Key]
        public int BrandProfileId { get; set; }
        public int TWBrandId { get; set; }
        [ForeignKey("TWBrandId")]
        [ValidateNever]
        public virtual TwoWheelerBrand? Brand { get; set; }
        [MaxLength(2)]
        public string VehicleType { get; set; } = null!;
        public bool? IsPetrol { get; set; }
        public bool? IsEV { get; set; }
        public string? Discription { get; set; }
        public string? MetaDescription { get; set; }
        public string? MetaKeywords { get; set; }
        public string? TitleTag { get; set; }
        public DateTime? CreatedDateTime { get; set; }
    }
}
