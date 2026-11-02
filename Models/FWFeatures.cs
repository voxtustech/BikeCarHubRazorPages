using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace BikeCarHub.Models
{
    [Table("FWFeatures")]
    public class FWFeatures
    {
        [Key]
        public int FWVarientId { get; set; }
        [ForeignKey("FWVarientId")]
        [ValidateNever]
        public virtual FWVarient? FWVarient { get; set; }

        // Comfort and convenience
        [MaxLength(50)]
        public string? AirConditioner { get; set; }
        public bool? RearACVents { get; set; }
        [MaxLength(50)]
        public string? PowerWindows { get; set; }
        public bool? CruiseControl { get; set; }
        public bool? KeylessEntry { get; set; }
        public bool? PushButtonStart { get; set; }
        [MaxLength(50)]
        public string? Sunroof { get; set; }
        [MaxLength(50)]
        public string? VentilatedSeats { get; set; }
        public bool? PoweredDriverSeat { get; set; }
        public bool? WirelessCharger { get; set; }
        public bool? AutoHeadlamps { get; set; }
        public bool? RainSensingWipers { get; set; }
        public bool? ElectricFoldingORVM { get; set; }
        public bool? AutoDimmingIRVM { get; set; }
        public bool? RearWiperWasher { get; set; }
        [MaxLength(50)]
        public string? SeatUpholstery { get; set; }
        public string? AdditionalFeaturesOfVariant { get; set; }

        // Infotainment
        [MaxLength(50)]
        public string? TouchscreenSize { get; set; }
        [MaxLength(50)]
        public string? AndroidAutoAppleCarPlay { get; set; }
        public int? Speakers { get; set; }
        public bool? ConnectedCarTech { get; set; }
        [MaxLength(50)]
        public string? DigitalInstrumentCluster { get; set; }
        public bool? HeadUpDisplay { get; set; }
        public bool? AmbientLighting { get; set; }

        // Exterior and lighting
        [MaxLength(50)]
        public string? Headlamps { get; set; }
        public bool? LEDDRLs { get; set; }
        [MaxLength(50)]
        public string? FogLamps { get; set; }
        [MaxLength(50)]
        public string? TailLamps { get; set; }
        public bool? ConnectedTailLamp { get; set; }
        public bool? RoofRails { get; set; }
        public bool? DualToneRoof { get; set; }
    }
}
