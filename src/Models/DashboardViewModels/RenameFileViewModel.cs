using System;
using System.ComponentModel.DataAnnotations;
using Aiursoft.CSTools.Attributes;

namespace AiurDrive.Models.DashboardViewModels
{
    public class RenameFileViewModel : LayoutViewModel
    {
        [Obsolete(message: "This method is only for framework", error: true)]
        public RenameFileViewModel() { }
        public RenameFileViewModel(AiurDriveUser user) : base(user, "Delete file")
        {

        }

        public void Recover(AiurDriveUser user)
        {
            RootRecover(user, "Delete file");
        }

        [Required]
        public string Path { get; set; }

        [Display(Name = "New file name")]
        [Required]
        [ValidFolderName]
        public string NewFileName { get; set; }
    }
}
