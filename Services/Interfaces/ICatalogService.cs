using Supermarket.API.DTOs.Catalog;
using Supermarket.API.DTOs.Common;

namespace Supermarket.API.Services.Interfaces;

public interface ICatalogService
{
    Task<List<CategoryResponse>> GetCategoriesAsync();
    Task<CategoryResponse> GetCategoryAsync(int categoryId);
    Task<PagedResult<ProductListItem>> SearchProductsAsync(ProductQuery query);
    Task<ProductDetailResponse> GetProductAsync(int productId);
}
