using Microsoft.EntityFrameworkCore;
using Supermarket.API.Models;

namespace Supermarket.API.Data;

// Development-only sample data so the browse/search endpoints have something to return.
public static class DevSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        if (await db.Categories.AnyAsync()) return;

        var fruit = new Category { Name = "Fruits & Vegetables", Description = "Fresh produce" };
        var dairy = new Category { Name = "Dairy & Eggs", Description = "Milk, cheese, yoghurt and eggs" };
        var drinks = new Category { Name = "Drinks", Description = "Water, juice and soft drinks" };

        db.Categories.AddRange(fruit, dairy, drinks);

        Product P(Category c, string name, string sku, decimal price, string unit, int stock) => new()
        {
            Category = c,
            Name = name,
            Sku = sku,
            Price = price,
            Unit = unit,
            Inventory = new Inventory { Quantity = stock }
        };

        db.Products.AddRange(
            P(fruit, "Banana", "FRT-001", 1.20m, "kg", 80),
            P(fruit, "Tomato", "VEG-001", 1.80m, "kg", 60),
            P(fruit, "Carrot", "VEG-002", 1.10m, "kg", 0),
            P(dairy, "Fresh Milk 1L", "DAI-001", 2.40m, "bottle", 40),
            P(dairy, "Eggs (10 pack)", "DAI-002", 2.90m, "pack", 25),
            P(dairy, "Plain Yoghurt 500g", "DAI-003", 1.90m, "cup", 30),
            P(drinks, "Mineral Water 1.5L", "DRK-001", 0.60m, "bottle", 200),
            P(drinks, "Orange Juice 1L", "DRK-002", 2.20m, "bottle", 45));

        await db.SaveChangesAsync();
    }
}
