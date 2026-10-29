using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BikeCarHub.Models
{
    [Table("TWUpcomingBikes")]
    public class UpcomingBike
    {
        [Key]
        public int UpcomingBikeId { get; set; }
        public string? UpcomingBikeName { get; set; }
        public string? UpcomingBikeDetails { get; set; }
        //[DataType(DataType.Date)]
        //[DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = true)]

        public string? ExpectedLaunchDate { get; set; }
        //public string? ExpectedPrice { get; set; } -----------we dont have this field in database for now----------
        public string? ImageURL { get; set; }
        public int? TwoWBrandId { get; set; }
        [ForeignKey("TwoWBrandId")]
        public virtual TwoWheelerBrand TwoWheelerBrands { get; set; }
        public string? BrandName { get; set; }
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = true)]
        public DateTime? FilterLaunchDate { get; set; }
        public DateTime? CreatedDateTime { get; set; }
        public string? ImageFolderURL { get; set; }
    }
}