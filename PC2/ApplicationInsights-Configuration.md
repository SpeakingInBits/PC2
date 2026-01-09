# Application Insights Configuration

## Local Development Setup

For local development, Application Insights works in **local mode** with sample analytics data.

### What Happens Locally:
- ? **Server-side telemetry** - Tracked locally in Visual Studio Output window
- ? **Client-side JavaScript tracking** - Events logged to browser console
- ? **Page view tracking** - Works locally with TelemetryClient
- ? **Custom events** - Tracked and visible in Visual Studio
- ? **Analytics Dashboard** - Shows sample data for development/testing

### How It Works:

1. **appsettings.Development.json** - Connection strings are empty:
   ```json
   {
     "APPLICATIONINSIGHTS_CONNECTION_STRING": "",
     "ApplicationInsights": {
       "WorkspaceId": "",
       "EnableLocalDevelopment": true,
       "DeveloperMode": true
     }
   }
   ```

2. **Program.cs** - Always configures Application Insights:
   - Registers TelemetryClient for dependency injection
   - Enables DeveloperMode for local Visual Studio output
   - Page view tracking works in all environments
   - Falls back gracefully if no connection string

3. **_Layout.cshtml** - Injects Configuration to check settings:
   - Loads real Application Insights JavaScript when configured
   - Provides mock implementation for pure local development
   - Mock logs events to browser console for debugging

4. **AnalyticsService** - Smart fallback logic:
   - Checks if Azure WorkspaceId and ConnectionString are configured
   - **Azure Mode**: Queries Log Analytics workspace for real data
   - **Local Mode**: Provides sample analytics data for testing UI
   - Automatically uses the right mode based on configuration

### Local Development Benefits:
- ?? Faster development (no Azure queries needed)
- ?? Easy debugging (telemetry in Visual Studio Output)
- ?? No Azure costs for development
- ?? Test Analytics UI with sample data
- ?? See how dashboard looks with realistic data

### Visual Studio Output:
Open **View ? Output** and select **"Debug"** to see:
```
Application Insights Telemetry:
PageView: /Home/Index
Event: ResourceGuideSearch - City: Tacoma
PageView: /Admin/Analytics
Providing sample analytics data for local development
```

### Analytics Dashboard in Local Mode:
When you visit `/Admin/Analytics` locally, you'll see:
- **Sample page views** (Home, Resources, Events, etc.)
- **Sample PDF downloads** (Recent newsletters)
- **Sample search terms** (Cities, services, agencies)
- All features work - you can test date filters, UI, etc.

## Production Setup

For production (Azure App Service):

1. **Azure Portal** - Configure these Application Settings:
   - `APPLICATIONINSIGHTS_CONNECTION_STRING` - Your App Insights connection string
   - `ApplicationInsights__WorkspaceId` - Your Log Analytics Workspace ID

2. **Automatic Override** - Azure App Settings automatically override appsettings.json values

3. **Full Telemetry & Analytics Enabled**:
   - Server-side telemetry sent to Azure
   - Client-side JavaScript tracking active
   - Page views, custom events, and exceptions tracked
   - Analytics dashboard queries real Azure data
   - No sample data - all metrics are live

## Testing with Real Azure Application Insights Locally (Optional)

If you want to test with actual Azure Application Insights data in local development:

1. **Get your credentials** from Azure Portal:
   - Application Insights Connection String
   - Log Analytics Workspace ID

2. **Use User Secrets** (recommended - keeps credentials out of source control):
   ```bash
   dotnet user-secrets set "APPLICATIONINSIGHTS_CONNECTION_STRING" "InstrumentationKey=xxx;IngestionEndpoint=https://xxx"
   dotnet user-secrets set "ApplicationInsights:WorkspaceId" "your-workspace-id-here"
   ```

3. **Or update appsettings.Development.json** (not recommended for shared repos):
   ```json
   {
     "APPLICATIONINSIGHTS_CONNECTION_STRING": "InstrumentationKey=xxx...",
     "ApplicationInsights": {
       "WorkspaceId": "your-workspace-id-here"
     }
   }
   ```

4. **Authenticate with Azure CLI**:
   ```bash
   az login
   ```

Now your local environment will query real Azure data!

?? **Security Note**: Never commit Azure credentials to source control. Always use User Secrets for local development.

## Analytics Dashboard Behavior

### Local Development (No Azure Config):
- Dashboard loads with sample data
- All UI features work (filters, date ranges, etc.)
- Data is static but realistic
- Perfect for UI development and testing

### Local Development (With Azure Config):
- Dashboard queries real Azure Application Insights
- Shows actual telemetry from your Azure environment
- Requires Azure CLI authentication (`az login`)
- Great for debugging and validating queries

### Production (Azure Deployment):
- Dashboard queries production Azure Application Insights
- Uses Managed Identity for authentication (no credentials needed)
- Shows real user behavior and metrics
- Powers business intelligence and decision making

## Troubleshooting

### "No data found" in Analytics Dashboard
**Local Mode**: This is normal - means you're using sample data mode. Check the message banner.
**Azure Mode**: 
- Verify WorkspaceId is correct
- Ensure you're authenticated (`az login`)
- Check if there's data in your Application Insights workspace

### TelemetryClient not found error
- Ensure `AddApplicationInsightsTelemetry()` is called in Program.cs
- Should be registered even without connection string
- Check that application is building successfully

### Browser console shows "AI Event (Local)"
- This means client-side is in mock mode
- Add connection string to enable real Azure tracking
- Or leave as-is for pure local development
