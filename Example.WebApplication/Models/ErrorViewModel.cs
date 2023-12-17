namespace Example.WebApplication.Models;

public sealed class ErrorViewModel
{
    public string RequestId { get; set; } = default!;

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
