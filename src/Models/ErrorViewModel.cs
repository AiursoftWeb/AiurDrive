namespace Aiursoft.AiurDrive.Models;

public class ErrorViewModel(string requestId)
{
    public string RequestId { get; set; } = requestId;

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
