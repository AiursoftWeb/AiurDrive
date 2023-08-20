using Aiursoft.Probe.SDK.Models;

namespace Aiursoft.AiurDrive.Models.DashboardViewModels
{
    public class ViewFilesViewModel : LayoutViewModel
    {
        [Obsolete(message: "This method is only for framework", error: true)]
        public ViewFilesViewModel() { }
        public ViewFilesViewModel(AiurDriveUser user) : base(user, "View files") { }

        public Folder Folder { get; set; }
        public string Path { get; set; }
        public string SiteName { get; set; }
    }
}
