Bakong KHQR payments (real QR + automatic payment check)

HOW IT WORKS
  1. Customer picks "Khqr" -> POST /api/orders/{id}/payments {"method":"Khqr"}
     The API builds a KHQR for the exact order amount (official NBC SDK) and returns "qrData".
     The app turns qrData into a QR image. The customer scans it with ABA, ACLEDA, Wing, Bakong, ...
  2. The app calls POST /api/payments/{paymentId}/check every ~3 seconds.
     A background job also checks every few seconds, so the order is confirmed even if the app is closed.
  3. When Bakong reports the payment, the payment becomes Paid and the order Confirmed.
  Unpaid orders still expire after 30 minutes (stock goes back).

SETUP
  1. Install the NBC package in the API project (Package Manager Console):
       Install-Package Kh.Gov.Nbc.BakongKHQR
  2. Copy into your project (same paths):
       Options/BakongOptions.cs
       Services/Payments/IPaymentVerifier.cs
       Services/Payments/Bakong/BakongApiClient.cs
       Services/Payments/Bakong/BakongKhqrProvider.cs
       Services/Background/PaymentVerificationService.cs
     Overwrite:
       Services/PaymentService.cs, Services/Interfaces/IPaymentService.cs,
       Controllers/PaymentController.cs, Program.cs
     (Program.cs = the Phase 9 file + Bakong. It does not contain the Docker options yet.)
  3. Open Services/Payments/Bakong/BakongKhqrProvider.cs. Put the cursor on BakongKHQR, press Ctrl+. and
     pick the "using ..." suggestion. Repeat for IndividualInfo / MerchantInfo / KHQRCurrency if still red.
     If "Amount = amount" says double -> decimal, delete "(double)" on the line above.
  4. appsettings.json: add the "Bakong" section from appsettings.Bakong.template.json and fill in
     AccountId, MerchantName, Currency (same currency as your product prices).
  5. User Secrets: add "Bakong:Token" (see secrets.add.json). NEVER put the token in appsettings.json,
     in Git, or in a chat.
  6. Tests (optional): copy tests/Supermarket.API.Tests/PaymentVerificationTests.cs next to your other tests.
  No database migration is needed for this step.

TRY IT
  - Start with Bakong:Enabled = true. Place an order, choose Khqr, copy "qrData" and paste it into any
    QR generator (or the app), scan and pay a small amount in the Bakong sandbox / real account.
  - Watch the API log: "1 payment(s) confirmed by Bakong."
  - Set Enabled = false in Development to go back to the fake sandbox (POST /api/payments/{id}/sandbox-confirm).
