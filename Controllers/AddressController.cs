using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Supermarket.API.DTOs.Address;
using Supermarket.API.Extensions;
using Supermarket.API.Services.Interfaces;

namespace Supermarket.API.Controllers;

[ApiController]
[Authorize]
[Route("api/addresses")]
public class AddressController : ControllerBase
{
    private readonly IAddressService _addresses;

    public AddressController(IAddressService addresses) => _addresses = addresses;

    [HttpGet]
    public async Task<ActionResult<List<AddressResponse>>> GetAll()
        => Ok(await _addresses.GetAllAsync(User.GetCustomerId()));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AddressResponse>> GetById(int id)
        => Ok(await _addresses.GetByIdAsync(User.GetCustomerId(), id));

    [HttpPost]
    public async Task<ActionResult<AddressResponse>> Create(CreateAddressRequest request)
    {
        var created = await _addresses.CreateAsync(User.GetCustomerId(), request);
        return CreatedAtAction(nameof(GetById), new { id = created.AddressId }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AddressResponse>> Update(int id, UpdateAddressRequest request)
        => Ok(await _addresses.UpdateAsync(User.GetCustomerId(), id, request));

    [HttpPut("{id:int}/default")]
    public async Task<ActionResult<AddressResponse>> SetDefault(int id)
        => Ok(await _addresses.SetDefaultAsync(User.GetCustomerId(), id));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _addresses.DeleteAsync(User.GetCustomerId(), id);
        return NoContent();
    }
}
