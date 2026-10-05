# PC2
PC2 is a modern, database-driven website for the Pierce County Coalition for Developmental Disabilities (PC2). 
It provides a searchable resource guide, file uploads, and administrative features, replacing the original Wix-based site the client had.

## Technology Stack
- ASP.NET Core MVC
- Azure Blob Storage (Azurite Emulator for local development)
- SQL Server (localdb for development)

## Production Environment
This website is hosted on Azure App Service, providing a reliable and scalable platform for production use. The database 
is hosted on Azure SQL Database.

## Getting Started

### Prerequisites
- Visual Studio 2026
- Ensure the SQL Server Data Tools component are installed with Visual Studio.
- .NET 10 SDK (bundled with Visual Studio 2026)
- ASP.NET and web development workload

### Setup Instructions
1. Clone the repository.
2. Open the solution in Visual Studio.
3. Run `update-database` in the Package Manager Console for the `PC2` project.
4. Execute `PC2-TestData.sql` (found in the Solution Items folder) against localdb. It adds agencies, calendar events, members,
   job opportunities, and Resource Guide feedback.
5. Run the website to create default roles and admin login.

### Azure Blob Storage
Azurite emulator is included as a dependency and runs automatically in Visual Studio. See [Azurite documentation](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite?tabs=visual-studio) for details.

### Google reCAPTCHA
Google reCAPTCHA v3 is used for spam protection on forms. To configure it for local development:
1. Create a development key at the [Google reCAPTCHA Admin Console](https://www.google.com/recaptcha/admin) using **reCAPTCHA v3** with `localhost` as the only allowed domain. Use this key for local development only.
2. Store your keys in [user secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) for the `PC2` project:
   ```
   dotnet user-secrets set "GoogleReCaptcha:SiteKey" "<your-site-key>"
   dotnet user-secrets set "GoogleReCaptcha:SecretKey" "<your-secret-key>"
   ```

Production keys are managed separately, so contributors only need a development key. If the keys aren't set, the app still runs, but reCAPTCHA verification reports as unavailable.

Visitors who score below `GoogleReCaptcha:MinimumScore` are shown a reCAPTCHA v2 **"I'm not a robot"** checkbox so they can prove they're
human instead of being turned away. The checkbox needs its own keys: create them in the Admin Console using **reCAPTCHA v2 > "I'm not a robot" Checkbox**
with the same domains, then add them alongside the v3 keys:
```
dotnet user-secrets set "GoogleReCaptcha:CheckboxSiteKey" "<your-checkbox-site-key>"
dotnet user-secrets set "GoogleReCaptcha:CheckboxSecretKey" "<your-checkbox-secret-key>"
```
Without the checkbox keys, visitors with low scores get an error as before. To try the checkbox locally, use Google's
[v2 test keys](https://developers.google.com/recaptcha/docs/faq#id-like-to-run-automated-tests-with-recaptcha.-what-should-i-do), which always pass,
and temporarily set `GoogleReCaptcha:MinimumScore` to `1.0` so every submission scores too low.

### Resource Guide Feedback Emails
Visitors can leave feedback after searching the Resource Guide. Admin and Staff can view it at `/Feedback` (linked from the Admin Dashboard).
New feedback is emailed to PC2 in a single weekly digest, sent by a background service (`FeedbackDigestBackgroundService`) using SendGrid.
Feedback that has been emailed or marked as reviewed on the website is never emailed again.

Settings are in the `FeedbackDigest` section of `appsettings.json`:
- `Enabled` - turns the automatic weekly email on or off. It is off in `appsettings.Development.json` so local runs don't send email.
- `Recipient` - who receives the email. Defaults to `PC2Email` when blank.
- `SendDay`, `SendHour`, `TimeZone` - when the email is sent (default: Sunday at 8 AM Pacific).
- `WebsiteUrl` - the public site URL, used to link to the feedback page from the email.

In production, enable **Always On** for the App Service so the background service keeps running. If the app is asleep at the scheduled time,
the email is sent the next time the app checks (every 15 minutes while running). Run a single instance; scaling out could send duplicate emails.

#### Testing emails in development
`appsettings.Development.json` sets `"EmailSender": "File"`, so in development emails are saved as HTML files in `PC2/DevEmails/`
(ignored by git) instead of being sent. Open a file in a browser to see the email. No SendGrid key is needed and nothing is delivered.

- **Send the digest manually:** sign in as an admin, go to the Admin Dashboard > Resource Guide Feedback, and click **Email New Feedback Now**.
- **Test the weekly schedule:** set `FeedbackDigest:Enabled` to `true` in `appsettings.Development.json` (or user secrets) and run the app.
  The seed data includes new feedback from over a week ago, so the digest is due and is saved within a few seconds of startup.
  Re-run the feedback section of `PC2-TestData.sql` after deleting all rows from `Feedback` and then `FeedbackDigests` to reset it.
- **Send real email from development:** set `EmailSender` to `SendGrid` in user secrets and configure the SendGrid settings.

### Get Help Referral Forms
For now, the only link to the forms is the **Get Help** button on the Contact Us page. It goes to `/GetHelp`, where visitors choose between asking for help for themselves or their family
(`/GetHelp/Self`) and a professional referral (`/GetHelp/Professional`). Submitted referrals are emailed to the `PC2Email` address and are
**not saved** in the database. Both forms are protected by reCAPTCHA. If Google can't be reached the referral is still sent, with a note in
the email that it wasn't checked for spam.

The questions follow the [Open Doors for Multicultural Families referral form](https://www.tfaforms.com/forms/view/4979848), which the
client chose as an example, until the client asks for changes. Some sections only appear after certain answers, e.g. the child's details
after choosing "My child". The answer choices are in `ReferralChoices`, and the questions in each section are set by the
`ReferralPersonSection`s, both in `Models/Referral.cs`.

In development, referral emails are saved to `PC2/DevEmails/` like other emails (see above).

### Mailing List Sign-up
PC2 manages its mailing list and email campaigns on [Sender.net](https://www.sender.net/). The sign-up form on the home page is designed
in the Sender.net dashboard, so its fields, wording, and colors are changed there rather than in this repository. Sign-ups go straight to
Sender.net and are not saved in our database.

Settings are in the `SenderNet` section of `appsettings.json`:
- `AccountId` - PC2's Sender.net account ID, from the JavaScript snippet Sender.net provides.
- `SignupFormId` - the sign-up form's `data-sender-form-id`.

Both are blank in `appsettings.Development.json`, so **local runs never load Sender.net** and test sign-ups can't reach PC2's real
mailing list. A dashed placeholder box shows where the form would appear. To try the real form locally, create a free Sender.net
account of your own, copy the form there, and put your IDs in user secrets. Don't use PC2's IDs:
```
dotnet user-secrets set "SenderNet:AccountId" "<your-account-id>"
dotnet user-secrets set "SenderNet:SignupFormId" "<your-form-id>"
```

`Views/Shared/_SenderNetScriptPartial.cshtml` loads Sender.net on every page, as Sender.net asks. To show the form on another page,
add `<partial name="_MailingListSignupPartial" />` to its view. The form won't appear if the browser can't reach `cdn.sender.net`
(e.g. an ad blocker).

### Accessibility Testing
The website should meet [WCAG 2.2 Level AA](https://www.w3.org/WAI/WCAG22/quickref/?levels=aaa). The `PC2AccessibilityTests` project scans each page with
[axe-core](https://github.com/dequelabs/axe-core) in a headless browser. It checks the public pages, the Resource Guide search results, and
the Admin/Staff pages (logged in as the default admin).

1. Run the website (F5 or `dotnet run --project PC2 --launch-profile PC2`).
2. Run the tests in **Test Explorer**, or from a second terminal:
   ```
   dotnet test PC2AccessibilityTests
   ```

Each failing test lists the problem elements, the WCAG rule, and a link explaining how to fix it. The tests use Microsoft Edge or Google Chrome
if installed, otherwise Playwright downloads Chromium the first time. If the site isn't running, the tests are skipped, so they don't affect CI.
Set `PC2_A11Y_BASE_URL` to scan a different address (default `https://localhost:7057`). To leave them out of a full test run, use
`dotnet test --filter TestCategory!=Accessibility`.

Automated tools find roughly a third of accessibility problems. When changing a page, also:
- Use the page with only the keyboard (Tab, Shift+Tab, Enter, Space, Esc). Every control should be reachable and show a visible focus outline.
- Run [Accessibility Insights for Web](https://accessibilityinsights.io/docs/web/overview/) **FastPass**, or Lighthouse in Chrome/Edge DevTools.
- Try the page with the free [NVDA](https://www.nvaccess.org/) screen reader.
- Zoom the browser to 200% and narrow the window to 320px wide. Content shouldn't be cut off or need sideways scrolling.

## Admin Credentials
- Username: `admin@pc2online.org`
- Password: `Password01#`

## Contributors
Made possible by [contributors](https://github.com/SpeakingInBits/PC2/graphs/contributors) and students at Clover Park Technical College.

<a href="https://github.com/speakinginbits/pc2/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=speakinginbits/pc2" />
</a>

The success of this website is a direct result of the dedication and hard work of CPTC students. By collaborating on a real client project, 
students gain valuable hands-on experience in software development, teamwork, and project delivery. Their contributions not only enhance 
their technical skills, but also provide the rewarding opportunity to see their work deployed in a live production environment, making a 
meaningful impact for the community.

Made with [contrib.rocks](https://contrib.rocks).
