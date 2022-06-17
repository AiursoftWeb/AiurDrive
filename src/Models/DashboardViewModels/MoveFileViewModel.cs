using Aiursoft.Probe.SDK.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace AiurDrive.Models.DashboardViewModels
{
    public class MoveFileViewModel : LayoutViewModel
    {
        [Obsolete(message: "This method is only for framework", error: true)]
        public MoveFileViewModel() { }
        public MoveFileViewModel(AiurDriveUser user) : base(user, "Move file")
        {

        }
        public void Recover(AiurDriveUser user)
        {
            RootRecover(user, "Move file");
        }

        public Folder Folder { get; set; }

        [Required]
        public string Path { get; set; }

        public string NewFolderPath { get; set; }
    }
}
