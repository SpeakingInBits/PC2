using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using SendGrid;

namespace IdentityLogin.Models
{
    /// <summary>
    /// Saves emails as HTML files instead of sending them, so email features can be tested in development
    /// without SendGrid or sending real email. Enabled by setting "EmailSender" to "File" in configuration.
    /// </summary>
    public class FileEmailSender : IEmailSender
    {
        private readonly IConfiguration _config;
        private readonly ILogger<FileEmailSender> _logger;

        /// <summary>
        /// The folder emails are saved to. Set with "FileEmailDirectory", defaults to a DevEmails folder in the project.
        /// </summary>
        public string Directory { get; }

        public FileEmailSender(IConfiguration config, IHostEnvironment environment, ILogger<FileEmailSender> logger)
        {
            _config = config;
            _logger = logger;
            Directory = config.GetSection("FileEmailDirectory").Value is { Length: > 0 } directory
                ? directory
                : Path.Combine(environment.ContentRootPath, "DevEmails");
        }

        public async Task<Response> SendEmailAsync(string Name, string Email, string Phone, string Subject, string Message)
        {
            string plainText = "From: " + Name +
                "\nEmail: " + Email +
                "\nPhone: " + Phone + ",\n\n" +
                Message;
            return await SaveAsync(_config.GetSection("PC2Email").Value ?? "", Subject, plainText, null);
        }

        public async Task<Response> SendHtmlEmailAsync(string toEmail, string subject, string plainTextContent, string htmlContent)
        {
            return await SaveAsync(toEmail, subject, plainTextContent, htmlContent);
        }

        private async Task<Response> SaveAsync(string toEmail, string subject, string plainTextContent, string? htmlContent)
        {
            System.IO.Directory.CreateDirectory(Directory);

            string slug = Regex.Replace(subject, "[^A-Za-z0-9]+", "-").Trim('-');
            if (slug.Length > 60)
            {
                slug = slug[..60];
            }
            string filePath = Path.Combine(Directory, $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}_{slug}.html");

            StringBuilder file = new();
            file.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\">");
            file.Append($"<title>{Encode(subject)}</title></head><body style=\"background: #fff;\">");
            file.Append("<table style=\"font-family: monospace; background: #eef; padding: 8px; margin-bottom: 16px; width: 100%;\">");
            file.Append($"<tr><td><strong>From:</strong></td><td>{Encode(_config.GetSection("PC2NoReplyEmail").Value ?? "")}</td></tr>");
            file.Append($"<tr><td><strong>To:</strong></td><td>{Encode(toEmail)}</td></tr>");
            file.Append($"<tr><td><strong>Subject:</strong></td><td>{Encode(subject)}</td></tr>");
            file.Append($"<tr><td><strong>Saved:</strong></td><td>{DateTime.Now:F}</td></tr>");
            file.Append("</table>");
            if (htmlContent != null)
            {
                file.Append(htmlContent);
                file.Append("<details style=\"margin-top: 24px;\"><summary>Plain text version</summary>");
            }
            file.Append($"<pre style=\"white-space: pre-wrap;\">{Encode(plainTextContent)}</pre>");
            if (htmlContent != null)
            {
                file.Append("</details>");
            }
            file.Append("</body></html>");

            await File.WriteAllTextAsync(filePath, file.ToString());
            _logger.LogInformation("Email \"{Subject}\" to {To} was saved to {FilePath} instead of being sent.", subject, toEmail, filePath);

            return new Response(HttpStatusCode.Accepted, null, null);
        }

        private static string Encode(string value) => WebUtility.HtmlEncode(value);
    }
}
