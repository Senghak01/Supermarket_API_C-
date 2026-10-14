using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Supermarket.API.Data;
using Supermarket.API.Middleware;
using Supermarket.API.Models;
using Supermarket.API.Options;
using Supermarket.API.Services;
using Supermarket.API.Services.Background;
using Supermarket.API.Services.Interfaces;
using Supermarket.API.Services.Payments;
using Supermarket.API.Services.Payments.Bakong;

var builder = WebApplication.CreateBuilder(args);

// The API only receives small JSON bodies, so refuse anything big
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 1_000_000);

// ---- Database (MySQL via Pomelo) ----
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is missing.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// ---- JWT ----
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is missing.");

if (Encoding.UTF8.GetBytes(jwtKey).Length < 32)
    throw new InvalidOperationException("Jwt:Key must be at least 32 bytes long.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

// ---- Rate limiting: slows down password guessing on register / login ----
var authPerMinute = builder.Configuration.GetValue("RateLimiting:AuthRequestsPerMinute", 10);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // One bucket per client IP address
    options.AddPolicy("auth", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = authPerMinute,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));

    // For the app asking "has the customer paid yet?" every few seconds
    options.AddPolicy("poll", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 60,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));

    // Same JSON shape as every other error: { status, message }
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { status = 429, message = "Too many attempts. Please wait a minute and try again." },
            cancellationToken);
    };
});

// ---- Options ----
builder.Services.Configure<DeliveryOptions>(builder.Configuration.GetSection(DeliveryOptions.SectionName));
builder.Services.Configure<PaymentOptions>(builder.Configuration.GetSection(PaymentOptions.SectionName));

// ---- App services ----
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IAddressService, AddressService>();
builder.Services.AddScoped<ICatalogService, CatalogService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();

// ---- Payments ----
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IPaymentProvider, CashOnDeliveryProvider>();

var bakongSection = builder.Configuration.GetSection(BakongOptions.SectionName);
var bakong = bakongSection.Get<BakongOptions>() ?? new BakongOptions();
builder.Services.Configure<BakongOptions>(bakongSection);

if (bakong.Enabled)
{
    // Real KHQR through Bakong
    if (string.IsNullOrWhiteSpace(bakong.Token)
        || string.IsNullOrWhiteSpace(bakong.AccountId)
        || string.IsNullOrWhiteSpace(bakong.MerchantName))
        throw new InvalidOperationException("Bakong is enabled, but Bakong:Token, Bakong:AccountId or Bakong:MerchantName is missing.");

    builder.Services.AddHttpClient<BakongApiClient>(client =>
    {
        client.BaseAddress = new Uri(bakong.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(15);
    });
    builder.Services.AddScoped<IPaymentProvider, BakongKhqrProvider>();
    builder.Services.AddHostedService<PaymentVerificationService>();
}
else if (builder.Environment.IsDevelopment())
{
    // Fake KHQR so the app can be tested without Bakong
    builder.Services.AddScoped<IPaymentProvider>(_ => new SandboxPaymentProvider(PaymentMethod.Khqr));
}

if (builder.Environment.IsDevelopment())
{
    // Fake card provider (no real card gateway yet). Never registered in production.
    builder.Services.AddScoped<IPaymentProvider>(_ => new SandboxPaymentProvider(PaymentMethod.Card));
}
builder.Services.AddHostedService<UnpaidOrderExpiryService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Supermarket API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the token only (no 'Bearer ' prefix)."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>(); // first, so it catches everything below

// Basic hardening headers for every response
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Development only: sample categories/products (run Update-Database first)
    using var scope = app.Services.CreateScope();
    await DevSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
}
else
{
    app.UseHsts(); // tell browsers to always use HTTPS
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();   // must come before UseAuthorization
app.UseAuthorization();
app.MapControllers();

// For uptime monitors and the hosting platform: 200 when the database answers, 503 otherwise
app.MapGet("/health", async (ApplicationDbContext db) =>
    await db.Database.CanConnectAsync()
        ? Results.Ok(new { status = "healthy" })
        : Results.Json(new { status = "unhealthy" }, statusCode: StatusCodes.Status503ServiceUnavailable))
    .AllowAnonymous();

app.Run();
