using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BikeCarHub.Models
{
    [Table("Reviews")]
    public class Review
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ReviewID { get; set; }

        [Required]
        public int TwoWheelerID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        [StringLength(100)]
        public string Username { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        //    [Required]
        //    [StringLength(1000)]
        //    //public string ReviewText { get; set; }
        //    [StringLength(500, MinimumLength = 100,
        //ErrorMessage = "Review must be between 100 and 500 characters.")]
        //    public string ReviewText { get; set; }
        [Required]
        [StringLength(1000, MinimumLength = 100,
        ErrorMessage = "Review must be 100 or more than 100 characters.")]
        public string ReviewText { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation Property
        [ForeignKey("TwoWheelerID")]
        public virtual TwoWheeler TwoWheeler { get; set; }
    }
}