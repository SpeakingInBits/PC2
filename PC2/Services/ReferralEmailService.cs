using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Reflection;
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
            ProfessionalReferral { IsConsentGiven: true } professional =>
                $"New professional referral: {FullName(professional.Parent)} (from {professional.OrganizationName})",
            ProfessionalReferral professional => $"New professional referral from {professional.OrganizationName}",
            SelfReferral self => $"New Get Help request: {FullName(GetContactPerson(self))} (for {DescribeReferringFor(self.ReferringFor)})",
            _ => "New Get Help request"
        };

        // Visitors type the names, so keep line breaks out of the email header
        return Regex.Replace(subject, @"\s+", " ").Trim();
    }

    /// <summary>
    /// The person PC2 should contact about a self-referral
    /// </summary>
    private static ReferralPerson GetContactPerson(SelfReferral referral) => referral.ReferringFor switch
    {
        ReferralChoices.MyChild => referral.Parent,
        ReferralChoices.FamilyMemberOrFriend => referral.Referrer,
        _ => referral.Self
    };

    private static string FullName(ReferralPerson person) => $"{person.FirstName} {person.LastName}";

    private static string DescribeReferringFor(string? referringFor) => referringFor switch
    {
        ReferralChoices.Myself => "themselves",
        ReferralChoices.MyChild => "their child",
        ReferralChoices.FamilyMemberOrFriend => "a family member or friend",
        _ => "unknown"
    };

    /// <summary>
    /// Lists the answers on the referral, grouped the same way as on the form. Sections hidden by the
    /// visitor's answers are left out.
    /// </summary>
    public static List<ReferralEmailSection> BuildSections(ReferralSubmission referral)
    {
        List<ReferralEmailSection> sections = new();

        if (referral is SelfReferral self)
        {
            sections.Add(new ReferralEmailSection("Referral",
            [
                Field(self, nameof(SelfReferral.ReferringFor)),
                Field(self, nameof(ReferralSubmission.Purpose)),
                Field(self, nameof(ReferralSubmission.BestTimesToContact)),
                Field(self, nameof(ReferralSubmission.HowHeard))
            ]));

            switch (self.ReferringFor)
            {
                case ReferralChoices.Myself:
                    sections.Add(PersonSection(self, SelfReferral.SelfSection,
                        Field(self, nameof(SelfReferral.HasChildWithDisability)),
                        Field(self, nameof(SelfReferral.CaresForAdult)),
                        Field(self, nameof(SelfReferral.PaidToCareForAdultChild))));
                    break;
                case ReferralChoices.MyChild:
                    sections.Add(PersonSection(self, ReferralSubmission.ParentSection, Field(self, nameof(SelfReferral.AgeRange))));
                    sections.Add(PersonSection(self, SelfReferral.ChildSection,
                        Field(self, nameof(SelfReferral.ChildIsAdultWithPaidCaregiver))));
                    break;
                case ReferralChoices.FamilyMemberOrFriend:
                    sections.Add(PersonSection(self, SelfReferral.ReferrerSection));
                    sections.Add(PersonSection(self, SelfReferral.FamilyMemberSection,
                        Field(self, nameof(SelfReferral.CaringWithoutPay))));
                    break;
            }

            sections.Add(new ReferralEmailSection("Their situation", [Field(self, nameof(SelfReferral.Situation))]));
        }
        else if (referral is ProfessionalReferral professional)
        {
            sections.Add(new ReferralEmailSection("Referral",
            [
                Field(professional, nameof(ReferralSubmission.Purpose)),
                Field(professional, nameof(ReferralSubmission.BestTimesToContact)),
                Field(professional, nameof(ReferralSubmission.HowHeard))
            ]));
            sections.Add(new ReferralEmailSection("Referred by",
            [
                Field(professional, nameof(ProfessionalReferral.OrganizationName)),
                new("Name", FullName(new ReferralPerson { FirstName = professional.FirstName, LastName = professional.LastName })),
                Field(professional, nameof(ProfessionalReferral.RoleOrRelationship)),
                Field(professional, nameof(ProfessionalReferral.Phone), ReferralEmailFieldType.Phone),
                Field(professional, nameof(ProfessionalReferral.Email), ReferralEmailFieldType.Email)
            ]));
            sections.Add(new ReferralEmailSection("About the family",
            [
                Field(professional, nameof(ProfessionalReferral.FamilyPrimaryLanguage)),
                Field(professional, nameof(ProfessionalReferral.NeedsLanguageSupport)),
                Field(professional, nameof(ProfessionalReferral.Diagnosis)),
                Field(professional, nameof(ProfessionalReferral.AgeRange)),
                Field(professional, nameof(ProfessionalReferral.AdditionalInformation)),
                Field(professional, nameof(ProfessionalReferral.HasConsent))
            ]));
            if (professional.IsConsentGiven)
            {
                sections.Add(PersonSection(professional, ReferralSubmission.ParentSection));
            }
        }

        // Leave out questions that weren't answered, and any section left empty
        return sections
            .Select(s => s with { Fields = s.Fields.Where(f => !string.IsNullOrWhiteSpace(f.Value)).ToList() })
            .Where(s => s.Fields.Count > 0)
            .ToList();
    }

    /// <summary>
    /// An answer labeled with the question's display name
    /// </summary>
    private static ReferralEmailField Field(ReferralSubmission referral, string propertyName,
        ReferralEmailFieldType type = ReferralEmailFieldType.Text)
    {
        PropertyInfo property = referral.GetType().GetProperty(propertyName)!;
        string label = property.GetCustomAttribute<DisplayAttribute>()?.Name ?? propertyName;
        return new ReferralEmailField(label, property.GetValue(referral) as string, type);
    }

    private static ReferralEmailSection PersonSection(ReferralSubmission referral, ReferralPersonSection section,
        params ReferralEmailField[] extraFields)
    {
        ReferralPerson person = referral.GetPerson(section);
        IEnumerable<ReferralEmailField> fields = section.Fields.Select(field => new ReferralEmailField(
            ReferralPerson.GetDisplayName(field),
            person.GetValue(field),
            field switch
            {
                nameof(ReferralPerson.MobileNumber) => ReferralEmailFieldType.Phone,
                nameof(ReferralPerson.Email) => ReferralEmailFieldType.Email,
                _ => ReferralEmailFieldType.Text
            }));
        return new ReferralEmailSection(section.Heading, fields.Concat(extraFields).ToList());
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
