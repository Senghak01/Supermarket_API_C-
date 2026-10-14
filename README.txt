========================================================================
 SUPERMARKET API - SETUP & TROUBLESHOOTING GUIDE (README.txt)
========================================================================

------------------------------------------------------------------------
1. FIXING DATABASE CONNECTION ERRORS (MySQL + Pomelo EF Core)
------------------------------------------------------------------------
If your application fails to connect to MySQL during startup or database 
migrations, follow these checks:

A. Update `appsettings.json` Database Connection String
   Ensure your MySQL server details (Host, Database, User, Password) are 
   configured properly.
   
   Example `appsettings.json`:
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Port=3306;Database=supermarket_db;Uid=root;Pwd=YOUR_MYSQL_PASSWORD;"
     }
   }

B. Verify MySQL Service & Package Setup
   1. Ensure MySQL Server is running locally or on your target host.
   2. Verify that `Pomelo.EntityFrameworkCore.MySql` is installed in your
      API project dependencies.

C. Apply Migrations to MySQL
   Open Visual Studio Package Manager Console (Tools > NuGet Package Manager):
   
   PM> Add-Migration InitialCreate
   PM> Update-Database

------------------------------------------------------------------------
2. SECURE USER SECRETS SETUP (Prevent Git Secret Leaks)
------------------------------------------------------------------------
Never store secret tokens (JWT Key, Bakong Token, DB Passwords) in 
appsettings.json when committing code.

1. Right-click the backend project in Visual Studio Solution Explorer.
2. Select "Manage User Secrets".
3. Add your sensitive credentials to `secrets.json`:

   {
     "ConnectionStrings:DefaultConnection": "Server=localhost;Port=3306;Database=supermarket_db;Uid=root;Pwd=YOUR_PASSWORD;",
     "Jwt:Key": "YOUR_STRONG_JWT_SECRET_KEY_HERE_MIN_32_CHARS",
     "Bakong:Token": "YOUR_BAKONG_OPEN_API_JWT_TOKEN"
   }

------------------------------------------------------------------------
3. BAKONG KHQR PAYMENT INTEGRATION SETUP
------------------------------------------------------------------------
A. Add Bakong Configuration to `appsettings.json`:

   {
     "Bakong": {
       "Enabled": true,
       "BaseUrl": "https://api-bakong.nbc.gov.kh",
       "AccountId": "2040292@aba",
       "MerchantName": "Bule Panda",
       "MerchantCity": "PHNOM PENH",
       "Currency": "USD",
       "AccountType": "Individual",
       "PollIntervalSeconds": 5
     },
     "Payment": {
       "UnpaidOrderExpiryMinutes": 30
     }
   }

B. Program.cs Registration:
   Ensure `DbContext`, `BakongKhqrProvider`, and Payment background workers 
   are registered in `Program.cs`:

   builder.Services.AddDbContext<ApplicationDbContext>(options =>
       options.UseMySql(
           builder.Configuration.GetConnectionString("DefaultConnection"),
           ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
       ));

------------------------------------------------------------------------
4. RUNNING & TESTING PAYMENT ENDPOINTS
------------------------------------------------------------------------
1. Start the API (F5 in Visual Studio).
2. Authenticate via POST /api/auth/login or /api/auth/google to get JWT.
3. Create an order and initiate payment:
   POST /api/orders/{orderId}/payments
   Body: { "method": "Khqr" }
4. Response returns KHQR payload string / MD5 transaction hash.
5. Once scanned and paid via Bakong/ABA app, the payment worker updates 
   Order Status from `Pending` to `Confirmed`.

------------------------------------------------------------------------
5. COMMON ERRORS & SOLUTIONS
------------------------------------------------------------------------
- "Cannot connect to MySQL server":
  Check MySQL service status and verify `DefaultConnection` credentials.

- "Bakong is enabled, but Bakong:Token is missing":
  Add `"Bakong:Token": "..."` to Visual Studio User Secrets (`secrets.json`).

- "Table 'supermarket_db.Payments' doesn't exist":
  Run `Add-Migration AddPayments` and `Update-Database` in Package Manager Console.
========================================================================