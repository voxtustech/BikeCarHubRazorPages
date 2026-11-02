using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace BikeCarHub.Models
{
    [Table("FWSafety")]
    public class FWSafety
    {
        [Key]
        public int FWVarientId { get; set; }
        [ForeignKey("FWVarientId")]
        [ValidateNever]
        public virtual FWVarient? FWVarient { get; set; }
        public int? Airbags { get; set; }
        public bool? ABS { get; set; }
        public bool? EBD { get; set; }
        public bool? ESC { get; set; }
        public bool? HillHoldAssist { get; set; }
        public bool? HillDescentControl { get; set; }
        public bool? TPMS { get; set; }
        public bool? RearParkingSensors { get; set; }
        public bool? FrontParkingSensors { get; set; }
        [MaxLength(50)]
        public string? ParkingCamera { get; set; }
        public bool? ISOFIX { get; set; }
        [MaxLength(50)]
        public string? ADAS { get; set; }
        public bool? AllSeatThreePointSeatbelts { get; set; }
        public bool? RearDefogger { get; set; }
    }
}
