using System.ComponentModel.DataAnnotations;

namespace BikeCarHub.Models
{
    public class LoginModel
    {
        [Key]
        public int LoginId { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
    }
}
