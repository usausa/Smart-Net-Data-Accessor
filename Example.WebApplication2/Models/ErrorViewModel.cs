namespace Example.WebApplication2.Models;

public sealed class ErrorViewModel
{
    public string RequestId { get; set; } = default!;

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
