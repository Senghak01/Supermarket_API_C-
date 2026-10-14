using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Supermarket.API.DTOs.Address;
using Supermarket.API.Models;

namespace Supermarket.API.DTOs.Order;

public class PlaceOrderRequest
{
    [Range(1, int.MaxValue)]
    public int AddressId { get; set; }
}

public class OrderSummaryResponse
{
    public int OrderId { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OrderStatus Status { get; set; }

    public decimal TotalAmount { get; set; }

    // Total units across all lines
    public int ItemCount { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class OrderItemResponse
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal Subtotal { get; set; }
}

public class OrderDetailResponse
{
    public int OrderId { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OrderStatus Status { get; set; }

    public decimal Subtotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal Discount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool CanCancel { get; set; }

    public PaymentSummaryResponse? Payment { get; set; }
    public AddressResponse? DeliveryAddress { get; set; }
    public List<OrderItemResponse> Items { get; set; } = new();
}

public class PaymentSummaryResponse
{
    public int PaymentId { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PaymentMethod Method { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PaymentStatus Status { get; set; }

    public decimal Amount { get; set; }
    public DateTime? PaidAt { get; set; }
}
