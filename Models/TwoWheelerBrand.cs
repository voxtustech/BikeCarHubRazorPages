using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BikeCarHub.Models
{
    [Table("TWBrands")]
    public class TwoWheelerBrand
    {
        [Key]
        public int TWBrandId { get; set; }
        public String? BrandName { get; set; }
        //public TwoWheeler twoWheelers { get; set; }
        public ICollection<TwoWheeler> TwoWheelers { get; set; }
        public bool? IsPetrol { get; set; }
        public bool? IsEV { get; set; }
        public String? BrandLogoURL { get; set; }
        public String? Discription { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public bool? IsTWBrand { get; set; }
        public bool? IsFWBrand { get; set; }
        public string? TitleTag { get; set; }
        public string? MetaDescription { get; set; }
        public string? MetaKeywords { get; set; }
    }
}
