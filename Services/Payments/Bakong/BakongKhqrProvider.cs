using kh.gov.nbc.bakong_khqr;
using kh.gov.nbc.bakong_khqr.model;
using Microsoft.Extensions.Options;
using Supermarket.API.Models;
using Supermarket.API.Options;

// Add the usings for the NBC SDK package (Kh.Gov.Nbc.BakongKHQR) here:
// put the cursor on "BakongKHQR", press Ctrl+. and choose the "using ..." suggestion.
// Do the same for IndividualInfo, MerchantInfo and KHQRCurrency if they stay red.

namespace Supermarket.API.Services.Payments.Bakong;

// KHQR payments through Bakong:
//  1. InitiateAsync builds a dynamic KHQR for the exact order amount with the official NBC SDK.
//     The app shows it as a QR code; the customer scans and pays with any Cambodian banking app.
//  2. VerifyAsync asks the Bakong Open API whether that QR (identified by its MD5) was paid.
public class BakongKhqrProvider : IPaymentProvider, IPaymentVerifier
{
    private readonly BakongApiClient _client;
    private readonly BakongOptions _options;
    private readonly ILogger<BakongKhqrProvider> _logger;

    public BakongKhqrProvider(
        BakongApiClient client,
        IOptions<BakongOptions> options,
        ILogger<BakongKhqrProvider> logger)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    public PaymentMethod Method => PaymentMethod.Khqr;

    public bool RequiresOnlinePayment => true;

    public Task<PaymentInitResult> InitiateAsync(Order order, Payment payment, CancellationToken cancellationToken = default)
    {
        var currency = _options.Currency.Equals("KHR", StringComparison.OrdinalIgnoreCase)
            ? KHQRCurrency.KHR
            : KHQRCurrency.USD;

        // Unique per payment attempt, so every QR (and its MD5) is unique
        var billNumber = $"P{payment.PaymentId}";

        // If the compiler says it cannot convert double to decimal, remove "(double)"
        var amount = (double)payment.Amount;

        string qr;
        string md5;

        if (_options.AccountType.Equals("Merchant", StringComparison.OrdinalIgnoreCase))
        {
            var result = BakongKHQR.GenerateMerchant(new MerchantInfo
            {
                BakongAccountID = _options.AccountId,
                MerchantID = _options.MerchantId,
                AcquiringBank = _options.AcquiringBank,
                Currency = currency,
                Amount = amount,
                MerchantName = _options.MerchantName,
                MerchantCity = _options.MerchantCity,
                BillNumber = billNumber,
                MobileNumber = _options.MobileNumber,
                StoreLabel = _options.StoreLabel
            });

            if (result.Status.Code != 0)
                throw new InvalidOperationException($"KHQR generation failed: {result.Status.Message}");

            qr = result.Data.QR;
            md5 = result.Data.MD5;
        }
        else
        {
            var result = BakongKHQR.GenerateIndividual(new IndividualInfo
            {
                BakongAccountID = _options.AccountId,
                Currency = currency,
                Amount = amount,
                MerchantName = _options.MerchantName,
                MerchantCity = _options.MerchantCity,
                BillNumber = billNumber,
                MobileNumber = _options.MobileNumber,
                StoreLabel = _options.StoreLabel
            });

            if (result.Status.Code != 0)
                throw new InvalidOperationException($"KHQR generation failed: {result.Status.Message}");

            qr = result.Data.QR;
            md5 = result.Data.MD5;
        }

        // The MD5 is stored as the payment's TransactionId and used to look the payment up later
        return Task.FromResult(new PaymentInitResult { TransactionId = md5, QrData = qr });
    }

    public async Task<PaymentVerification> VerifyAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(payment.TransactionId))
            return new PaymentVerification();

        var transaction = await _client.CheckTransactionByMd5Async(payment.TransactionId, cancellationToken);

        if (transaction is null)
            return new PaymentVerification(); // not paid yet

        // Safety checks: never confirm an order for the wrong amount or the wrong currency
        if (transaction.Amount is not null && transaction.Amount.Value != payment.Amount)
        {
            _logger.LogWarning(
                "Bakong transaction for payment {PaymentId} has amount {Actual} but {Expected} was expected. Not confirmed.",
                payment.PaymentId, transaction.Amount, payment.Amount);
            return new PaymentVerification();
        }

        if (transaction.Currency is not null
            && !transaction.Currency.Equals(_options.Currency, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Bakong transaction for payment {PaymentId} is in {Actual} but {Expected} was expected. Not confirmed.",
                payment.PaymentId, transaction.Currency, _options.Currency);
            return new PaymentVerification();
        }

        return new PaymentVerification { Paid = true, ProviderReference = transaction.Hash };
    }
}
