using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace BikeCarHub.Models
{
    [Table("LatestNews")]
    public class TWLatestNews
    {
        [Key]
        public int TWLatestNewsId { get; set; }
        [MaxLength(500)]
        public string? NewsHeading { get; set; }
        [MaxLength(2000)]
        public string? NewsSummary { get; set; }
        public string? NewsDetail { get; set; }
        public string? ImageURL { get; set; }
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = true)]

        public DateTime? Date { get; set; }
        public int? TwoWBrandId { get; set; }
        [ForeignKey("TwoWBrandId")]
        [ValidateNever]
        public virtual TwoWheelerBrand TwoWheelerBrands { get; set; }
        [Column(TypeName = "varchar(30)")]
        public string? BrandName { get; set; }
        public string? ImageFolderURL { get; set; }
        public bool? IsTwoWheeler { get; set; }
    }
}
