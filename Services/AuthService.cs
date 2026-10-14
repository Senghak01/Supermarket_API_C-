using Microsoft.EntityFrameworkCore;
using Supermarket.API.Data;
using Supermarket.API.DTOs.Auth;
using Supermarket.API.DTOs.Profile;
using Supermarket.API.Exceptions;
using Supermarket.API.Models;
using Supermarket.API.Services.Interfaces;

namespace Supermarket.API.Services;

public class AuthService : IAuthService
{
    private static readonly string DummyHash = BCrypt.Net.BCrypt.HashPassword("not-a-real-password");

    private readonly ApplicationDbContext _db;
    private readonly ITokenService _tokens;

    public AuthService(ApplicationDbContext db, ITokenService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _db.Customers.AnyAsync(c => c.Email == email))
            throw AppException.Conflict("Email is already registered.");

        var customer = new Customer
        {
            Username = request.Username.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        _db.Customers.Add(customer);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Two registrations with the same email at the same moment (unique index on Email)
            throw AppException.Conflict("Email is already registered.");
        }

        return BuildResponse(customer);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Email == email);

        // Always run one password check, even for an unknown email, so the response time
        // does not reveal which emails are registered
        var passwordOk = BCrypt.Net.BCrypt.Verify(request.Password, customer?.PasswordHash ?? DummyHash);

        // Same message for unknown email / wrong password / social-only account
        if (customer is null || customer.PasswordHash is null || !passwordOk)
            throw AppException.Unauthorized("Invalid email or password.");

        if (!customer.IsActive)
            throw AppException.Forbidden("This account is deactivated.");

        return BuildResponse(customer);
    }

    public async Task<CustomerResponse> GetMeAsync(int customerId)
    {
        var customer = await _db.Customers.AsNoTracking()
            .FirstOrDefaultAsync(c => c.CustomerId == customerId && c.IsActive)
            ?? throw AppException.Unauthorized("Account not found.");

        return ToResponse(customer);
    }

    private AuthResponse BuildResponse(Customer customer)
    {
        var (token, expiresAt) = _tokens.CreateToken(customer);
        return new AuthResponse { Token = token, ExpiresAt = expiresAt, Customer = ToResponse(customer) };
    }

    private static CustomerResponse ToResponse(Customer c) => new()
    {
        CustomerId = c.CustomerId,
        Username = c.Username,
        Email = c.Email
    };
}
