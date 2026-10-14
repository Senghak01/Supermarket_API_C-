using Microsoft.Extensions.Options;
using Supermarket.API.Options;
using Supermarket.API.Services.Interfaces;

namespace Supermarket.API.Services.Background;

// Every minute: cancels orders that are still Pending (no payment method chosen, or an online
// payment that never arrived) after UnpaidOrderExpiryMinutes, and puts their stock back.
public class UnpaidOrderExpiryService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly PaymentOptions _options;
    private readonly ILogger<UnpaidOrderExpiryService> _logger;

    public UnpaidOrderExpiryService(
        IServiceScopeFactory scopes,
        IOptions<PaymentOptions> options,
        ILogger<UnpaidOrderExpiryService> logger)
    {
        _scopes = scopes;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopes.CreateScope();
                    var orders = scope.ServiceProvider.GetRequiredService<IOrderService>();

                    var cutoff = DateTime.UtcNow.AddMinutes(-_options.UnpaidOrderExpiryMinutes);
                    var cancelled = await orders.ExpireUnpaidOrdersAsync(cutoff);

                    if (cancelled > 0)
                        _logger.LogInformation("Cancelled {Count} unpaid order(s) and released their stock.", cancelled);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Unpaid order expiry run failed.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // the app is shutting down
        }
    }
}
