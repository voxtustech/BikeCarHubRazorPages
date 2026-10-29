using System.ComponentModel.DataAnnotations;

namespace BikeCarHub.Models
{
    public class ValueForMoney
    {
        [Key]
        public int VFMId { get; set; }
        [MaxLength(500)]
        public string? VFMHeading { get; set; }
        public string? URL { get; set; }
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = true)]

        public DateTime? Date { get; set; }
        public bool? IsTwoWheeler { get; set; }
        public string? ImageURL { get; set; }
    }
}
