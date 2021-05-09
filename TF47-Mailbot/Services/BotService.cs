using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TF47_Mailbot.Services
{
    public class BotService
    {
        private readonly ILogger<BotService> _logger;
        private readonly IConfiguration _configuration;
        private DiscordSocketClient _client;
        private SocketChannel _channel;
        private bool _isReady;

        public BotService(
            ILogger<BotService> logger, 
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task Initialize()
        {
            _client = new DiscordSocketClient();
            _client.Log += message =>
            {
                _logger.LogInformation(message.Message);
                return Task.CompletedTask;
            };
            _client.Ready += () =>
            {
                _logger.LogInformation("Client ready");
                _isReady = true;
                return Task.CompletedTask;
            };

            await _client.LoginAsync(TokenType.Bot, _configuration["Discord:Token"]);
            await _client.StartAsync();

            await Task.Run(() => 
            {
                while (!_isReady)
                {
                    Task.Delay(100);
                }
            });
            
            await _client.SetActivityAsync(new Game("Listening to awesome mails (but mostly bills) ..."));

            var channelId = Convert.ToUInt64(_configuration["Discord:ChannelId"]);

            foreach (var clientGroupChannel in _client.GroupChannels)
            {
                _logger.LogInformation($"{clientGroupChannel.Name} {clientGroupChannel.Id}");
            }
            
            _channel = _client.GetChannel(channelId);
        }

        public async Task SendMessage(string message)
        {
            var typingState = ((IMessageChannel)_channel).EnterTypingState();
            await ((IMessageChannel)_channel).SendMessageAsync(message);
            typingState.Dispose();
        }

        public async Task SendEmbed(Embed embed)
        {
            var typingState = ((IMessageChannel)_channel).EnterTypingState();
            await ((IMessageChannel)_channel).SendMessageAsync("",false, embed);
            typingState.Dispose();
        }

        public async Task UploadImage(Stream stream, string fileName, Embed embed)
        {
            var typingState = ((IMessageChannel)_channel).EnterTypingState();
            await ((IMessageChannel) _channel).SendFileAsync(stream, fileName, embed: embed);
            typingState.Dispose();
        }
        
    }
}