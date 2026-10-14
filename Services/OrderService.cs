using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Supermarket.API.Data;
using Supermarket.API.DTOs.Address;
using Supermarket.API.DTOs.Common;
using Supermarket.API.DTOs.Order;
using Supermarket.API.Exceptions;
using Supermarket.API.Models;
using Supermarket.API.Options;
using Supermarket.API.Services.Interfaces;

namespace Supermarket.API.Services;

public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _db;
    private readonly DeliveryOptions _delivery;

    public OrderService(ApplicationDbContext db, IOptions<DeliveryOptions> delivery)
    {
        _db = db;
        _delivery = delivery.Value;
    }

    // Turns the customer's cart into an order
    public async Task<OrderDetailResponse> PlaceOrderAsync(int customerId, PlaceOrderRequest request)
    {
        var address = await _db.CustomerAddresses
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AddressId == request.AddressId && a.CustomerId == customerId)
            ?? throw new AppException("Delivery address not found.");

        // Throwing below rolls the transaction back when it is disposed
        await using var tx = await _db.Database.BeginTransactionAsync();

        var cartItems = await _db.CartItems
            .Where(ci => ci.Cart.CustomerId == customerId)
            .Include(ci => ci.Product).ThenInclude(p => p.Category)
            .Include(ci => ci.Product).ThenInclude(p => p.Inventory)
            .OrderBy(ci => ci.ProductId) // fixed lock order avoids deadlocks between two checkouts
            .ToListAsync();

        if (cartItems.Count == 0)
            throw new AppException("Your cart is empty.");

        var problems = cartItems
            .Select(ci => new
            {
                ci.Product.Name,
                Requested = ci.Quantity,
                Available = ci.Product.IsActive && ci.Product.Category.IsActive
                    ? ci.Product.Inventory?.Quantity ?? 0
                    : 0
            })
            .Where(x => x.Requested > x.Available)
            .ToList();

        if (problems.Count > 0)
        {
            var details = string.Join("; ", problems.Select(p => p.Available == 0
                ? $"{p.Name} is unavailable"
                : $"{p.Name} (requested {p.Requested}, available {p.Available})"));

            throw AppException.Conflict($"Some items are not available in the requested quantity: {details}.");
        }

        var now = DateTime.UtcNow;

        // Take the stock with one atomic conditional UPDATE per product. If another checkout
        // got there first, 0 rows match and we stop, so stock can never go negative.
        foreach (var ci in cartItems)
        {
            var productId = ci.ProductId;
            var qty = ci.Quantity;

            var updated = await _db.Inventories
                .Where(i => i.ProductId == productId && i.Quantity >= qty)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(i => i.Quantity, i => i.Quantity - qty)
                    .SetProperty(i => i.UpdatedAt, now));

            if (updated == 0)
                throw AppException.Conflict($"\"{ci.Product.Name}\" just ran out of stock. Please update your cart.");
        }

        var subtotal = cartItems.Sum(ci => ci.Product.Price * ci.Quantity);
        var deliveryFee = subtotal >= _delivery.FreeDeliveryThreshold ? 0m : _delivery.FlatFee;
        var discount = 0m; // no promotions yet

        var order = new Order
        {
            CustomerId = customerId,
            AddressId = address.AddressId,
            Status = OrderStatus.Pending,
            Subtotal = subtotal,
            DeliveryFee = deliveryFee,
            Discount = discount,
            TotalAmount = subtotal + deliveryFee - discount,
            CreatedAt = now,
            Items = cartItems.Select(ci => new OrderItem
            {
                ProductId = ci.ProductId,
                ProductName = ci.Product.Name,
                UnitPrice = ci.Product.Price,
                Quantity = ci.Quantity,
                Subtotal = ci.Product.Price * ci.Quantity
            }).ToList()
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(); // assigns OrderId

        _db.StockMovements.AddRange(order.Items.Select(i => new StockMovement
        {
            ProductId = i.ProductId,
            MovementType = StockMovementType.Sale,
            Quantity = -i.Quantity,
            ReferenceId = order.OrderId,
            CreatedAt = now
        }));

        _db.CartItems.RemoveRange(cartItems);
        await _db.SaveChangesAsync();

        await tx.CommitAsync();

        return ToDetail(order, address, null);
    }

    // Order history, newest first
    public async Task<PagedResult<OrderSummaryResponse>> GetAllAsync(int customerId, int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _db.Orders.AsNoTracking().Where(o => o.CustomerId == customerId);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .ThenByDescending(o => o.OrderId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderSummaryResponse
            {
                OrderId = o.OrderId,
                Status = o.Status,
                TotalAmount = o.TotalAmount,
                ItemCount = o.Items.Sum(i => i.Quantity),
                CreatedAt = o.CreatedAt
            })
            .ToListAsync();

        return new PagedResult<OrderSummaryResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<OrderDetailResponse> GetByIdAsync(int customerId, int orderId)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderId == orderId && o.CustomerId == customerId)
            ?? throw AppException.NotFound("Order not found.");

        // The customer may have deleted the address since; old orders must still show it
        var address = await _db.CustomerAddresses
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.AddressId == order.AddressId);

        // The latest payment attempt that was not replaced or cancelled
        var payment = await _db.Payments
            .AsNoTracking()
            .Where(p => p.OrderId == orderId && p.Status != PaymentStatus.Cancelled)
            .OrderByDescending(p => p.PaymentId)
            .FirstOrDefaultAsync();

        return ToDetail(order, address, payment);
    }

    // Customers can cancel while the order is Pending or Confirmed and not paid online.
    // Stock goes back on the shelf.
    public async Task<OrderDetailResponse> CancelAsync(int customerId, int orderId)
    {
        var owned = await _db.Orders.AnyAsync(o => o.OrderId == orderId && o.CustomerId == customerId);
        if (!owned) throw AppException.NotFound("Order not found.");

        var paid = await _db.Payments.AnyAsync(p => p.OrderId == orderId && p.Status == PaymentStatus.Paid);
        if (paid)
            throw AppException.Conflict("This order is already paid. Please contact support to cancel it and get a refund.");

        if (!await CancelCoreAsync(orderId, customerId, pendingOnly: false))
            throw AppException.Conflict("This order can no longer be cancelled.");

        return await GetByIdAsync(customerId, orderId);
    }

    public async Task<int> ExpireUnpaidOrdersAsync(DateTime createdBefore)
    {
        var ids = await _db.Orders
            .AsNoTracking()
            .Where(o => o.Status == OrderStatus.Pending && o.CreatedAt < createdBefore)
            .OrderBy(o => o.OrderId)
            .Select(o => o.OrderId)
            .Take(100)
            .ToListAsync();

        var cancelled = 0;

        foreach (var id in ids)
        {
            // pendingOnly: an order that was just paid (now Confirmed) must never be expired
            if (await CancelCoreAsync(id, customerId: null, pendingOnly: true))
                cancelled++;
        }

        return cancelled;
    }

    // Cancels one order, restores its stock and cancels its unpaid payments, all in one transaction.
    // Returns false when the order is not in a cancellable state (for example someone else cancelled it first).
    private async Task<bool> CancelCoreAsync(int orderId, int? customerId, bool pendingOnly)
    {
        var now = DateTime.UtcNow;

        await using var tx = await _db.Database.BeginTransactionAsync();

        // Conditional update: two cancel requests can not both succeed, so stock is restored once
        var cancelled = await _db.Orders
            .Where(o => o.OrderId == orderId
                        && (customerId == null || o.CustomerId == customerId)
                        && (o.Status == OrderStatus.Pending || (!pendingOnly && o.Status == OrderStatus.Confirmed)))
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.Status, OrderStatus.Cancelled)
                .SetProperty(o => o.UpdatedAt, (DateTime?)now));

        if (cancelled == 0) return false;

        var lines = await _db.OrderItems
            .AsNoTracking()
            .Where(i => i.OrderId == orderId)
            .OrderBy(i => i.ProductId)
            .ToListAsync();

        foreach (var line in lines)
        {
            var productId = line.ProductId;
            var qty = line.Quantity;

            await _db.Inventories
                .Where(i => i.ProductId == productId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(i => i.Quantity, i => i.Quantity + qty)
                    .SetProperty(i => i.UpdatedAt, now));

            _db.StockMovements.Add(new StockMovement
            {
                ProductId = productId,
                MovementType = StockMovementType.Return,
                Quantity = qty,
                ReferenceId = orderId,
                CreatedAt = now
            });
        }

        await _db.Payments
            .Where(p => p.OrderId == orderId && p.Status == PaymentStatus.Pending)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, PaymentStatus.Cancelled));

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return true;
    }

    private static OrderDetailResponse ToDetail(Order order, CustomerAddress? address, Payment? payment) => new()
    {
        OrderId = order.OrderId,
        Status = order.Status,
        Subtotal = order.Subtotal,
        DeliveryFee = order.DeliveryFee,
        Discount = order.Discount,
        TotalAmount = order.TotalAmount,
        CreatedAt = order.CreatedAt,
        UpdatedAt = order.UpdatedAt,
        CanCancel = order.Status is OrderStatus.Pending or OrderStatus.Confirmed
                    && payment?.Status != PaymentStatus.Paid,
        Payment = payment is null ? null : new PaymentSummaryResponse
        {
            PaymentId = payment.PaymentId,
            Method = payment.Method,
            Status = payment.Status,
            Amount = payment.Amount,
            PaidAt = payment.PaidAt
        },
        DeliveryAddress = address is null ? null : AddressResponse.From(address),
        Items = order.Items
            .OrderBy(i => i.OrderItemId)
            .Select(i => new OrderItemResponse
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity,
                Subtotal = i.Subtotal
            })
            .ToList()
    };
}
