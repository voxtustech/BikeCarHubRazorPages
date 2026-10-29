using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace BikeCarHub.Models
{
    public class TWVarient
    {
        [Key]
        public int TWVarientId { get; set; }
        public string? Varients { get; set; }
        public string? TWName { get; set; }
        public int? TwoWheelerId { get; set; }
        [ForeignKey("TwoWheelerId")]
        [ValidateNever]
        public virtual TwoWheeler TwoWheeler { get; set; }
        public DateTime? CreatedDateTime { get; set; }
        public string? SEO_Title { get; set; }

        public string? SEO_Description { get; set; }

        public string? SEO_Keywords { get; set; }
    }
}
