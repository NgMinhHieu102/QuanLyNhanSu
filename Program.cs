using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.Cookie.Name = "HumaHr.Auth";
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddDbContext<HumaHr.Data.HumaHrDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddScoped<HumaHr.Services.HrRepository>();
builder.Services.AddScoped<HumaHr.Services.AccountService>();

var app = builder.Build();

// This SQL Server edition initializes a new database from the EF Core model.
// Existing MySQL databases must be exported and migrated separately.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HumaHr.Data.HumaHrDbContext>();
    db.Database.EnsureCreated();
    var accounts = scope.ServiceProvider.GetRequiredService<HumaHr.Services.AccountService>();
    accounts.EnsureInitialAdmin(
        builder.Configuration["AdminAccount:Email"] ?? "admin@gmail.com",
        builder.Configuration["AdminAccount:FullName"] ?? "Quản trị viên",
        builder.Configuration["AdminAccount:Password"] ?? "HumaHr@2026");
    HumaHr.Data.SeedData.Initialize(db);
    HumaHr.Data.SeedData.EnsureDepartmentPositions(db);
    if (app.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("DemoAttendanceSeed:Enabled"))
    {
        var period = builder.Configuration["DemoAttendanceSeed:Month"];
        if (!DateTime.TryParseExact(period, "yyyy-MM", System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var month))
            throw new InvalidOperationException("DemoAttendanceSeed:Month phải có định dạng yyyy-MM.");
        var inserted = HumaHr.Data.SeedData.EnsureDemoAttendance(db, month);
        app.Logger.LogInformation("Demo attendance seed {Month}: added {Count} missing records.", period, inserted);
    }

}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
// Serve wwwroot assets in both Development and Production. MapStaticAssets supplies
// fingerprinted endpoints, while UseStaticFiles keeps plain ~/css and ~/lib URLs
// working for layouts and controllers that do not use static-asset link conventions.
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "account",
    pattern: "{controller=Account}/{action=Login}/{id?}");


app.Run();
