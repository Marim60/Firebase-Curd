using System.ComponentModel.DataAnnotations;

namespace Task_1.Models
{
    public class FileUploadViewModel
    {
        [Required(ErrorMessage = "UserID is required")]
        public string UserID { get; set; }



        [Required(ErrorMessage = "Please select a file")]
        public IFormFile File { get; set; }
    }
}
