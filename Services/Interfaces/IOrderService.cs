using Supermarket.API.DTOs.Common;
using Supermarket.API.DTOs.Order;

namespace Supermarket.API.Services.Interfaces;

public interface IOrderService
{
    Task<OrderDetailResponse> PlaceOrderAsync(int customerId, PlaceOrderRequest request);
    Task<PagedResult<OrderSummaryResponse>> GetAllAsync(int customerId, int page, int pageSize);
    Task<OrderDetailResponse> GetByIdAsync(int customerId, int orderId);
    Task<OrderDetailResponse> CancelAsync(int customerId, int orderId);

    // Cancels Pending orders created before the given time and puts their stock back. Returns how many.
    Task<int> ExpireUnpaidOrdersAsync(DateTime createdBefore);
}
