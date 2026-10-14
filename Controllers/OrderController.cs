using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Supermarket.API.DTOs.Common;
using Supermarket.API.DTOs.Order;
using Supermarket.API.Extensions;
using Supermarket.API.Services.Interfaces;

namespace Supermarket.API.Controllers;

[ApiController]
[Authorize]
[Route("api/order")]
public class OrderController : ControllerBase
{
    private readonly IOrderService _order;

    public OrderController(IOrderService orders) => _order = orders;

    // Checkout: turns the cart into an order
    [HttpPost]
    public async Task<ActionResult<OrderDetailResponse>> Checkout(PlaceOrderRequest request)
    {
        var order = await _order.PlaceOrderAsync(User.GetCustomerId(), request);
        return CreatedAtAction(nameof(GetById), new { id = order.OrderId }, order);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<OrderSummaryResponse>>> GetAll(int page = 1, int pageSize = 20)
        => Ok(await _order.GetAllAsync(User.GetCustomerId(), page, pageSize));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDetailResponse>> GetById(int id)
        => Ok(await _order.GetByIdAsync(User.GetCustomerId(), id));
    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<OrderDetailResponse>> Cancel(int id)
        => Ok(await _order.CancelAsync(User.GetCustomerId(), id));
}
