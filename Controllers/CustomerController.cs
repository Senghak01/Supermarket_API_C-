using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Supermarket.API.DTOs.Profile;
using Supermarket.API.Extensions;
using Supermarket.API.Services.Interfaces;

namespace Supermarket.API.Controllers;

[ApiController]
[Authorize]
[Route("api/customers")]
public class CustomerController : ControllerBase
{
    private readonly ICustomerService _customers;

    public CustomerController(ICustomerService customers) => _customers = customers;

    [HttpGet("me")]
    public async Task<ActionResult<CustomerResponse>> GetMe()
        => Ok(await _customers.GetProfileAsync(User.GetCustomerId()));

    [HttpPut("me")]
    public async Task<ActionResult<CustomerResponse>> UpdateMe(UpdateProfileRequest request)
        => Ok(await _customers.UpdateProfileAsync(User.GetCustomerId(), request));
}
