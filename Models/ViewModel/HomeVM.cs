using Fatiha__app.Extension;

namespace Fatiha__app.Models.ViewModel
{
    public class HomeVM
    {

        public FatihaRequest Request { get; set; }

        public int Numberofstudent { get; set; }

        public int Numberofmjazin { get; set; }

        public int Numberofcertificates { get; set; }

        public List<languageS> displayedlanguages { get; set; }
    }
}
