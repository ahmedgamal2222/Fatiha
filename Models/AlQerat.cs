using System.ComponentModel.DataAnnotations;

namespace Fatiha__app.Models
{
    public class AlQerat
    {
        public int Id { get; set; }

       
        public string QeratName { get; set; }

        public string Description { get; set; }
        public string AudioFile { get; set; }
    }
}
