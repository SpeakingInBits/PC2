using IdentityLogin.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PC2.Data;
using PC2.Filters;
using PC2.Models;
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

builder.Services.AddApplicationInsightsTelemetry(options =>
    options.ConnectionString = builder.Configuration.GetSection("APPLICATIONINSIGHTS_CONNECTION_STRING").Value);

builder.Services.AddDefaultIdentity<IdentityUser>(IdentityHelper.SetIdentityOptions)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<ApplicationInsightsPageViewTracker>();
});

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
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB
});
builder.Services.AddAzureClients(clientBuilder =>
{
    clientBuilder.AddBlobServiceClient(builder.Configuration["AzureBlob:StorageConnection:blobServiceUri"]!).WithName("StorageConnection");
    clientBuilder.AddQueueServiceClient(builder.Configuration["AzureBlob:StorageConnection:queueServiceUri"]!).WithName("StorageConnection");
    clientBuilder.AddTableServiceClient(builder.Configuration["AzureBlob:StorageConnection:tableServiceUri"]!).WithName("StorageConnection");
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
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

#if DEBUG
var serviceProvider = app.Services.GetRequiredService<IServiceProvider>().CreateScope();
IdentityHelper.CreateRoles(serviceProvider.ServiceProvider, IdentityHelper.Admin)
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
