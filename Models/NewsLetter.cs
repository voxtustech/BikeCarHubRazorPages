using System.ComponentModel.DataAnnotations;

namespace BikeCarHub.Models
{
    public class NewsLetter
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Email { get; set; }
        public DateTime CreatedDateTime { get; set; }
    }
}
