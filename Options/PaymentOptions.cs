namespace Supermarket.API.Options;

public class PaymentOptions
{
    public const string SectionName = "Payment";

    // A Pending (unpaid) order is cancelled and its stock released after this many minutes
    public int UnpaidOrderExpiryMinutes { get; set; } = 30;
}
