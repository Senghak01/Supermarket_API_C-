namespace Supermarket.API.Options;

public class DeliveryOptions
{
    public const string SectionName = "Delivery";

    // Charged when the subtotal is below FreeDeliveryThreshold
    public decimal FlatFee { get; set; } = 1.50m;

    // Subtotal at or above this amount ships free
    public decimal FreeDeliveryThreshold { get; set; } = 20m;
}
