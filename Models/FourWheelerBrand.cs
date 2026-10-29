using System.ComponentModel.DataAnnotations;

namespace BikeCarHub.Models
{
    public class FourWheelerBrand
    {
        [Key]
        public int Id { get; set; }
        public String? FourWheelerBrandName { get; set; }
    }
}
