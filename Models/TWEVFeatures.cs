using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace BikeCarHub.Models
{
    public class TWEVFeatures
    {
        [Key]
        public int TWEVFeaturesId { get; set; }
        public string? EVName { get; set; }
        public string? Varients { get; set; }
        [Column(TypeName = "varchar(30)")]
        public string? InstrumentConsole { get; set; }
        [Column(TypeName = "varchar(60)")]
        public string? BluetoothConnectivity { get; set; }
        [Column(TypeName = "varchar(30)")]
        public string? Navigation { get; set; }
        [Column(TypeName = "varchar(30)")]
        public string? CallSMSAlerts { get; set; }
        [Column(TypeName = "varchar(30)")]
        public string? RoadsideAssistance { get; set; }
        [Column(TypeName = "varchar(30)")]
        public string? AntiTheftAlarm { get; set; }
        [Column(TypeName = "varchar(30)")]
        public string? USBChargingPort { get; set; }
        [Column(TypeName = "varchar(30)")]
        public string? MusicControl { get; set; }
        [Column(TypeName = "varchar(30)")]
        public string? OTA { get; set; }
        [Column(TypeName = "varchar(20)")]
        public string? Speedometer { get; set; }
        [Column(TypeName = "varchar(20)")]
        public string? Tripmeter { get; set; }
        [Column(TypeName = "varchar(20)")]
        public string? Odometer { get; set; }
        public string? AdditionalFeaturesOfVariant { get; set; }
        [Column(TypeName = "varchar(20)")]
        public string? SeatType { get; set; }
        [Column(TypeName = "varchar(20)")]
        public string? Clock { get; set; }
        [Column(TypeName = "varchar(10)")]
        public string? PassengerFootrest { get; set; }
        [Column(TypeName = "varchar(10)")]
        public string? CarryHook { get; set; }
        public decimal? UnderseatStorage { get; set; }
        [Column(TypeName = "varchar(30)")]
        public string? ChargerOutput { get; set; }
        [Column(TypeName = "varchar(10)")]
        public string? RegenerativeBraking { get; set; }
        [Column(TypeName = "varchar(10)")]
        public string? HillHold { get; set; }
        [Column(TypeName = "varchar(10)")]
        public string? KeylessIgnition { get; set; }
        public int? TwoWheelerId { get; set; }
        [ForeignKey("TwoWheelerId")]
        [ValidateNever]
        public virtual TwoWheeler TwoWheeler { get; set; }
        public int? TWVarientId { get; set; }
        [ForeignKey("TWVarientId")]
        [ValidateNever]
        public virtual TWVarient TWVarients { get; set; }
        public DateTime? CreatedDateTime { get; set; }
    }
}
