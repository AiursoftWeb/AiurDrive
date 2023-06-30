using Microsoft.AspNetCore.Mvc;
using System;
using Aiursoft.CSTools.Attributes;

namespace AiurDrive.Models.DashboardViewModels
{
    public class NewFolderViewModel : LayoutViewModel
    {
        [Obsolete(message: "This method is only for framework", error: true)]
        public NewFolderViewModel() { }
        public NewFolderViewModel(AiurDriveUser user) : base(user, "Create new folder") { }
        public void Recover(AiurDriveUser user)
        {
            RootRecover(user, "Create new folder");
        }

        [ValidFolderName]
        public string NewFolderName { get; set; }

        [FromRoute]
        public string Path { get; set; }
    }
}
