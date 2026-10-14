namespace Supermarket.API.Models;

public enum PaymentMethod
{
    CashOnDelivery,
    Khqr,
    Card
}

public enum PaymentStatus
{
    Pending,
    Paid,
    Failed,
    Cancelled,
    Refunded
}

public class Payment
{
    public int PaymentId { get; set; }

    public int OrderId { get; set; }

    public PaymentMethod Method { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public decimal Amount { get; set; }

    // The payment provider's reference. Null for cash on delivery.
    public string? TransactionId { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
