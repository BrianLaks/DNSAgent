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

// Set current directory to executable location (CRITICAL for Service DB access)
Directory.SetCurrentDirectory(AppContext.BaseDirectory);

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
builder.Services.AddHttpClient();
builder.Services.AddSingleton<ThemeService>();
builder.Services.AddSingleton<DeArrowService>();
builder.Services.AddSingleton<DnsWorker>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<DnsWorker>());

// API Controllers
builder.Services.AddControllers();

// CORS for local network access
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalNetworkOnly", policy =>
    {
        policy.AllowAnyOrigin() // Extensions have irregular origins (chrome-extension://...)
        .AllowAnyMethod()
        .AllowAnyHeader();
    });
});

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

    // --- DATABASE MIGRATION START ---
    
    // 1. QueryLogs Table
    try { db.Database.ExecuteSqlRaw("ALTER TABLE QueryLogs ADD COLUMN SourceHostname TEXT DEFAULT ''"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE QueryLogs ADD COLUMN ClientId TEXT"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE QueryLogs ADD COLUMN Transport TEXT DEFAULT 'UDP'"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE QueryLogs ADD COLUMN IsDnssec INTEGER NOT NULL DEFAULT 0"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE QueryLogs ADD COLUMN ResponseTimeMs INTEGER NOT NULL DEFAULT 0"); } catch { }

    // 2. Devices Table (Create if not exists)
    try {
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS Devices (
                Id TEXT PRIMARY KEY,
                MachineName TEXT NOT NULL,
                UserName TEXT NOT NULL,
                LastIP TEXT NOT NULL,
                LastSeen TEXT NOT NULL
            );");
    } catch { }
    
    // Ensure Devices columns exist (for upgrades from earlier versions)
    try { db.Database.ExecuteSqlRaw("ALTER TABLE Devices ADD COLUMN MachineName TEXT DEFAULT ''"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE Devices ADD COLUMN UserName TEXT DEFAULT ''"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE Devices ADD COLUMN LastIP TEXT DEFAULT ''"); } catch { }

    // 3. YouTubeStats Table
    try {
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS YouTubeStats (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Timestamp TEXT NOT NULL,
                AdsBlocked INTEGER NOT NULL,
                AdsFailed INTEGER NOT NULL,
                SponsorsSkipped INTEGER NOT NULL,
                TitlesCleaned INTEGER NOT NULL DEFAULT 0,
                ThumbnailsReplaced INTEGER NOT NULL DEFAULT 0,
                TimeSavedSeconds REAL NOT NULL,
                DeviceName TEXT,
                FilterVersion TEXT
            );");
    } catch { }

    // Ensure YouTubeStats columns
    try { db.Database.ExecuteSqlRaw("ALTER TABLE YouTubeStats ADD COLUMN TitlesCleaned INTEGER NOT NULL DEFAULT 0"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE YouTubeStats ADD COLUMN ThumbnailsReplaced INTEGER NOT NULL DEFAULT 0"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE YouTubeStats ADD COLUMN DeviceName TEXT"); } catch { }

    // 4. Other tables
    try {
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS BlacklistedDomains (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Domain TEXT NOT NULL,
                Reason TEXT,
                AddedAt TEXT NOT NULL
            );");
    } catch { }

    try {
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS DnsProviders (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                PrimaryIP TEXT NOT NULL,
                SecondaryIP TEXT,
                DoHUrl TEXT,
                IsActive INTEGER NOT NULL DEFAULT 0,
                IsPreset INTEGER NOT NULL DEFAULT 0
            );");
    } catch { }

    // --- DATABASE MIGRATION END ---

    // Seed default providers if table is empty
    try 
    {
        var hasProviders = db.DnsProviders.Any();
        if (!hasProviders)
        {
            db.DnsProviders.AddRange(new List<DnsProvider>
            {
                new DnsProvider { Name = "Google DNS", PrimaryIP = "8.8.8.8", SecondaryIP = "8.8.4.4", DoHUrl = "https://dns.google/dns-query", IsActive = true, IsPreset = true },
                new DnsProvider { Name = "Cloudflare", PrimaryIP = "1.1.1.1", SecondaryIP = "1.0.0.1", DoHUrl = "https://cloudflare-dns.com/dns-query", IsActive = false, IsPreset = true },
                new DnsProvider { Name = "Quad9", PrimaryIP = "9.9.9.9", SecondaryIP = "149.112.112.112", DoHUrl = "https://dns.quad9.net/dns-query", IsActive = false, IsPreset = true }
            });
            db.SaveChanges();
        }
    } catch { }
    
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
    
    // API Middleware (must be before authentication)
    app.UseMiddleware<DNSAgent.Service.Middleware.LocalNetworkOnlyMiddleware>();
    app.UseCors("LocalNetworkOnly");
    
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseAntiforgery();
    
    // Map API controllers
    app.MapControllers();
    
    app.MapRazorComponents<DNSAgent.Service.Components.App>()
        .AddInteractiveServerRenderMode();
}

try 
{
    await app.RunAsync();
}
catch (Exception ex)
{
    var logPath = Path.Combine(AppContext.BaseDirectory, "startup_error.txt");
    File.WriteAllText(logPath, $"Fatal Startup Error ({DateTime.Now}): {ex}");
    throw;
}
