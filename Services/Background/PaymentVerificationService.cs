using Microsoft.Extensions.Options;
using Supermarket.API.Options;
using Supermarket.API.Services.Interfaces;

namespace Supermarket.API.Services.Background;

// Safety net: keeps asking Bakong about unpaid QR payments, so an order is confirmed even when the
// customer pays and closes the app. Only registered when Bakong is enabled.
public class PaymentVerificationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly BakongOptions _bakong;
    private readonly PaymentOptions _payment;
    private readonly ILogger<PaymentVerificationService> _logger;

    public PaymentVerificationService(
        IServiceScopeFactory scopes,
        IOptions<BakongOptions> bakong,
        IOptions<PaymentOptions> payment,
        ILogger<PaymentVerificationService> logger)
    {
        _scopes = scopes;
        _bakong = bakong.Value;
        _payment = payment.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(_bakong.PollIntervalSeconds, 2));
        using var timer = new PeriodicTimer(interval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopes.CreateScope();
                    var payments = scope.ServiceProvider.GetRequiredService<IPaymentService>();

                    // Unpaid orders are cancelled after UnpaidOrderExpiryMinutes, so older payments are not worth asking about
                    var createdAfter = DateTime.UtcNow.AddMinutes(-(_payment.UnpaidOrderExpiryMinutes + 5));
                    var paid = await payments.VerifyPendingPaymentsAsync(createdAfter, stoppingToken);

                    if (paid > 0)
                        _logger.LogInformation("{Count} payment(s) confirmed by Bakong.", paid);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Payment verification run failed.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // the app is shutting down
        }
    }
}
