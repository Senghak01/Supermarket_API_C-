using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Supermarket.API.DTOs.Payments;
using Supermarket.API.Extensions;
using Supermarket.API.Services.Interfaces;

namespace Supermarket.API.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _payments;
    private readonly IWebHostEnvironment _env;

    public PaymentController(IPaymentService payments, IWebHostEnvironment env)
    {
        _payments = payments;
        _env = env;
    }

    // Choose how to pay for an order: { "method": "CashOnDelivery" | "Khqr" | "Card" }
    [HttpPost("orders/{orderId:int}/payments")]
    public async Task<ActionResult<PaymentResponse>> Initiate(int orderId, InitiatePaymentRequest request)
        => Ok(await _payments.InitiateAsync(User.GetCustomerId(), orderId, request));

    [HttpGet("orders/{orderId:int}/payments")]
    public async Task<ActionResult<List<PaymentResponse>>> GetForOrder(int orderId)
        => Ok(await _payments.GetForOrderAsync(User.GetCustomerId(), orderId));

    // The app calls this every few seconds while it shows the QR code. We ask the provider
    // (Bakong) whether the customer has paid and confirm the order when they have.
    [EnableRateLimiting("poll")]
    [HttpPost("payments/{paymentId:int}/check")]
    public async Task<ActionResult<PaymentResponse>> Check(int paymentId)
        => Ok(await _payments.RefreshAsync(User.GetCustomerId(), paymentId));

    // Development only: simulates the provider telling us the customer paid
    [HttpPost("payments/{paymentId:int}/sandbox-confirm")]
    public async Task<ActionResult<PaymentResponse>> SandboxConfirm(int paymentId)
    {
        if (!_env.IsDevelopment()) return NotFound();

        return Ok(await _payments.SandboxConfirmAsync(User.GetCustomerId(), paymentId));
    }
}
