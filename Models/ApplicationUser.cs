using Microsoft.AspNetCore.Identity;

namespace BikeCarHub.Models
{
    //public class ApplicationUser : IdentityUser
    public class ApplicationUser : IdentityUser<int>
    {
        public string? FullName { get; set; }
    }
}