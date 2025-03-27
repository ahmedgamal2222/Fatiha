using Fatiha__app.Models;

namespace Fatiha__app.ViewModels
{
    public class TestViewModel
    {

      
        

        public List<languageS> Languages { get; set; }
        public List<FatihaExam> Questions { get; set; }
        public int SelectedLanguageId { get; set; }

    }
}
