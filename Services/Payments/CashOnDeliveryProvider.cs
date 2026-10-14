using Supermarket.API.Models;

namespace Supermarket.API.Services.Payments;

public class CashOnDeliveryProvider : IPaymentProvider
{
    public PaymentMethod Method => PaymentMethod.CashOnDelivery;

    public bool RequiresOnlinePayment => false;

    public Task<PaymentInitResult> InitiateAsync(Order order, Payment payment, CancellationToken cancellationToken = default)
        => Task.FromResult(new PaymentInitResult());
}
