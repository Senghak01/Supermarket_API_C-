using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Supermarket.API.DTOs.Cart;
using Supermarket.API.Extensions;
using Supermarket.API.Services.Interfaces;

namespace Supermarket.API.Controllers;

[ApiController]
[Authorize]
[Route("api/cart")]
public class CartController : ControllerBase
{
    private readonly ICartService _cart;

    public CartController(ICartService cart) => _cart = cart;

    [HttpGet]
    public async Task<ActionResult<CartResponse>> Get()
        => Ok(await _cart.GetAsync(User.GetCustomerId()));

    [HttpPost("items")]
    public async Task<ActionResult<CartResponse>> AddItem(AddCartItemRequest request)
        => Ok(await _cart.AddItemAsync(User.GetCustomerId(), request));

    [HttpPut("items/{cartItemId:int}")]
    public async Task<ActionResult<CartResponse>> UpdateItem(int cartItemId, UpdateCartItemRequest request)
        => Ok(await _cart.UpdateItemAsync(User.GetCustomerId(), cartItemId, request));

    [HttpDelete("items/{cartItemId:int}")]
    public async Task<ActionResult<CartResponse>> RemoveItem(int cartItemId)
        => Ok(await _cart.RemoveItemAsync(User.GetCustomerId(), cartItemId));

    [HttpDelete]
    public async Task<IActionResult> Clear()
    {
        await _cart.ClearAsync(User.GetCustomerId());
        return NoContent();
    }
}
