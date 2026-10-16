using System.Threading.Tasks;

namespace SecureFileManager.Infrastructure.Services
{
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string body);
    }

    // Development fallback: writes messages to disk under AppStorage/Emails
    public class FileEmailService : IEmailService
    {
        private readonly IConfiguration _config;
        public FileEmailService(IConfiguration config) { _config = config; }

        public Task SendAsync(string to, string subject, string body)
        {
            var root = _config.GetValue<string>("Storage:RootPath") ?? "./AppStorage";
            var dir = Path.Combine(root, "Emails");
            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, $"email_{DateTime.UtcNow:yyyyMMddHHmmssfff}.txt");
            var content = $"To: {to}\nSubject: {subject}\n\n{body}";
            return File.WriteAllTextAsync(file, content);
        }
    }

    // SMTP implementation (reads SMTP settings from config)
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _config;
        public SmtpEmailService(IConfiguration config) { _config = config; }

        public async Task SendAsync(string to, string subject, string body)
        {
            var host = _config.GetValue<string>("Smtp:Host");
            var port = _config.GetValue<int?>("Smtp:Port") ?? 587;
            var user = _config.GetValue<string>("Smtp:Username");
            var pass = _config.GetValue<string>("Smtp:Password");
            var from = _config.GetValue<string>("Smtp:From") ?? user;

            using var client = new System.Net.Mail.SmtpClient(host, port)
            {
                EnableSsl = true,
                Credentials = new System.Net.NetworkCredential(user, pass)
            };
            var msg = new System.Net.Mail.MailMessage(from, to, subject, body);
            await client.SendMailAsync(msg);
        }
    }
}
