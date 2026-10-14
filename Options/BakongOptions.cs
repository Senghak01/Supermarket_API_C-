namespace Supermarket.API.Options;

public class BakongOptions
{
    public const string SectionName = "Bakong";

    // false: Khqr is not offered (sandbox only in Development). true: real Bakong KHQR.
    public bool Enabled { get; set; }

    // Production: https://api-bakong.nbc.gov.kh   Sandbox: https://sit-api-bakong.nbc.gov.kh
    // Use the one your token was issued for.
    public string BaseUrl { get; set; } = "https://api-bakong.nbc.gov.kh";

    // SECRET: keep it in User Secrets / environment variable (Bakong__Token), never in appsettings.json
    public string Token { get; set; } = string.Empty;

    // The Bakong account that receives the money, e.g. shopname@abaa
    public string AccountId { get; set; } = string.Empty;

    public string MerchantName { get; set; } = string.Empty;
    public string MerchantCity { get; set; } = "PHNOM PENH";

    // Must be the same currency your product prices are in: USD or KHR
    public string Currency { get; set; } = "USD";

    // Individual | Merchant (Merchant also needs MerchantId and AcquiringBank)
    public string AccountType { get; set; } = "Individual";
    public string? MerchantId { get; set; }
    public string? AcquiringBank { get; set; }

    public string? MobileNumber { get; set; }
    public string? StoreLabel { get; set; }

    // How often the background job asks Bakong about unpaid QR payments
    public int PollIntervalSeconds { get; set; } = 5;
}
