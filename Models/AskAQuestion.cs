using System.ComponentModel.DataAnnotations;

namespace BikeCarHub.Models
{
    public class AskAQuestion
    {
        [Key]
        public int QId { get; set; }
        [MaxLength(500)]
        public string? Name { get; set; }
        [MaxLength(500)]
        public string? Email { get; set; }
        [MaxLength(2000)]
        public string? Question { get; set; }
    }
}
