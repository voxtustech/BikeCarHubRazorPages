using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace BikeCarHub.Models
{
    [Table("Twowheelers")]
    public class TwoWheeler
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public int TwoWheelerId { get; set; }

        [Required]
        public string? TwoWheelerName { get; set; } = null!;
        public string? Price { get; set; }
        public int? BasePrice { get; set; }

        public int? TopPrice { get; set; }
        [Column(TypeName = "varchar(30)")]
        public string? Brand { get; set; } = null!;
        [Column(TypeName = "varchar(30)")]
        public string? Type { get; set; } = null!;
        public int? TwoWBrandId { get; set; }
        [ForeignKey("TwoWBrandId")]
        [ValidateNever]
        public virtual TwoWheelerBrand TwoWheelerBrands { get; set; }
        public string? TWImage { get; set; }
        //[DataType(DataType.Date)]
        //[DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = true)]

        public string? LaunchDate { get; set; }
        //[DataType(DataType.Date)]
        //[DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = true)]
        //public DateOnly Date { get; set; }
        public bool? IsEV {get; set;}
        public bool? IsActive { get; set;}
        [Column(TypeName = "varchar(100)")]
        public string? NotesOrComments { get; set;}
        public DateTime? CreatedDateTime { get; set; }
        public virtual ICollection<TWVarient> TWVarients { get; set; } = new List<TWVarient>();
        //public virtual ICollection<TWSpec> TWSpecs { get; set; } = new List<TWSpec>();
        //public static string GenerateSlug(string name)
        //{
        //    return name.ToLower().Replace(" ", "-").Replace("/", "-");
        //}
        public string? Discription { get; set; }
    }
}
