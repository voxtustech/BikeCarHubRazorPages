using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace BikeCarHub.Models
{
    [Table("FWImages")]
    public class FWImage
    {
        [Key]
        public int FWImageId { get; set; }
        public int FourWheelerId { get; set; }
        [ForeignKey("FourWheelerId")]
        [ValidateNever]
        public virtual FourWheeler? FourWheeler { get; set; }
        public int? FWColourId { get; set; }
        [ForeignKey("FWColourId")]
        [ValidateNever]
        public virtual FWColour? FWColour { get; set; }
        public int? FWVarientId { get; set; }
        [ForeignKey("FWVarientId")]
        [ValidateNever]
        public virtual FWVarient? FWVarient { get; set; }
        [Required]
        [MaxLength(20)]
        public string ImageType { get; set; } = null!;
        [MaxLength(20)]
        public string? ViewAngle { get; set; }
        public int SortOrder { get; set; }
        public bool IsPrimary { get; set; }
        [MaxLength(260)]
        public string? FileName { get; set; }
        [Required]
        [MaxLength(400)]
        public string WebPPath { get; set; } = null!;
        [MaxLength(400)]
        public string? ImageAltTag { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        [MaxLength(50)]
        public string? Source { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedDateTime { get; set; }
    }
}
