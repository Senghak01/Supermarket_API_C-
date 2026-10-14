using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Supermarket.API.Data;
using Supermarket.API.DTOs.Catalog;
using Supermarket.API.DTOs.Common;
using Supermarket.API.Exceptions;
using Supermarket.API.Models;
using Supermarket.API.Services.Interfaces;

namespace Supermarket.API.Services;

public class CatalogService : ICatalogService
{
    private readonly ApplicationDbContext _db;

    public CatalogService(ApplicationDbContext db) => _db = db;

    private static readonly Expression<Func<Category, CategoryResponse>> ToCategory = c => new CategoryResponse
    {
        CategoryId = c.CategoryId,
        Name = c.Name,
        Description = c.Description,
        ImageUrl = c.ImageUrl,
        ProductCount = c.Products.Count(p => p.IsActive)
    };

    public async Task<List<CategoryResponse>> GetCategoriesAsync()
        => await _db.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(ToCategory)
            .ToListAsync();

    public async Task<CategoryResponse> GetCategoryAsync(int categoryId)
        => await _db.Categories
            .AsNoTracking()
            .Where(c => c.CategoryId == categoryId && c.IsActive)
            .Select(ToCategory)
            .FirstOrDefaultAsync()
            ?? throw AppException.NotFound("Category not found.");

    public async Task<PagedResult<ProductListItem>> SearchProductsAsync(ProductQuery q)
    {
        if (q.MinPrice > q.MaxPrice)
            throw new AppException("MinPrice cannot be greater than MaxPrice.");

        var query = _db.Products
            .AsNoTracking()
            .Where(p => p.IsActive && p.Category.IsActive);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var term = q.Search.Trim();
            query = query.Where(p =>
                p.Name.Contains(term) ||
                p.Sku == term ||
                (p.Description != null && p.Description.Contains(term)));
        }

        if (q.CategoryId is not null)
            query = query.Where(p => p.CategoryId == q.CategoryId);

        if (q.MinPrice is not null)
            query = query.Where(p => p.Price >= q.MinPrice);

        if (q.MaxPrice is not null)
            query = query.Where(p => p.Price <= q.MaxPrice);

        if (q.InStockOnly)
            query = query.Where(p => p.Inventory != null && p.Inventory.Quantity > 0);

        // ProductId tie-breaker keeps paging stable
        query = q.Sort switch
        {
            "price_asc" => query.OrderBy(p => p.Price).ThenBy(p => p.ProductId),
            "price_desc" => query.OrderByDescending(p => p.Price).ThenBy(p => p.ProductId),
            "newest" => query.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.ProductId),
            _ => query.OrderBy(p => p.Name).ThenBy(p => p.ProductId)
        };

        var total = await query.CountAsync();

        var items = await query
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .Select(p => new ProductListItem
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Sku = p.Sku,
                Price = p.Price,
                Unit = p.Unit,
                ImageUrl = p.ImageUrl,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.Name,
                InStock = p.Inventory != null && p.Inventory.Quantity > 0
            })
            .ToListAsync();

        return new PagedResult<ProductListItem>
        {
            Items = items,
            Page = q.Page,
            PageSize = q.PageSize,
            TotalCount = total
        };
    }

    public async Task<ProductDetailResponse> GetProductAsync(int productId)
        => await _db.Products
            .AsNoTracking()
            .Where(p => p.ProductId == productId && p.IsActive && p.Category.IsActive)
            .Select(p => new ProductDetailResponse
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Description = p.Description,
                Sku = p.Sku,
                Price = p.Price,
                Unit = p.Unit,
                ImageUrl = p.ImageUrl,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.Name,
                StockQuantity = p.Inventory != null ? p.Inventory.Quantity : 0,
                InStock = p.Inventory != null && p.Inventory.Quantity > 0
            })
            .FirstOrDefaultAsync()
            ?? throw AppException.NotFound("Product not found.");
}
