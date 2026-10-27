using MailKit.Net.Smtp;
using MimeKit;
using MyChat.BLL.Interfaces;
using System.Threading.Tasks;
namespace MyChat.BLL.Services;

public class EmailService : IEmailService
{
    private readonly IAccountService _accountService;
    private readonly string appPassword = "";
    
    public  EmailService(IAccountService accountService)
    {
        _accountService = accountService;
    }

    public async Task SendEmailAsync(string email, string subject, string text)
    {
        var emailMessage = new MimeMessage();
        emailMessage.From.Add(new MailboxAddress("Kurmanjan", "Kurmanjanurankyzy@gmail.com"));
        emailMessage.To.Add(new MailboxAddress("",email));
        emailMessage.Subject = subject;

        emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Text)
            { Text = text };
        using (var client= new MailKit.Net.Smtp.SmtpClient())
        {
            await client.ConnectAsync("Smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync("Kurmanjanurankyzy@gmail.com", "tdat rjbw lmvo fvpt");
            await client.SendAsync(emailMessage);
            await client.DisconnectAsync(true);
        }
    }
}