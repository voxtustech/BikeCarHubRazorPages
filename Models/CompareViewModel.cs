namespace BikeCarHub.Models
{
    public class CompareViewModel
    {
        public TWVarient Variant1 { get; set; }
        public TWVarient Variant2 { get; set; }
        public List<TWVarient> FirstBikeVariants { get; set; }
        public List<TWVarient> SecondBikeVariants { get; set; }
    }
}
