using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace BikeCarHub.Models
{
    [Table("FourWheelers")]
    public class FourWheeler
    {
        [Key]
        public int FourWheelerId { get; set; }
        [Required]
        [MaxLength(200)]
        public string FourWheelerName { get; set; } = null!;
        [MaxLength(200)]
        public string ModelSlug { get; set; } = null!;
        public int FWBrandId { get; set; }
        [ForeignKey("FWBrandId")]
        [ValidateNever]
        public virtual TwoWheelerBrand? Brand { get; set; }
        public int? BasePrice { get; set; }
        public int? TopPrice { get; set; }
        [MaxLength(50)]
        public string? Type { get; set; }
        [MaxLength(100)]
        public string? Segment { get; set; }
        public DateTime? LaunchDate { get; set; }
        public bool IsEV { get; set; }
        public bool IsActive { get; set; }
        public string? Discription { get; set; }
        [MaxLength(400)]
        public string? ImageFolder { get; set; }
        public string? NotesOrComments { get; set; }
        public DateTime? CreatedDateTime { get; set; }
        public virtual ICollection<FWVarient> FWVarients { get; set; } = new List<FWVarient>();
        public virtual ICollection<FWColour> FWColours { get; set; } = new List<FWColour>();
        public virtual ICollection<FWImage> FWImages { get; set; } = new List<FWImage>();
        public virtual ICollection<FWNcapRating> FWNcapRatings { get; set; } = new List<FWNcapRating>();
    }
}
