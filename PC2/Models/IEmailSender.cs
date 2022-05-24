using SendGrid;
using SendGrid.Helpers.Mail;

namespace IdentityLogin.Models
{
    public interface IEmailSender
    {
        Task<Response> SendEmailAsync(string Name, string Email, string Phone, string Subject, string Message);
    }

    public class EmailSenderSendGrid : IEmailSender
    {
        private readonly IConfiguration _config;
        public EmailSenderSendGrid(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// Sends an information request to PC2 from users. Email will be sent to the PC2 information email,
        /// sent from a noreply address
        /// </summary>
        /// <param name="Name">The users name</param>
        /// <param name="Email">The users email address</param>
        /// <param name="Phone">The users phone number</param>
        /// <param name="Subject">The subject of the email</param>
        /// <param name="Message">The message of the email</param>
        public async Task<Response> SendEmailAsync(string Name, string Email, string Phone, string Subject, string Message)
        {
            string apiKey = _config.GetSection("PC2SendGridAPIKey").Value;
            string PC2Email = _config.GetSection("PC2Email").Value;
            string noReplyEmail = _config.GetSection("PC2NoReplyEmail").Value;
            SendGridClient client = new(apiKey);
            SendGridMessage msg = new()
            {
                From = new EmailAddress(noReplyEmail, "From: PC2 Website"),
                Subject = Subject,
                PlainTextContent = "From: " + Name + 
                "\nEmail: " + Email + 
                "\nPhone: " + Phone + ",\n\n" + 
                Message,
            };
            msg.AddTo(new EmailAddress(PC2Email, "PC2 Team"));
            return await client.SendEmailAsync(msg);
        }
    }
}