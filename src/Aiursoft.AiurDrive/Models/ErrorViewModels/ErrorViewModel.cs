using Aiursoft.UiStack.Layout;

namespace Aiursoft.AiurDrive.Models.ErrorViewModels;

public class ErrorViewModel: UiStackLayoutViewModel
{
    public ErrorViewModel(string pageTitle = "Error")
    {
        PageTitle = pageTitle;
    }

    public int ErrorCode { get; set; } = 500;

    public required string RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public string? ReturnUrl { get; set; }
}
