namespace Fatiha__app.Models.ViewModel
{
    public class FatihaExamVM
    {
        public IEnumerable<FatihaExam> FatihaExams { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }

        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
    }
}
