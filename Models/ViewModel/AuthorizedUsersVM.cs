namespace Fatiha__app.Models.ViewModel
{
    public class AuthorizedUsersVM
    {
        public IEnumerable<AuthorizedUsers> AuthorizedUsers { get; set; }
        public int PageNumber { get; set; }
        public int TotalPages { get; set; }
    }
}
