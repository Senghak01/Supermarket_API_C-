using System.ComponentModel.DataAnnotations;

namespace Supermarket.API.DTOs.Cart;

public static class CartLimits
{
    public const int MaxQuantityPerItem = 99;
}

public class AddCartItemRequest
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Range(1, CartLimits.MaxQuantityPerItem)]
    public int Quantity { get; set; } = 1;
}

public class UpdateCartItemRequest
{
    [Range(1, CartLimits.MaxQuantityPerItem)]
    public int Quantity { get; set; }
}

public class CartItemResponse
{
    public int CartItemId { get; set; }
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }

    // Live price. The price is only frozen into OrderItem at checkout.
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }

    public int AvailableQuantity { get; set; }

    // Product (and its category) are still active
    public bool IsAvailable { get; set; }

    // Available and the requested quantity is covered by stock
    public bool CanCheckout => IsAvailable && Quantity <= AvailableQuantity;
}

public class CartResponse
{
    public List<CartItemResponse> Items { get; set; } = new();

    // Total units across all lines
    public int ItemCount { get; set; }

    // Sum of lines that can currently be bought
    public decimal Subtotal { get; set; }

    // True when any line is unavailable or exceeds stock; the app should prompt the customer to fix it
    public bool HasIssues { get; set; }
}
