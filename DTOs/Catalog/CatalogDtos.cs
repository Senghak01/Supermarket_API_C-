using System.ComponentModel.DataAnnotations;

namespace Supermarket.API.DTOs.Catalog;

public class CategoryResponse
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int ProductCount { get; set; }
}

public class ProductQuery
{
    [MaxLength(100)]
    public string? Search { get; set; }

    public int? CategoryId { get; set; }

    [Range(0, 1_000_000)]
    public decimal? MinPrice { get; set; }

    [Range(0, 1_000_000)]
    public decimal? MaxPrice { get; set; }

    public bool InStockOnly { get; set; }

    // name (default) | price_asc | price_desc | newest
    public string? Sort { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 50)]
    public int PageSize { get; set; } = 20;
}

public class ProductListItem
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool InStock { get; set; }
}

public class ProductDetailResponse : ProductListItem
{
    public string? Description { get; set; }
    public int StockQuantity { get; set; }
}
