using Supermarket.API.Models;

namespace Supermarket.API.Services.Payments;

// Implemented by a provider that can be asked "has this customer paid yet?" (Bakong KHQR).
// Providers that call us back instead (a webhook) do not need it.
public interface IPaymentVerifier
{
    Task<PaymentVerification> VerifyAsync(Payment payment, CancellationToken cancellationToken = default);
}

public class PaymentVerification
{
    public bool Paid { get; set; }

    // The provider's proof of payment (for Bakong: the transaction hash)
    public string? ProviderReference { get; set; }
}
