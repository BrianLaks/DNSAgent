using DNSAgent.Service.Configuration;
using DNSAgent.Service.Data;
using DNSAgent.Service.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Configure Windows Service
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "DNS Agent";
});

// Load configuration
builder.Services.Configure<DnsAgentSettings>(
    builder.Configuration.GetSection("DnsAgent"));

var settings = builder.Configuration.GetSection("DnsAgent").Get<DnsAgentSettings>() 
    ?? new DnsAgentSettings();

// Database
builder.Services.AddDbContext<DnsDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// DNS Worker (always runs)
builder.Services.AddSingleton<DnsWorker>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<DnsWorker>());

// Conditional Web UI
if (settings.EnableWebUI)
{
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();
    
    // Configure Kestrel to listen on specified port
    builder.WebHost.ConfigureKestrel(serverOptions =>
    {
        serverOptions.ListenAnyIP(settings.WebUIPort);
    });
}

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DnsDbContext>();
    db.Database.EnsureCreated();
}

// Configure Web UI if enabled
if (settings.EnableWebUI)
{
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
    }

    app.UseStaticFiles();
    app.UseAntiforgery();
    
    app.MapRazorComponents<DNSAgent.Service.Components.App>()
        .AddInteractiveServerRenderMode();
}

await app.RunAsync();
