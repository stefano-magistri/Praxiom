namespace DottoressaIorio.Shared.Branding;

public sealed class ProductBrandingOptions
{
    public string ProductName { get; init; } = "Praxiom";
    // TODO: Move studio/customer branding to install-time or per-studio configuration.
    public string StudioDisplayName { get; init; } = "Studio: Alessandra Iorio";
    public string SupportEmailAddress { get; init; } = "stefanomagistri@gmail.com";

    public string SupportEmailSubject => $"{ProductName} Support Request";
}
