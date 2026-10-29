using System.ComponentModel.DataAnnotations;

namespace BikeCarHub.Models
{
    public class CompareItems
    {
        [Key]
        public int Id { get; set; }

        public string Topic { get; set; }

        public string CompareUrl { get; set; }
    }

}
