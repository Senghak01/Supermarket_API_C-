namespace Supermarket.API.Models;

public enum OrderStatus
{
    Pending,
    Confirmed,
    Preparing,
    ReadyForDelivery,
    OutForDelivery,
    Delivered,
    Cancelled
}
