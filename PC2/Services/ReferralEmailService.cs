using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using IdentityLogin.Models;
using Microsoft.Extensions.Options;
using PC2.Models;

namespace PC2.Services;

/// <summary>
/// A labeled answer shown in a referral email. Phone numbers and email addresses are shown as links.
/// </summary>
public record ReferralEmailField(string Label, string? Value, ReferralEmailFieldType Type = ReferralEmailFieldType.Text);

public enum ReferralEmailFieldType
{
    Text,
    Phone,
    Email
}

public record ReferralEmailSection(string Heading, IReadOnlyList<ReferralEmailField> Fields);

/// <summary>
/// Emails referrals submitted from the Get Help forms to PC2.
/// </summary>
public class ReferralEmailService
{
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _config;
    private readonly TimeZoneInfo _timeZone;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ReferralEmailService> _logger;

    public ReferralEmailService(IEmailSender emailSender, IConfiguration config, IOptions<FeedbackDigestOptions> siteOptions,
        TimeProvider timeProvider, ILogger<ReferralEmailService> logger)
    {
        _emailSender = emailSender;
        _config = config;
        _timeZone = siteOptions.Value.GetTimeZoneInfo();
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <summary>
    /// Emails the referral to the PC2Email address
    /// </summary>
    /// <param name="referral">The referral to send</param>
    /// <param name="isSpamCheckSkipped">True when reCAPTCHA could not verify the submission, so PC2 knows it wasn't checked for spam</param>
    /// <returns>True if the email was sent</returns>
    public async Task<bool> SendAsync(ReferralSubmission referral, bool isSpamCheckSkipped, CancellationToken cancellationToken = default)
    {
        string? recipient = _config.GetSection("PC2Email").Value;
        if (string.IsNullOrWhiteSpace(recipient))
        {
            _logger.LogError("Referral email not sent: PC2Email is not configured.");
            return false;
        }

        DateTime submittedAt = FeedbackDigestService.ToLocal(_timeProvider.GetUtcNow().UtcDateTime, _timeZone);
        List<ReferralEmailSection> sections = BuildSections(referral);
        string subject = BuildSubject(referral);

        try
        {
            SendGrid.Response response = await _emailSender.SendHtmlEmailAsync(recipient, subject,
                BuildPlainText(referral, sections, submittedAt, isSpamCheckSkipped),
                BuildHtml(referral, sections, submittedAt, isSpamCheckSkipped));
            if (!response.IsSuccessStatusCode)
            {
                string body = response.Body == null ? "" : await response.Body.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Referral email failed with HTTP status {StatusCode}: {Body}", response.StatusCode, body);
                return false;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while sending a referral email.");
            return false;
        }

        _logger.LogInformation("{ReferralType} sent to {Recipient}.", GetReferralTypeName(referral), recipient);
        return true;
    }

    public static string GetReferralTypeName(ReferralSubmission referral) => referral switch
    {
        ProfessionalReferral => "Professional referral",
        _ => "Self-referral"
    };

    public static string BuildSubject(ReferralSubmission referral)
    {
        string subject = referral switch
        {
            ProfessionalReferral professional =>
                $"New professional referral: {professional.ContactName} (from {professional.OrganizationName})",
            SelfReferral self => $"New Get Help request: {self.FirstName} {self.LastName} (for {DescribeReferringFor(self.ReferringFor)})",
            _ => "New Get Help request"
        };

        // Visitors type the names, so keep line breaks out of the email header
        return Regex.Replace(subject, @"\s+", " ").Trim();
    }

    private static string DescribeReferringFor(string? referringFor) => referringFor switch
    {
        "Myself" => "themselves",
        "My child" => "their child",
        "A family member or friend" => "a family member or friend",
        _ => "unknown"
    };

    /// <summary>
    /// Lists the answers on the referral, grouped the same way as on the form
    /// </summary>
    public static List<ReferralEmailSection> BuildSections(ReferralSubmission referral)
    {
        List<ReferralEmailSection> sections = new();

        if (referral is SelfReferral self)
        {
            sections.Add(new ReferralEmailSection("Contact information",
            [
                new("Who needs help", self.ReferringFor),
                new("Name", $"{self.FirstName} {self.LastName}"),
                new("Phone", self.Phone, ReferralEmailFieldType.Phone),
                new("Email", self.Email, ReferralEmailFieldType.Email),
                new("Best way to contact", self.PreferredContactMethod),
                new("Best days and times", referral.BestTimesToContact),
                new("ZIP code", self.ZipCode)
            ]));
            sections.Add(new ReferralEmailSection("About the person who needs help", PersonFields(referral, self.PersonName)));
        }
        else if (referral is ProfessionalReferral professional)
        {
            sections.Add(new ReferralEmailSection("Person or family to contact",
            [
                new("Name", professional.ContactName),
                new("Phone", professional.ContactPhone, ReferralEmailFieldType.Phone),
                new("Email", professional.ContactEmail, ReferralEmailFieldType.Email),
                new("Best way to contact", professional.ContactPreferredMethod),
                new("Best days and times", referral.BestTimesToContact),
                new("ZIP code", professional.ZipCode)
            ]));
            sections.Add(new ReferralEmailSection("About the person with a disability", PersonFields(referral, professional.PersonName)));
            sections.Add(new ReferralEmailSection("Referred by",
            [
                new("Name", $"{professional.FirstName} {professional.LastName}"),
                new("Organization", professional.OrganizationName),
                new("Role or relationship", professional.RoleOrRelationship),
                new("Phone", professional.Phone, ReferralEmailFieldType.Phone),
                new("Email", professional.Email, ReferralEmailFieldType.Email),
                new("Family agreed to the referral", professional.HasConsent ? "Yes" : "No")
            ]));
        }

        sections.Add(new ReferralEmailSection("Other",
        [
            new("How they heard about PC2", referral.HowHeard),
            new("Additional information", referral.AdditionalInformation)
        ]));

        // Leave out questions that weren't answered, and any section left empty
        return sections
            .Select(s => s with { Fields = s.Fields.Where(f => !string.IsNullOrWhiteSpace(f.Value)).ToList() })
            .Where(s => s.Fields.Count > 0)
            .ToList();
    }

    private static List<ReferralEmailField> PersonFields(ReferralSubmission referral, string? personName)
    {
        List<string> purposes = ReferralChoices.Purposes
            .Where(p => p != ReferralChoices.OtherPurpose && referral.Purposes.Contains(p))
            .ToList();
        if (referral.Purposes.Contains(ReferralChoices.OtherPurpose))
        {
            purposes.Add($"Other: {referral.OtherPurpose?.Trim()}");
        }

        return
        [
            new("Name", personName),
            new("Help needed", string.Join("\n", purposes)),
            new("Age", referral.AgeRange),
            new("Has a diagnosis", referral.Diagnosis),
            new("Primary language", referral.PrimaryLanguage),
            new("Interpreter needed", referral.NeedsInterpreter)
        ];
    }

    public static string BuildPlainText(ReferralSubmission referral, List<ReferralEmailSection> sections, DateTime localSubmittedAt,
        bool isSpamCheckSkipped)
    {
        StringBuilder text = new();
        text.AppendLine($"{GetReferralTypeName(referral)} submitted from the Get Help form on the PC2 website on {FormatDate(localSubmittedAt)}.");
        if (isSpamCheckSkipped)
        {
            text.AppendLine(SpamCheckSkippedText);
        }

        foreach (ReferralEmailSection section in sections)
        {
            text.AppendLine();
            text.AppendLine(section.Heading.ToUpperInvariant());
            foreach (ReferralEmailField field in section.Fields)
            {
                // Indent continuation lines so multi-line answers stay under their label
                text.AppendLine($"{field.Label}: {field.Value!.Trim().ReplaceLineEndings(Environment.NewLine + "    ")}");
            }
        }

        return text.ToString();
    }

    public static string BuildHtml(ReferralSubmission referral, List<ReferralEmailSection> sections, DateTime localSubmittedAt,
        bool isSpamCheckSkipped)
    {
        const string green = "#5b7623";

        StringBuilder html = new();
        html.Append("<div style=\"font-family: Arial, Helvetica, sans-serif; color: #222; max-width: 640px; margin: 0 auto;\">");
        html.Append($"<h2 style=\"color: {green}; margin-bottom: 4px;\">{Encode(GetReferralTypeName(referral))}</h2>");
        html.Append($"<p style=\"margin-top: 0; color: #555;\">Submitted from the Get Help form on the PC2 website on {Encode(FormatDate(localSubmittedAt))}.</p>");
        if (isSpamCheckSkipped)
        {
            html.Append($"<p style=\"border-left: 4px solid #b58105; background: #fff8e1; padding: 8px 12px;\">{Encode(SpamCheckSkippedText)}</p>");
        }

        foreach (ReferralEmailSection section in sections)
        {
            html.Append($"<h3 style=\"border-bottom: 1px solid #ddd; padding-bottom: 4px; margin-top: 24px;\">{Encode(section.Heading)}</h3>");
            html.Append("<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" style=\"border-collapse: collapse; width: 100%;\">");
            foreach (ReferralEmailField field in section.Fields)
            {
                html.Append("<tr>");
                html.Append($"<td style=\"padding: 4px 16px 4px 0; vertical-align: top; color: #555; width: 35%;\">{Encode(field.Label)}</td>");
                html.Append($"<td style=\"padding: 4px 0; vertical-align: top; white-space: pre-line;\">{FormatHtmlValue(field)}</td>");
                html.Append("</tr>");
            }
            html.Append("</table>");
        }

        html.Append("<p style=\"color: #777; font-size: 12px; margin-top: 24px;\">This email was sent automatically by the PC2 website. " +
                    "Referrals are not saved on the website, so keep this email until the referral has been followed up.</p>");
        html.Append("</div>");
        return html.ToString();
    }

    private const string SpamCheckSkippedText =
        "Note: the website could not check this submission for spam because reCAPTCHA was unavailable.";

    private static string FormatDate(DateTime localDate) => localDate.ToString("dddd, MMMM d, yyyy 'at' h:mm tt");

    private static string FormatHtmlValue(ReferralEmailField field)
    {
        string value = field.Value!.Trim();
        return field.Type switch
        {
            ReferralEmailFieldType.Email => $"<a href=\"mailto:{Encode(value)}\">{Encode(value)}</a>",
            ReferralEmailFieldType.Phone => $"<a href=\"tel:{Encode(Regex.Replace(value, @"[^\d+]", ""))}\">{Encode(value)}</a>",
            _ => Encode(value)
        };
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
