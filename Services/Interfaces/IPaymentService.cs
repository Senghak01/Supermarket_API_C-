using Supermarket.API.DTOs.Payments;

namespace Supermarket.API.Services.Interfaces;

public interface IPaymentService
{
    Task<PaymentResponse> InitiateAsync(int customerId, int orderId, InitiatePaymentRequest request);
    Task<List<PaymentResponse>> GetForOrderAsync(int customerId, int orderId);

    // Records a successful payment and confirms the order. Safe to call twice (providers retry).
    // The real provider's webhook calls this once the signature has been verified.
    Task<PaymentResponse> MarkPaidAsync(int paymentId, string? transactionId);

    // Asks the provider whether the customer has paid (methods that can be checked, like Bakong KHQR).
    // The app calls this every few seconds while it shows the QR code.
    Task<PaymentResponse> RefreshAsync(int customerId, int paymentId);

    // For the background job: checks recent unpaid online payments. Returns how many turned out to be paid.
    Task<int> VerifyPendingPaymentsAsync(DateTime createdAfter, CancellationToken cancellationToken = default);

    // Development only: pretend the provider confirmed the payment
    Task<PaymentResponse> SandboxConfirmAsync(int customerId, int paymentId);
}
