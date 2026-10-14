using Supermarket.API.Models;

namespace Supermarket.API.Services.Payments;

// One implementation per way of paying. A real provider (ABA PayWay, Bakong KHQR, a card
// gateway, ...) is added by implementing this interface and registering it in Program.cs.
public interface IPaymentProvider
{
    PaymentMethod Method { get; }

    // false: nothing to wait for (cash on delivery). true: the customer must pay online first.
    bool RequiresOnlinePayment { get; }

    Task<PaymentInitResult> InitiateAsync(Order order, Payment payment, CancellationToken cancellationToken = default);
}

public class PaymentInitResult
{
    // The provider's reference for this payment
    public string? TransactionId { get; set; }

    // Where the app sends the customer to pay (card checkout page)
    public string? PaymentUrl { get; set; }

    // Data the app turns into a QR code (KHQR)
    public string? QrData { get; set; }
}
