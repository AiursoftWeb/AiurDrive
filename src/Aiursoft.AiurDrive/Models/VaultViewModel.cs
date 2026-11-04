using Aiursoft.UiStack.Layout;

namespace Aiursoft.AiurDrive.Models;

public class VaultViewModel : UiStackLayoutViewModel
{
    public int StorageLimitInGiB { get; set; }

    public VaultViewModel()
    {
        PageTitle = "Zero-Trust Vault";
    }
}
