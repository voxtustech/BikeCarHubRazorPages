using System.ComponentModel.DataAnnotations;

namespace BikeCarHub.Models
{
    public class FWLatestNews
    {
        [Key]
        public int FWLatestNewsId { get; set; }
        [MaxLength(100)]
        public string? NewsHeading { get; set; }
        [MaxLength(300)]
        public string? NewsSummary { get; set; }
        [MaxLength(800)]
        public string? NewsDetail { get; set; }
        public string ImageURL { get; set; }
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = true)]

        public DateTime Date { get; set; }
    }
}
