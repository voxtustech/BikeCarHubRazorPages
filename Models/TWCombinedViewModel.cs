using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BikeCarHub.Models
{
    public class TWCombinedViewModel
    {
        //public TWSpec TWSpec { get; set; }
        //public TWSafety TWSafety { get; set; }

        //public SelectList TwoWheelers { get; set; }
        //public SelectList Varients { get; set; }
        //public TWSpec Spec { get; set; }
        //public TWSafety Safety { get; set; }

        //public SelectList TwoWheelers { get; set; }
        //public SelectList TWVarients { get; set; }
        public TWSpec Spec { get; set; } = new TWSpec();
        public TWSafety Safety { get; set; } = new TWSafety();
        [ValidateNever]
        public List<SelectListItem> TwoWheelers { get; set; }
        [ValidateNever]
        public List<SelectListItem> TWVarients { get; set; }
    }
}
