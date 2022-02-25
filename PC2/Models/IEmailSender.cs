using SendGrid;
using SendGrid.Helpers.Mail;

namespace IdentityLogin.Models
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string Name, string Email, string Phone, string Subject, string Message);
    }

    public class EmailSenderSendGrid : IEmailSender
    {
        private readonly IConfiguration _config;
        public EmailSenderSendGrid(IConfiguration config)
        {
            _config = config;
        }
        public async Task SendEmailAsync(string Name, string Email, string Phone, string Subject, string Message) // something coming in null
        {
            var apiKey = _config.GetSection("PC2SENDGRIDAPIKEY").Value;
            var PC2Email = _config.GetSection("PC2EMAIL").Value;
            var client = new SendGridClient(apiKey);
            var msg = new SendGridMessage()
            {
                From = new EmailAddress(PC2Email, "From: PC2 Team"),
                Subject = Subject,
                PlainTextContent = "From: " + Name + 
                "\nEmail: " + Email + 
                "\nPhone: " + Phone + ",\n\n" + 
                Message,
            };
            msg.AddTo(new EmailAddress(PC2Email, "PC2 Team"));
            var response = await client.SendEmailAsync(msg);

        }
    }
}