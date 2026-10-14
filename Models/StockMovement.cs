namespace Supermarket.API.Models;

public enum StockMovementType
{
    Purchase,
    Sale,
    Return,
    Adjustment
}

public class StockMovement
{
    public int StockMovementId { get; set; }

    public int ProductId { get; set; }

    // Null for system-made movements (customer orders). The Employee FK is added in Phase 10.
    public int? EmployeeId { get; set; }

    public StockMovementType MovementType { get; set; }

    // Signed: negative removes stock (Sale), positive adds stock (Purchase, Return)
    public int Quantity { get; set; }

    // OrderId for Sale/Return, PurchaseId for Purchase
    public int? ReferenceId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
