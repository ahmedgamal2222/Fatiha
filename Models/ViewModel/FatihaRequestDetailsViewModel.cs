namespace Fatiha__app.Models.ViewModel
{
    public class FatihaRequestDetailsViewModel
    {

        public FatihaRequest FatihaRequest { get; set; }
        public FatihaRequestComment FatihaRequestComment { get; set; }
        public List<FatihaRequestComment> CommentsList { get; set; }  // list of comments
        public bool IsAdminsComment { get; set; }
    }
}
