using System.ComponentModel.DataAnnotations;

namespace Aiursoft.AiurDrive.Models.DashboardViewModels
{
    public class DeleteFileViewModel : LayoutViewModel
    {
        [Obsolete(message: "This method is only for framework", error: true)]
        public DeleteFileViewModel() { }
        public DeleteFileViewModel(AiurDriveUser user) : base(user, "Delete file")
        {

        }

        public void Recover(AiurDriveUser user)
        {
            RootRecover(user, "Delete file");
        }

        [Required]
        public string Path { get; set; }
    }
}
