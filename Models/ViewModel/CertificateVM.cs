namespace Fatiha__app.Models.ViewModel
{
    public class CertificateVM
    {
        public FatihaRequest FatihaRequest { get; set; }
        public Certificate Certificate { get; set; }
        public int FatihaRequestId { get; set; }
        public string ApplicationUserId { get; set; }
    }
}
