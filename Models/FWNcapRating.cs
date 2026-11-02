using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace BikeCarHub.Models
{
    [Table("FWNcapRatings")]
    public class FWNcapRating
    {
        [Key]
        public int FWNcapRatingId { get; set; }
        public int FourWheelerId { get; set; }
        [ForeignKey("FourWheelerId")]
        [ValidateNever]
        public virtual FourWheeler? FourWheeler { get; set; }
        [MaxLength(50)]
        public string? NCAPAgency { get; set; }
        public int? NCAPYear { get; set; }
        public int? NCAPAdultRating { get; set; }
        public int? NCAPChildRating { get; set; }
        [Column(TypeName = "decimal(10,2)")]
        public decimal? NCAPAdultPoints { get; set; }
        [Column(TypeName = "decimal(10,2)")]
        public decimal? NCAPChildPoints { get; set; }
    }
}
