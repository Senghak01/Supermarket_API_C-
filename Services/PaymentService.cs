using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Supermarket.API.Data;
using Supermarket.API.DTOs.Payments;
using Supermarket.API.Exceptions;
using Supermarket.API.Models;
using Supermarket.API.Options;
using Supermarket.API.Services.Interfaces;
using Supermarket.API.Services.Payments;

namespace Supermarket.API.Services;

public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _db;
    private readonly IEnumerable<IPaymentProvider> _providers;
    private readonly PaymentOptions _options;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        ApplicationDbContext db,
        IEnumerable<IPaymentProvider> providers,
        IOptions<PaymentOptions> options,
        ILogger<PaymentService> logger)
    {
        _db = db;
        _providers = providers;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<PaymentResponse> InitiateAsync(int customerId, int orderId, InitiatePaymentRequest request)
    {
        if (!Enum.TryParse<PaymentMethod>(request.Method?.Trim(), ignoreCase: true, out var method)
            || !Enum.IsDefined(method))
            throw new AppException("Unknown payment method. Use CashOnDelivery, Khqr or Card.");

        var order = await _db.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderId == orderId && o.CustomerId == customerId)
            ?? throw AppException.NotFound("Order not found.");

        if (order.Status != OrderStatus.Pending)
            throw AppException.Conflict("This order can no longer be paid.");

        var provider = _providers.FirstOrDefault(p => p.Method == method)
            ?? throw new AppException("This payment method is not available yet.");

        // A new attempt replaces any earlier unpaid one
        await _db.Payments
            .Where(p => p.OrderId == orderId && p.Status == PaymentStatus.Pending)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, PaymentStatus.Cancelled));

        var payment = new Payment
        {
            OrderId = orderId,
            Method = method,
            Status = PaymentStatus.Pending,
            Amount = order.TotalAmount,
            CreatedAt = DateTime.UtcNow
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync(); // assigns PaymentId

        PaymentInitResult init;
        try
        {
            init = await provider.InitiateAsync(order, payment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Payment provider {Method} failed to start payment {PaymentId}", method, payment.PaymentId);
            payment.Status = PaymentStatus.Failed;
            await _db.SaveChangesAsync();
            throw new AppException("Could not start the payment. Please try again.", 502);
        }

        if (init.TransactionId is not null)
        {
            payment.TransactionId = init.TransactionId;
            await _db.SaveChangesAsync();
        }

        DateTime? expiresAt = null;

        if (provider.RequiresOnlinePayment)
        {
            expiresAt = order.CreatedAt.AddMinutes(_options.UnpaidOrderExpiryMinutes);
        }
        else
        {
            // Cash on delivery: nothing to wait for, so the shop can start preparing the order.
            // The payment stays Pending until the cash is collected on delivery.
            await _db.Orders
                .Where(o => o.OrderId == orderId && o.Status == OrderStatus.Pending)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(o => o.Status, OrderStatus.Confirmed)
                    .SetProperty(o => o.UpdatedAt, (DateTime?)DateTime.UtcNow));
        }

        return PaymentResponse.From(payment, init.PaymentUrl, init.QrData, expiresAt);
    }

    public async Task<List<PaymentResponse>> GetForOrderAsync(int customerId, int orderId)
    {
        var owned = await _db.Orders.AnyAsync(o => o.OrderId == orderId && o.CustomerId == customerId);
        if (!owned) throw AppException.NotFound("Order not found.");

        var payments = await _db.Payments
            .AsNoTracking()
            .Where(p => p.OrderId == orderId)
            .OrderByDescending(p => p.PaymentId)
            .ToListAsync();

        return payments.Select(p => PaymentResponse.From(p)).ToList();
    }

    public async Task<PaymentResponse> MarkPaidAsync(int paymentId, string? transactionId)
    {
        var orderId = await _db.Payments
            .AsNoTracking()
            .Where(p => p.PaymentId == paymentId)
            .Select(p => (int?)p.OrderId)
            .FirstOrDefaultAsync()
            ?? throw AppException.NotFound("Payment not found.");

        var now = DateTime.UtcNow;

        await using var tx = await _db.Database.BeginTransactionAsync();

        // Lock order is Orders first, then Payments: the same as cancellation, so they cannot deadlock
        var confirmed = await _db.Orders
            .Where(o => o.OrderId == orderId && o.Status == OrderStatus.Pending)
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.Status, OrderStatus.Confirmed)
                .SetProperty(o => o.UpdatedAt, (DateTime?)now));

        // "Not Paid yet" instead of "Pending" so a payment that arrives late is still recorded
        var paid = await _db.Payments
            .Where(p => p.PaymentId == paymentId && p.Status != PaymentStatus.Paid)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Status, PaymentStatus.Paid)
                .SetProperty(p => p.PaidAt, (DateTime?)now)
                .SetProperty(p => p.TransactionId, p => p.TransactionId ?? transactionId));

        await tx.CommitAsync();

        if (paid == 1 && confirmed == 0)
        {
            var status = await _db.Orders
                .AsNoTracking()
                .Where(o => o.OrderId == orderId)
                .Select(o => o.Status)
                .FirstAsync();

            if (status == OrderStatus.Cancelled)
                _logger.LogWarning(
                    "Payment {PaymentId} was received for cancelled order {OrderId}. The customer must be refunded.",
                    paymentId, orderId);
        }

        var payment = await _db.Payments.AsNoTracking().FirstAsync(p => p.PaymentId == paymentId);
        return PaymentResponse.From(payment);
    }

    public async Task<PaymentResponse> RefreshAsync(int customerId, int paymentId)
    {
        var payment = await _db.Payments
            .AsNoTracking()
            .Where(p => p.PaymentId == paymentId
                        && _db.Orders.Any(o => o.OrderId == p.OrderId && o.CustomerId == customerId))
            .FirstOrDefaultAsync()
            ?? throw AppException.NotFound("Payment not found.");

        if (payment.Status == PaymentStatus.Pending)
            await VerifyAndMarkPaidAsync(payment);

        var fresh = await _db.Payments.AsNoTracking().FirstAsync(p => p.PaymentId == paymentId);
        return PaymentResponse.From(fresh);
    }

    public async Task<int> VerifyPendingPaymentsAsync(DateTime createdAfter, CancellationToken cancellationToken = default)
    {
        var pending = await _db.Payments
            .AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Pending
                        && p.Method != PaymentMethod.CashOnDelivery
                        && p.CreatedAt > createdAfter)
            .OrderBy(p => p.PaymentId)
            .Take(50)
            .ToListAsync(cancellationToken);

        var paid = 0;

        foreach (var payment in pending)
        {
            if (await VerifyAndMarkPaidAsync(payment, cancellationToken))
                paid++;
        }

        return paid;
    }

    // Asks the provider (if it can be asked) and records the payment when it was paid
    private async Task<bool> VerifyAndMarkPaidAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        var verifier = _providers
            .Where(p => p.Method == payment.Method)
            .OfType<IPaymentVerifier>()
            .FirstOrDefault();

        if (verifier is null) return false;

        PaymentVerification result;
        try
        {
            result = await verifier.VerifyAsync(payment, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Bakong down, token expired, ...: the customer simply stays on "waiting for payment"
            _logger.LogError(ex, "Could not verify payment {PaymentId} with the provider", payment.PaymentId);
            return false;
        }

        if (!result.Paid) return false;

        await MarkPaidAsync(payment.PaymentId, result.ProviderReference);
        return true;
    }

    public async Task<PaymentResponse> SandboxConfirmAsync(int customerId, int paymentId)
    {
        var payment = await _db.Payments
            .AsNoTracking()
            .Where(p => p.PaymentId == paymentId
                        && _db.Orders.Any(o => o.OrderId == p.OrderId && o.CustomerId == customerId))
            .FirstOrDefaultAsync()
            ?? throw AppException.NotFound("Payment not found.");

        if (payment.Method == PaymentMethod.CashOnDelivery)
            throw new AppException("Cash on delivery is paid when the order is delivered.");

        return await MarkPaidAsync(payment.PaymentId, $"SBX-PAID-{payment.PaymentId}");
    }
}
