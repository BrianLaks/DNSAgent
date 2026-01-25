using DNSAgent.Service.Configuration;
using DNSAgent.Service.Data;
using DNSAgent.Service.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;

var builder = WebApplication.CreateBuilder(args);

// Configure Windows Service
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "DNSAgent";
});

// Load configuration
builder.Services.Configure<DnsAgentSettings>(
    builder.Configuration.GetSection("DnsAgent"));

var settings = builder.Configuration.GetSection("DnsAgent").Get<DnsAgentSettings>() 
    ?? new DnsAgentSettings();

// Database
builder.Services.AddDbContext<DnsDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 5;
})
.AddEntityFrameworkStores<DnsDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/login";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
});

// Register Services
builder.Services.AddSingleton<ThemeService>();
builder.Services.AddSingleton<DnsWorker>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<DnsWorker>());

// Conditional Web UI
if (settings.EnableWebUI)
{
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();
    
    builder.Services.AddCascadingAuthenticationState();
    builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();
    
    // Configure Kestrel to listen on specified port
    builder.WebHost.ConfigureKestrel(serverOptions =>
    {
        serverOptions.ListenAnyIP(settings.WebUIPort);
    });
}

var app = builder.Build();

// Ensure database is created and seed default admin
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DnsDbContext>();
    db.Database.EnsureCreated();
    
    // Seed default admin user
    await DbInitializer.SeedDefaultAdminAsync(scope.ServiceProvider);
}

// Configure Web UI if enabled
if (settings.EnableWebUI)
{
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
    }

    app.UseStaticFiles();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseAntiforgery();
    
    app.MapRazorComponents<DNSAgent.Service.Components.App>()
        .AddInteractiveServerRenderMode();
}

await app.RunAsync();
