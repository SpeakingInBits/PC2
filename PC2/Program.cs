using IdentityLogin.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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

// Built-in Identity UI pages the site doesn't use (registration, email confirmation, password reset) return 404.
// PostConfigure, because setting up Razor Pages replaces the conventions collection that Configure would add to.
builder.Services.PostConfigure<Microsoft.AspNetCore.Mvc.RazorPages.RazorPagesOptions>(options =>
    options.Conventions.Add(new UnusedIdentityPagesConvention()));

// Register AzureBlobUploader for DI
builder.Services.AddSingleton<AzureBlobUploader>();

// Register ImageService for DI
builder.Services.AddScoped<PC2.Services.ImageService>();

// Register AnalyticsService for DI. A singleton, so its Log Analytics client is created once and its results cache is shared.
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<AnalyticsService>();

// Sender.net mailing list sign-up form settings
builder.Services.AddOptions<SenderNetOptions>()
    .Bind(builder.Configuration.GetSection(SenderNetOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Register ReCaptchaService for DI
builder.Services.AddOptions<ReCaptchaOptions>()
    .Bind(builder.Configuration.GetSection(ReCaptchaOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddHttpClient<IReCaptchaService, ReCaptchaService>(client =>
{
    // Fail fast if Google is slow so form submissions don't hang for the 100 second default
    client.Timeout = TimeSpan.FromSeconds(10);
});

// Compresses pages (Brotli or Gzip). Static files are already compressed at build time by MapStaticAssets.
// Safe over HTTPS here: the only secret in pages, the antiforgery token, is different in every response.
builder.Services.AddResponseCompression(options => options.EnableForHttps = true);

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
// Every POST, PUT, PATCH and DELETE must carry a valid antiforgery token
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

// email provider. "File" saves emails to disk instead of sending them (used in development)
if (builder.Configuration.GetSection("EmailSender").Value == "File")
{
    builder.Services.AddTransient<IEmailSender, FileEmailSender>();
}
else
{
    builder.Services.AddTransient<IEmailSender, EmailSenderSendGrid>();
}

// Weekly Resource Guide feedback digest email
builder.Services.AddOptions<FeedbackDigestOptions>()
    .Bind(builder.Configuration.GetSection(FeedbackDigestOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(options => options.IsTimeZoneValid(), "FeedbackDigest:TimeZone is not a recognized time zone.")
    .ValidateOnStart();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<FeedbackDigestService>();
builder.Services.AddHostedService<FeedbackDigestBackgroundService>();

// Emails Get Help referrals to PC2
builder.Services.AddScoped<ReferralEmailService>();

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
    // Don't advertise the web server in a "Server: Kestrel" header
    options.AddServerHeader = false;
});

// Browsers only use HTTPS for the site for a year after each visit (the default is 30 days)
builder.Services.AddHsts(options => options.MaxAge = TimeSpan.FromDays(365));

var app = builder.Build();

// Standard browser security headers on every response, including static files and error pages
app.Use(async (context, next) =>
{
    IHeaderDictionary headers = context.Response.Headers;
    // Don't guess a file's type from its contents
    headers.XContentTypeOptions = "nosniff";
    // Only other pages on this site may show it in a frame
    headers.XFrameOptions = "SAMEORIGIN";
    // Other sites see only "pc2online.org" as the referrer, not the full address
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    // Features the site never uses, for its own pages and anything they embed
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=(), usb=()";
    await next();
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The max age is set by AddHsts above, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseResponseCompression();
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

// If a user tries to access the register page, redirect them to the login page.
// UnusedIdentityPagesConvention turns off the Identity UI Register page, so this is the only endpoint for the URL.
app.Map("/Identity/Account/Register", async context =>
{
    context.Response.Redirect("/Identity/Account/Login", true, true);
    await Task.CompletedTask;
});

app.Run();
