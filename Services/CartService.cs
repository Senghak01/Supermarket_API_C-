using Microsoft.EntityFrameworkCore;
using Supermarket.API.Data;
using Supermarket.API.DTOs.Cart;
using Supermarket.API.Exceptions;
using Supermarket.API.Models;
using Supermarket.API.Services.Interfaces;

namespace Supermarket.API.Services;

public class CartService : ICartService
{
    private readonly ApplicationDbContext _db;

    public CartService(ApplicationDbContext db) => _db = db;

    public async Task<CartResponse> GetAsync(int customerId)
    {
        // Inactive products are included on purpose so the customer can see and remove them
        var items = await _db.CartItems
            .AsNoTracking()
            .Where(ci => ci.Cart.CustomerId == customerId)
            .OrderBy(ci => ci.CartItemId)
            .Select(ci => new CartItemResponse
            {
                CartItemId = ci.CartItemId,
                ProductId = ci.ProductId,
                Name = ci.Product.Name,
                Sku = ci.Product.Sku,
                Unit = ci.Product.Unit,
                ImageUrl = ci.Product.ImageUrl,
                UnitPrice = ci.Product.Price,
                Quantity = ci.Quantity,
                AvailableQuantity = ci.Product.Inventory != null ? ci.Product.Inventory.Quantity : 0,
                IsAvailable = ci.Product.IsActive && ci.Product.Category.IsActive
            })
            .ToListAsync();

        foreach (var i in items)
            i.LineTotal = i.UnitPrice * i.Quantity;

        return new CartResponse
        {
            Items = items,
            ItemCount = items.Sum(i => i.Quantity),
            Subtotal = items.Where(i => i.CanCheckout).Sum(i => i.LineTotal),
            HasIssues = items.Any(i => !i.CanCheckout)
        };
    }

    // Adds the product, or increases the quantity if it is already in the cart
    public async Task<CartResponse> AddItemAsync(int customerId, AddCartItemRequest request)
    {
        var product = await _db.Products
            .AsNoTracking()
            .Where(p => p.ProductId == request.ProductId && p.IsActive && p.Category.IsActive)
            .Select(p => new { p.ProductId, Stock = p.Inventory != null ? p.Inventory.Quantity : 0 })
            .FirstOrDefaultAsync()
            ?? throw AppException.NotFound("Product not found.");

        var cart = await GetOrCreateCartAsync(customerId);

        var item = await _db.CartItems
            .FirstOrDefaultAsync(ci => ci.CartId == cart.CartId && ci.ProductId == product.ProductId);

        var newQuantity = (item?.Quantity ?? 0) + request.Quantity;

        if (newQuantity > CartLimits.MaxQuantityPerItem)
            throw new AppException($"You can add up to {CartLimits.MaxQuantityPerItem} of the same product.");

        if (newQuantity > product.Stock)
            throw AppException.Conflict($"Not enough stock. Available: {product.Stock}.");

        if (item is null)
            _db.CartItems.Add(new CartItem { CartId = cart.CartId, ProductId = product.ProductId, Quantity = newQuantity });
        else
            item.Quantity = newQuantity;

        cart.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Two requests added the same product at the same moment (unique index CartId + ProductId)
            throw AppException.Conflict("The cart was changed by another request. Please try again.");
        }

        return await GetAsync(customerId);
    }

    // Sets the quantity of a cart line
    public async Task<CartResponse> UpdateItemAsync(int customerId, int cartItemId, UpdateCartItemRequest request)
    {
        var item = await _db.CartItems
            .Include(ci => ci.Cart)
            .Include(ci => ci.Product).ThenInclude(p => p.Category)
            .Include(ci => ci.Product).ThenInclude(p => p.Inventory)
            .FirstOrDefaultAsync(ci => ci.CartItemId == cartItemId && ci.Cart.CustomerId == customerId)
            ?? throw AppException.NotFound("Cart item not found.");

        if (!item.Product.IsActive || !item.Product.Category.IsActive)
            throw AppException.Conflict("This product is no longer available. Remove it from your cart.");

        var stock = item.Product.Inventory?.Quantity ?? 0;
        if (request.Quantity > stock)
            throw AppException.Conflict($"Not enough stock. Available: {stock}.");

        item.Quantity = request.Quantity;
        item.Cart.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return await GetAsync(customerId);
    }

    public async Task<CartResponse> RemoveItemAsync(int customerId, int cartItemId)
    {
        var item = await _db.CartItems
            .Include(ci => ci.Cart)
            .FirstOrDefaultAsync(ci => ci.CartItemId == cartItemId && ci.Cart.CustomerId == customerId)
            ?? throw AppException.NotFound("Cart item not found.");

        item.Cart.UpdatedAt = DateTime.UtcNow;
        _db.CartItems.Remove(item);
        await _db.SaveChangesAsync();

        return await GetAsync(customerId);
    }

    public async Task ClearAsync(int customerId)
    {
        var cartId = await _db.Carts
            .Where(c => c.CustomerId == customerId)
            .Select(c => (int?)c.CartId)
            .FirstOrDefaultAsync();

        if (cartId is not null)
            await _db.CartItems.Where(ci => ci.CartId == cartId).ExecuteDeleteAsync();
    }

    private async Task<Cart> GetOrCreateCartAsync(int customerId)
    {
        var cart = await _db.Carts.FirstOrDefaultAsync(c => c.CustomerId == customerId);
        if (cart is not null) return cart;

        cart = new Cart { CustomerId = customerId };
        _db.Carts.Add(cart);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Another request created the cart first (unique index on CustomerId)
            _db.Entry(cart).State = EntityState.Detached;
            cart = await _db.Carts.FirstAsync(c => c.CustomerId == customerId);
        }

        return cart;
    }
}
