using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using _4Bet.Application.IServices;

namespace _4Bet.Application.Services;

public class EmailService(IConfiguration configuration, ILogger<EmailService> logger) : IEmailService
{
    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            var emailMessage = new MimeMessage();
            
            var senderName = configuration["EmailSettings:SenderName"];
            var senderEmail = configuration["EmailSettings:SenderEmail"];
            
            emailMessage.From.Add(new MailboxAddress(senderName, senderEmail));
            emailMessage.To.Add(new MailboxAddress("", toEmail));
            emailMessage.Subject = subject;
            emailMessage.Body = new TextPart(TextFormat.Html) { Text = body };

            using var client = new SmtpClient();
            
            var smtpServer = configuration["EmailSettings:SmtpServer"];
            var smtpPort = int.Parse(configuration["EmailSettings:SmtpPort"]!);
            var appPassword = configuration["EmailSettings:AppPassword"];

            await client.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(senderEmail, appPassword);
            
            await client.SendAsync(emailMessage);
            await client.DisconnectAsync(true);
            
            logger.LogInformation("Email sent successfully to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            throw; 
        }
    }
}