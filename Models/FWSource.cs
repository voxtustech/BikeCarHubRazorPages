using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace BikeCarHub.Models
{
    [Table("FWSources")]
    public class FWSource
    {
        [Key]
        public int FWSourceId { get; set; }
        public int FourWheelerId { get; set; }
        [ForeignKey("FourWheelerId")]
        [ValidateNever]
        public virtual FourWheeler? FourWheeler { get; set; }
        public string? SourceURLs { get; set; }
        public DateTime? FetchedDate { get; set; }
        public string? Notes { get; set; }
    }
}
