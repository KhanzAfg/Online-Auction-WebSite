using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OnlineAuction.Data;
using OnlineAuction.Models;
using OnlineAuction.Services;

var builder = WebApplication.CreateBuilder(args);

// =============================================
// 1. SERVICES CONFIGURATION
// =============================================

// Configure uploads directory (Render.com compatible)
var uploadsPath = Path.Combine(
    builder.Environment.ContentRootPath,
    builder.Environment.IsDevelopment() ? "wwwroot/uploads" : "../uploads");

Directory.CreateDirectory(uploadsPath); // Ensure exists
Directory.CreateDirectory(Path.Combine(uploadsPath, "sample")); // Sample subdirectory

// Add services to the container
builder.Services.AddControllersWithViews();

// Database Configuration with retry logic
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=onlineauction.db"));


// Identity Configuration
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// HTTP Services
builder.Services.AddHttpClient<ICardPriceService, CardPriceService>();
builder.Services.AddHttpClient();

// Cookie Policy
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.SlidingExpiration = true;
});

// Background Services
builder.Services.AddHostedService<AuctionEndCheckService>();

var app = builder.Build();

// =============================================
// 2. DATABASE INITIALIZATION
// =============================================

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await DbInitializer.InitializeAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Database initialization failed");

        if (!app.Environment.IsDevelopment())
            throw;
    }
}

// =============================================
// 3. MIDDLEWARE PIPELINE
// =============================================

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    // app.UseMigrationsEndPoint(); // ❌ Removed: not available in .NET 8
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts(); // HTTP Strict Transport Security
}

// Security Headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

app.UseHttpsRedirection();

// Static Files with caching
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=604800");
    }
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

await app.RunAsync();
