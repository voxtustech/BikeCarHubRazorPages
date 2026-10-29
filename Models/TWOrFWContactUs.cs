using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace BikeCarHub.Models
{
    [Table("TWOrFWContactUs")]
    public class TWOrFWContactUs
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column(TypeName = "varchar(60)")]
        public string Name { get; set; }

        [DisplayName("Phone No.")]
        [Column(TypeName = "varchar(20)")]
        public string? PhoneNo { get; set; }

        [Required]
        [Column(TypeName = "varchar(150)")]
        public string Email { get; set; }

        [Required]
        public string Message { get; set; }

        [Required]
        [Column(TypeName = "varchar(30)")]
        public string FormType { get; set; }

        public DateTime CreatedDateTime { get; set; }
    }
}