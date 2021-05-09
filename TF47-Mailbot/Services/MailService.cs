using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace TF47_Mailbot.Services
{
    public class MailService
    {
        private readonly ILogger<MailService> _logger;
        private readonly IConfiguration _configuration;

        public MailService(
            ILogger<MailService> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<List<MessageResponse>> GetUnreadMails()
        {
            var client = new ImapClient();
            try
            {
                await client.ConnectAsync(_configuration["Mail:Server"], int.Parse(_configuration["Mail:Port"]),
                    SecureSocketOptions.Auto);
                await client.AuthenticateAsync(_configuration["Mail:Username"], _configuration["Mail:Password"]);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to connect to IMAP Server: ", ex.Message);
                return null;
            }

            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadWrite);

            var notSeen = await inbox.SearchAsync(SearchQuery.NotSeen);

            var response = new List<MessageResponse>();
            foreach (var uniqueId in notSeen)
            {
                await inbox.SetFlagsAsync(uniqueId, MessageFlags.Seen, true);
                var message = await inbox.GetMessageAsync(uniqueId);
                if (string.IsNullOrEmpty(message.TextBody) && !string.IsNullOrEmpty(message.HtmlBody))
                {
                    response.Add(new MessageResponse(message.From.ToString(), message.Date.DateTime, message.Subject,
                        message.HtmlBody, true));
                    continue;
                }
                if (message.TextBody.Length > 1024)
                    response.Add(new MessageResponse(message.From.ToString(), message.Date.DateTime, message.Subject,
                        message.TextBody, true));
                else
                    response.Add(new MessageResponse(message.From.ToString(), message.Date.DateTime, message.Subject,
                        message.TextBody, false));
            }

            return response;
        }

        public record MessageResponse(string FromMail, DateTime InboxTime, string Subject, string Message, bool IsHtml);
    }
}