using System;
using System.ComponentModel.DataAnnotations;

namespace Aiursoft.AiurDrive.Models.DashboardViewModels
{
    public class DeleteFolderViewModel : LayoutViewModel
    {
        [Obsolete(message: "This method is only for framework", error: true)]
        public DeleteFolderViewModel() { }
        public DeleteFolderViewModel(AiurDriveUser user) : base(user, "Delete folder")
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
