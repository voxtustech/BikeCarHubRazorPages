using System.ComponentModel.DataAnnotations;

namespace BikeCarHub.Models
{
    public class UpcomingCar
    {
        [Key]
        public int UpcomingCarId { get; set; }
        public string? UpcomingCarName { get; set; }
        public string? UpcomingCarDetails { get; set; }
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = true)]

        public DateTime? ExpectedLaunchDate { get; set; }
        public string ImageURL { get; set; }
    }
}
