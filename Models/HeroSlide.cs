using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BikeCarHub.Models
{
    [Table("HeroSlides")]
    public class HeroSlide
    {
        [Key]
        public int HeroSlideId { get; set; }

        [Required]
        [Column(TypeName = "varchar(200)")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "varchar(500)")]
        public string Subtitle { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "varchar(500)")]
        public string ImageURL { get; set; } = string.Empty;

        [Column(TypeName = "varchar(100)")]
        public string? Badge { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string? Featured { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string? Price { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDateTime { get; set; }
    }
}