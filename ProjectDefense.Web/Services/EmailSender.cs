using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Threading.Tasks;

namespace ProjectDefense.Web.Services;

public class EmailSender : IEmailSender
{
    private readonly ILogger<EmailSender> _logger;
    private readonly IConfiguration _configuration;

    public EmailSender(ILogger<EmailSender> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var apiKey = _configuration["EmailSettings:SendGridApiKey"];
        
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_SENDGRID_API_KEY_HERE")
        {
            throw new InvalidOperationException("SendGrid API Key is not configured. Please check your appsettings.json.");
        }

        var client = new SendGridClient(apiKey);
        var fromEmail = _configuration["EmailSettings:SenderEmail"] ?? "admin@example.com";
        var fromName = _configuration["EmailSettings:SenderName"] ?? "Project Defense";
        var from = new EmailAddress(fromEmail, fromName);
        var to = new EmailAddress(email);
        var msg = MailHelper.CreateSingleEmail(from, to, subject, htmlMessage, htmlMessage);
        
        var response = await client.SendEmailAsync(msg);
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Email sent successfully to {Email}", email);
        }
        else
        {
            _logger.LogError("Failed to send email to {Email}. Status Code: {StatusCode}", email, response.StatusCode);
        }
    }
}
