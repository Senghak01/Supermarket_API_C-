using Supermarket.API.DTOs.Cart;

namespace Supermarket.API.Services.Interfaces;

public interface ICartService
{
    Task<CartResponse> GetAsync(int customerId);
    Task<CartResponse> AddItemAsync(int customerId, AddCartItemRequest request);
    Task<CartResponse> UpdateItemAsync(int customerId, int cartItemId, UpdateCartItemRequest request);
    Task<CartResponse> RemoveItemAsync(int customerId, int cartItemId);
    Task ClearAsync(int customerId);
}
