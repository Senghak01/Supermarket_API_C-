using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Supermarket.API.Models;

namespace Supermarket.API.DTOs.Payments;

public class InitiatePaymentRequest
{
    // CashOnDelivery | Khqr | Card
    [Required]
    public string Method { get; set; } = string.Empty;
}

public class PaymentResponse
{
    public int PaymentId { get; set; }
    public int OrderId { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PaymentMethod Method { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PaymentStatus Status { get; set; }

    public decimal Amount { get; set; }
    public string? TransactionId { get; set; }

    // Only filled in the response to "start payment". The app shows one of these to the customer.
    public string? PaymentUrl { get; set; }
    public string? QrData { get; set; }

    // Online payments only: when the unpaid order is cancelled automatically
    public DateTime? ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }

    public static PaymentResponse From(Payment p, string? paymentUrl = null, string? qrData = null, DateTime? expiresAt = null) => new()
    {
        PaymentId = p.PaymentId,
        OrderId = p.OrderId,
        Method = p.Method,
        Status = p.Status,
        Amount = p.Amount,
        TransactionId = p.TransactionId,
        PaymentUrl = paymentUrl,
        QrData = qrData,
        ExpiresAt = expiresAt,
        CreatedAt = p.CreatedAt,
        PaidAt = p.PaidAt
    };
}
