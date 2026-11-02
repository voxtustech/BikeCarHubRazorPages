using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace BikeCarHub.Models
{
    [Table("FWVarientColours")]
    public class FWVarientColour
    {
        [Key]
        public int FWVarientColourId { get; set; }
        public int FWVarientId { get; set; }
        [ForeignKey("FWVarientId")]
        [ValidateNever]
        public virtual FWVarient? FWVarient { get; set; }
        public int FWColourId { get; set; }
        [ForeignKey("FWColourId")]
        [ValidateNever]
        public virtual FWColour? FWColour { get; set; }
        public int SortOrder { get; set; }
        public bool IsAvailable { get; set; }
    }
}
