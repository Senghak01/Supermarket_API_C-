using Supermarket.API.Models;

namespace Supermarket.API.Services.Payments;

// DEVELOPMENT ONLY. Pretends to be an online provider so the mobile app can build and test the
// whole pay flow before a real provider is connected. It is only registered when the app runs
// in the Development environment. Confirm a payment with POST /api/payments/{id}/sandbox-confirm.
public class SandboxPaymentProvider : IPaymentProvider
{
    public SandboxPaymentProvider(PaymentMethod method) => Method = method;

    public PaymentMethod Method { get; }

    public bool RequiresOnlinePayment => true;

    public Task<PaymentInitResult> InitiateAsync(Order order, Payment payment, CancellationToken cancellationToken = default)
    {
        var result = new PaymentInitResult
        {
            TransactionId = $"SBX-{payment.PaymentId}-{Guid.NewGuid().ToString("N")[..8]}"
        };

        if (Method == PaymentMethod.Khqr)
            result.QrData = $"SANDBOX-KHQR|order={order.OrderId}|amount={payment.Amount:0.00}";
        else
            result.PaymentUrl = $"https://sandbox.invalid/pay/{payment.PaymentId}";

        return Task.FromResult(result);
    }
}
