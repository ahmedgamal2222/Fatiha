namespace Fatiha__app.Models.ViewModel
{
    // Pagination Prop
    public class AlQeratIndexVM
    {
        public IEnumerable<AlQerat> AlQerats { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}
