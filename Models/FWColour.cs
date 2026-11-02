using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace BikeCarHub.Models
{
    [Table("FWColours")]
    public class FWColour
    {
        [Key]
        public int FWColourId { get; set; }
        public int FourWheelerId { get; set; }
        [ForeignKey("FourWheelerId")]
        [ValidateNever]
        public virtual FourWheeler? FourWheeler { get; set; }
        [Required]
        [MaxLength(200)]
        public string ColourName { get; set; } = null!;
        [Required]
        [MaxLength(200)]
        public string ColourSlug { get; set; } = null!;
        [MaxLength(20)]
        public string? TopColorCode { get; set; }
        [MaxLength(20)]
        public string? BottomColorCode { get; set; }
        public bool IsDualTone { get; set; }
        public int SortOrder { get; set; }
        [MaxLength(400)]
        public string? ColourFolder { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedDateTime { get; set; }
    }
}
