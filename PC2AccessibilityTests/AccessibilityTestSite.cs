using Microsoft.Playwright;

namespace PC2AccessibilityTests;

/// <summary>
/// Shared setup for the accessibility tests. The tests scan a copy of the website that is
/// already running locally (start the PC2 project first), so they are skipped when the site can't be reached.
/// </summary>
[TestClass]
public static class AccessibilityTestSite
{
    /// <summary>
    /// Environment variable that overrides the address of the running website.
    /// </summary>
    public const string BaseUrlVariable = "PC2_A11Y_BASE_URL";

    /// <summary>
    /// Matches the https address in PC2/Properties/launchSettings.json
    /// </summary>
    private const string DefaultBaseUrl = "https://localhost:7057";

    // Default admin created by IdentityHelper.CreateDefaultAdmin in Debug builds. Override with environment variables if yours differs.
    private const string AdminEmailVariable = "PC2_A11Y_ADMIN_EMAIL";
    private const string AdminPasswordVariable = "PC2_A11Y_ADMIN_PASSWORD";
    private const string DefaultAdminEmail = "admin@pc2online.org";
    private const string DefaultAdminPassword = "Password01#";

    private static IPlaywright? _playwright;
    private static IBrowser? _browser;
    private static string? _skipReason;
    private static string? _adminStorageState;
    private static string? _adminLoginError;

    public static string BaseUrl { get; } =
        (Environment.GetEnvironmentVariable(BaseUrlVariable) ?? DefaultBaseUrl).TrimEnd('/');

    [AssemblyInitialize]
    public static async Task InitializeAsync(TestContext context)
    {
        if (!await IsSiteRunningAsync())
        {
            _skipReason = $"The PC2 website isn't running at {BaseUrl}. Start the PC2 project, then run the accessibility tests again. " +
                          $"Set the {BaseUrlVariable} environment variable if the site uses a different address.";
            return;
        }

        _playwright = await Playwright.CreateAsync();
        _browser = await LaunchBrowserAsync(_playwright);
        await LogInAsAdminAsync();
    }

    /// <summary>
    /// Uses Microsoft Edge or Google Chrome if one is installed, so nothing has to be downloaded.
    /// Otherwise downloads Playwright's copy of Chromium (only the first time).
    /// </summary>
    private static async Task<IBrowser> LaunchBrowserAsync(IPlaywright playwright)
    {
        foreach (string channel in new[] { "msedge", "chrome" })
        {
            try
            {
                return await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Channel = channel });
            }
            catch (PlaywrightException)
            {
                // Not installed, try the next browser
            }
        }

        int exitCode = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        if (exitCode != 0)
        {
            throw new InvalidOperationException(
                $"Edge and Chrome aren't installed, and Playwright could not download Chromium (exit code {exitCode}).");
        }
        return await playwright.Chromium.LaunchAsync();
    }

    [AssemblyCleanup]
    public static async Task CleanupAsync()
    {
        if (_browser is not null)
        {
            await _browser.CloseAsync();
        }
        _playwright?.Dispose();
    }

    /// <summary>
    /// Opens a new browser page for a visitor who isn't logged in.
    /// Skips the test (Inconclusive) when the website isn't running.
    /// </summary>
    public static async Task<IPage> NewPageAsync()
    {
        IBrowser browser = GetBrowserOrSkip();
        IBrowserContext context = await browser.NewContextAsync(CreateContextOptions());
        return await context.NewPageAsync();
    }

    /// <summary>
    /// Opens a new browser page logged in as the default admin.
    /// Skips the test (Inconclusive) when the website isn't running or the admin can't log in.
    /// </summary>
    public static async Task<IPage> NewAdminPageAsync()
    {
        IBrowser browser = GetBrowserOrSkip();
        if (_adminStorageState is null)
        {
            Assert.Inconclusive(_adminLoginError);
        }

        BrowserNewContextOptions options = CreateContextOptions();
        options.StorageState = _adminStorageState;
        IBrowserContext context = await browser.NewContextAsync(options);
        return await context.NewPageAsync();
    }

    private static IBrowser GetBrowserOrSkip()
    {
        if (_browser is null)
        {
            Assert.Inconclusive(_skipReason ?? "The browser was not started.");
        }
        return _browser;
    }

    private static BrowserNewContextOptions CreateContextOptions() => new()
    {
        BaseURL = BaseUrl,
        // The local development certificate may not be trusted on every machine
        IgnoreHTTPSErrors = true
    };

    private static async Task<bool> IsSiteRunningAsync()
    {
        using var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
        try
        {
            using HttpResponseMessage response = await client.GetAsync(BaseUrl);
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return false;
        }
    }

    private static async Task LogInAsAdminAsync()
    {
        string email = Environment.GetEnvironmentVariable(AdminEmailVariable) ?? DefaultAdminEmail;
        string password = Environment.GetEnvironmentVariable(AdminPasswordVariable) ?? DefaultAdminPassword;

        IBrowserContext context = await _browser!.NewContextAsync(CreateContextOptions());
        try
        {
            IPage page = await context.NewPageAsync();
            await page.GotoAsync("/Identity/Account/Login");
            await page.FillAsync("#Input_Email", email);
            await page.FillAsync("#Input_Password", password);
            await page.ClickAsync("#login-submit");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            if (page.Url.Contains("/Identity/Account/Login", StringComparison.OrdinalIgnoreCase))
            {
                _adminLoginError = $"Could not log in as {email}. Run the site in Debug so the default admin is created, " +
                                   $"or set the {AdminEmailVariable} and {AdminPasswordVariable} environment variables.";
                return;
            }

            _adminStorageState = await context.StorageStateAsync();
        }
        finally
        {
            await context.CloseAsync();
        }
    }
}
