using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Supermarket.API.Options;

namespace Supermarket.API.Services.Payments.Bakong;

public class BakongApiException : Exception
{
    public BakongApiException(string message) : base(message) { }
}

public class BakongTransaction
{
    public string? Hash { get; set; }
    public string? ToAccountId { get; set; }
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
}

// Talks to the Bakong Open API (National Bank of Cambodia)
public class BakongApiClient
{
    private readonly HttpClient _http;
    private readonly BakongOptions _options;

    public BakongApiClient(HttpClient http, IOptions<BakongOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    // Returns null while no payment has been made for this QR code
    public async Task<BakongTransaction?> CheckTransactionByMd5Async(string md5, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/check_transaction_by_md5")
        {
            Content = JsonContent.Create(new { md5 })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.Token);

        using var response = await _http.SendAsync(request, cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            throw new BakongApiException(
                $"Bakong refused the request ({(int)response.StatusCode}). The token may be expired or invalid, " +
                "or the request may come from outside Cambodia.");

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = doc.RootElement;

        // responseCode 0 = found. Anything else (for example "transaction could not be found")
        // simply means "not paid yet".
        if (!root.TryGetProperty("responseCode", out var code)
            || code.ValueKind != JsonValueKind.Number
            || code.GetInt32() != 0)
            return null;

        if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
            return null;

        return new BakongTransaction
        {
            Hash = ReadString(data, "hash"),
            ToAccountId = ReadString(data, "toAccountId"),
            Currency = ReadString(data, "currency"),
            Amount = ReadDecimal(data, "amount")
        };
    }

    private static string? ReadString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static decimal? ReadDecimal(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value)) return null;

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
            return number;

        if (value.ValueKind == JsonValueKind.String
            && decimal.TryParse(value.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var text))
            return text;

        return null;
    }
}
