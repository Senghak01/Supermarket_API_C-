using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Supermarket.API.DTOs.Catalog;
using Supermarket.API.DTOs.Common;
using Supermarket.API.Services.Interfaces;

namespace Supermarket.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/products")]
public class ProductController : ControllerBase
{
    private readonly ICatalogService _catalog;

    public ProductController(ICatalogService catalog) => _catalog = catalog;

    // GET /api/products?search=milk&categoryId=2&minPrice=1&maxPrice=10&inStockOnly=true&sort=price_asc&page=1&pageSize=20
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductListItem>>> Search([FromQuery] ProductQuery query)
        => Ok(await _catalog.SearchProductsAsync(query));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDetailResponse>> GetById(int id)
        => Ok(await _catalog.GetProductAsync(id));
}
