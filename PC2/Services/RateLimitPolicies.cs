using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace PC2.Services;

/// <summary>
/// Rate limits for the POSTs anyone can send: signing in, and the public forms that send email or save feedback.
/// Each visitor's IP address gets its own allowance. GETs are never limited, so the pages always load.
/// </summary>
/// <remarks>
/// Azure App Service passes the visitor's IP address in X-Forwarded-For, which ASP.NET Core applies to
/// RemoteIpAddress there. The limits are kept in memory, so each App Service instance counts separately.
/// </remarks>
public static class RateLimitPolicies
{
    /// <summary>
    /// Sign-in attempts. Account lockout stops guessing one account's password; this stops one visitor trying many accounts.
    /// </summary>
    public const string SignIn = "SignIn";

    /// <summary>
    /// The Get Help referral forms and Resource Guide feedback. Generous enough that someone fixing
    /// mistakes on a form never reaches it, but it stops a script from sending email after email.
    /// </summary>
    public const string PublicForms = "PublicForms";

    public const int SignInPermitLimit = 10;
    public static readonly TimeSpan SignInWindow = TimeSpan.FromMinutes(5);

    public const int PublicFormsPermitLimit = 10;
    public static readonly TimeSpan PublicFormsWindow = TimeSpan.FromMinutes(10);

    public const string RejectedMessage = "You've sent this form too many times in a short period. Please wait a few minutes and try again.";

    public static IServiceCollection AddRateLimitPolicies(this IServiceCollection services)
    {
        return services.AddRateLimiter(options =>
        {
            options.AddPolicy(SignIn, context => PostsPerIpAddress(context, SignInPermitLimit, SignInWindow));
            options.AddPolicy(PublicForms, context => PostsPerIpAddress(context, PublicFormsPermitLimit, PublicFormsWindow));
            options.OnRejected = WriteRejectionAsync;
        });
    }

    private static RateLimitPartition<string> PostsPerIpAddress(HttpContext context, int permitLimit, TimeSpan window)
    {
        if (!HttpMethods.IsPost(context.Request.Method))
        {
            return RateLimitPartition.GetNoLimiter(string.Empty);
        }

        string ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ipAddress, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = window,
            QueueLimit = 0
        });
    }

    /// <summary>
    /// Sends a 429 with a message the visitor can read: JSON for the feedback form's fetch, which shows
    /// "message", and plain text for a normal form post.
    /// </summary>
    private static async ValueTask WriteRejectionAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        HttpResponse response = context.HttpContext.Response;
        response.StatusCode = StatusCodes.Status429TooManyRequests;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
        {
            response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
        }

        string accept = context.HttpContext.Request.Headers.Accept.ToString();
        if (accept.Contains("text/html", StringComparison.OrdinalIgnoreCase))
        {
            response.ContentType = "text/plain; charset=utf-8";
            await response.WriteAsync(RejectedMessage, cancellationToken);
        }
        else
        {
            await response.WriteAsJsonAsync(new { message = RejectedMessage }, cancellationToken);
        }
    }
}
