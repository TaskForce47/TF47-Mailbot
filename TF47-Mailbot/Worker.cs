using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoreHtmlToImage;
using Discord;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TF47_Mailbot.Services;

namespace TF47_Mailbot
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly MailService _mailService;
        private readonly BotService _botService;

        public Worker(
            ILogger<Worker> logger, 
            MailService mailService,
            BotService botService)
        {
            _logger = logger;
            _mailService = mailService;
            _botService = botService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _botService.Initialize();
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                
                var unreadMails = await _mailService.GetUnreadMails();
                if (unreadMails != null)
                {
                    _logger.LogInformation($"{unreadMails.Count} unread mails, sending!");
                    foreach (var unreadMail in unreadMails)
                    { 
                        var embed = new EmbedBuilder
                        {
                            Title = unreadMail.Subject, Color = Color.Green
                        };
                        embed.AddField("From", unreadMail.FromMail);
                        if (! unreadMail.IsHtml)
                            embed.AddField("Message", unreadMail.Message);
                        embed.Footer =
                            new EmbedFooterBuilder().WithText($"Received at {unreadMail.InboxTime:f}");
                        
                        if (unreadMail.IsHtml)
                        {
                            var converter = new HtmlConverter();
                            var bytes = converter.FromHtmlString(unreadMail.Message);
                            Stream stream = new MemoryStream(bytes);
                            embed.ImageUrl = $"attachment://html.jpeg";
                            await _botService.UploadImage(stream, "html.jpeg", embed.Build());
                        } 
                        else 
                            await _botService.SendEmbed(embed.Build());
                    }
                }
                await Task.Delay(1000*30, stoppingToken);
            }
        }
    }
}