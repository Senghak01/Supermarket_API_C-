using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Supermarket.API.DTOs.Catalog;
using Supermarket.API.Services.Interfaces;

namespace Supermarket.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/categories")]
public class CategoryController : ControllerBase
{
    private readonly ICatalogService _catalog;

    public CategoryController(ICatalogService catalog) => _catalog = catalog;

    [HttpGet]
    public async Task<ActionResult<List<CategoryResponse>>> GetAll()
        => Ok(await _catalog.GetCategoriesAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoryResponse>> GetById(int id)
        => Ok(await _catalog.GetCategoryAsync(id));
}
