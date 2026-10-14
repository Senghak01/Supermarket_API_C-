namespace Supermarket.API.Models;

public class Order
{
    public int OrderId { get; set; }

    public int CustomerId { get; set; }

    public int AddressId { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public decimal Subtotal { get; set; }

    public decimal DeliveryFee { get; set; }

    public decimal Discount { get; set; }

    public decimal TotalAmount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
