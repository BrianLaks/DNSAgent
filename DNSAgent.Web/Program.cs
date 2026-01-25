using DNSAgent.Web.Components;
using DNSAgent.Web.Data;
using DNSAgent.Web.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// 1. Database
builder.Services.AddDbContext<DnsDbContext>(options =>
    options.UseSqlite("Data Source=dnsagent.db"));

// 2. DNS Worker (Background Service)
builder.Services.AddSingleton<DnsWorker>();
// Register as Hosted Service using the existing Singleton instance
builder.Services.AddHostedService(sp => sp.GetRequiredService<DnsWorker>());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Ensure DB is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DnsDbContext>();
    db.Database.EnsureCreated();
}

app.Run();
