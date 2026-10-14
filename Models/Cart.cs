namespace Supermarket.API.Models;

public class Cart
{
    public int CartId { get; set; }

    public int CustomerId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public Customer Customer { get; set; } = null!;

    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
