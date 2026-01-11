using Aiursoft.UiStack.Layout;

namespace Aiursoft.AiurDrive.Models.BackgroundJobs;

public class JobsIndexViewModel : UiStackLayoutViewModel
{
    public IEnumerable<JobInfo> AllRecentJobs { get; init; } = [];
}
