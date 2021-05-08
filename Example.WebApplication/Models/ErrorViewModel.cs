namespace Example.WebApplication.Models
{
    using System.Diagnostics.CodeAnalysis;

    public class ErrorViewModel
    {
        [AllowNull]
        public string RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
