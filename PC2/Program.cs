using IdentityLogin.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PC2.Data;
using PC2.Models;
using PC2.Services;
using System.Globalization;
using Microsoft.Extensions.Azure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Register AzureBlobUploader for DI
builder.Services.AddSingleton<AzureBlobUploader>();

// Register ImageService for DI
builder.Services.AddScoped<PC2.Services.ImageService>();

// Register AnalyticsService for DI
builder.Services.AddScoped<AnalyticsService>();

// Configure Application Insights - only add if connection string is provided
var appInsightsConnectionString = builder.Configuration.GetSection("APPLICATIONINSIGHTS_CONNECTION_STRING").Value;
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = appInsightsConnectionString;
        
    // Enable developer mode in development for faster telemetry
    if (builder.Environment.IsDevelopment())
    {
        options.EnableAdaptiveSampling = false;
        options.EnableDebugLogger = true;
    }
});


builder.Services.AddDefaultIdentity<IdentityUser>(IdentityHelper.SetIdentityOptions)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();

// email provider
builder.Services.AddTransient<IEmailSender, EmailSenderSendGrid>();

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("en-US");
    // Add culture language support here
    options.SupportedCultures = new List<CultureInfo>
    {
        new CultureInfo("en-US")
    };
});

// Configure Kestrel server options
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 50 * 1024 * 1024; // 50 MB
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.MapStaticAssets();
app.UseRequestLocalization();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();
app.MapRazorPages();

#if DEBUG
var serviceProvider = app.Services.GetRequiredService<IServiceProvider>().CreateScope();
IdentityHelper.CreateRoles(serviceProvider.ServiceProvider, IdentityHelper.Admin, IdentityHelper.Staff)
              .Wait();
IdentityHelper.CreateDefaultAdmin(serviceProvider.ServiceProvider, IdentityHelper.Admin)
              .Wait();
#endif

// If a user tries to access the register page, redirect them to the login page
app.Map("/Identity/Account/Register", async context =>
{
    context.Response.Redirect("/Identity/Account/Login", true, true);
    await Task.CompletedTask;
});

app.Run();
