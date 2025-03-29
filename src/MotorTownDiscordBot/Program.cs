using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using MotorTownDiscordBot;
using MotorTownDiscordBot.MotorTown;

public class Program
{
    private static readonly DiscordSocketClient _client = new DiscordSocketClient();
    private static readonly AppConfig _config = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText("config.json"))!;
    private static readonly MotorTown _motorTown = new MotorTown(_config.Path);

    public static async Task Main(string[] args)
    {
        try
        {
            _client.Log += Log;
            _client.Ready += Ready;

            await _client.LoginAsync(TokenType.Bot, _config.Token);
            await _client.StartAsync();

            await Run();
        }
        catch (Exception e)
        {
            LogError("Failed to start bot", e);
        }
    }

    private static async Task Ready()
    {
        if (_motorTown.WebAPI != null)
        {
            var botInteractions = new BotInteraction(_client, _motorTown.WebAPI);
            await botInteractions.RegisterCommands();
            Console.WriteLine("Bot commands registered");
        }
    }

    private static async Task Run()
    {
        UpdatePresence();

        await foreach (var gameEvent in _motorTown.ReadAsync())
        {
            try
            {
                await SendEvent(gameEvent);
            }
            catch (Exception e)
            {
                LogError("Failed to send event", e);
            }
        }
    }

    private static async void UpdatePresence()
    {
        if (_motorTown.WebAPI == null) return;

        var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        while (await timer.WaitForNextTickAsync())
        {
            try
            {
                int playerCount = await _motorTown.WebAPI.GetPlayerCount();
                await _client.SetActivityAsync(new Game($"with {playerCount} other players"));
            }
            catch (Exception e)
            {
                LogError("Failed to update presence", e);
            }
        }
    }

    private static async Task SendEvent(GameEvent gameEvent)
    {
        var messageParams = GetMessageParams(gameEvent);
        if (messageParams == null) return;

        if (await _client.GetChannelAsync(messageParams.ChannelId) is not IMessageChannel textChannel) return;

        await textChannel.SendMessageAsync(messageParams.Text, false, messageParams.Embed);
    }

    private static MessageParams? GetMessageParams(GameEvent gameEvent)
    {
        var messageConfig = GetConfigByGameEvent(gameEvent);
        if (messageConfig == null) return null;

        var messageParams = new MessageParams
        {
            ChannelId = messageConfig.ChannelId,
            Text = FormatMessageText(gameEvent, messageConfig.TextFormat),
            Embed = CreateEmbed(gameEvent, messageConfig.EmbedConfig)
        };

        return messageParams;
    }

    private static string? FormatMessageText(GameEvent gameEvent, string? textFormat)
    {
        if (textFormat == null) return null;

        var text = gameEvent.FormatTemplate(textFormat);
        var mentionConfig = _config.AdminMentionConfig;

        if (mentionConfig?.Keyword != null && mentionConfig?.DiscordID != null)
        {
            text = Regex.Replace(text, mentionConfig.Keyword, mentionConfig.DiscordID, RegexOptions.IgnoreCase);
        }

        return text;
    }

    private static Embed? CreateEmbed(GameEvent gameEvent, EmbedConfig? embedConfig)
    {
        if (embedConfig == null) return null;

        var embedBuilder = new EmbedBuilder();

        if (embedConfig.Color != null)
            embedBuilder.WithColor(Color.Parse(embedConfig.Color));

        if (embedConfig.TitleFormat != null)
            embedBuilder.WithTitle(gameEvent.FormatTemplate(embedConfig.TitleFormat));

        if (embedConfig.DescriptionFormat != null)
            embedBuilder.WithDescription(gameEvent.FormatTemplate(embedConfig.DescriptionFormat));

        if (embedConfig.ThumbnailURL != null)
            embedBuilder.WithThumbnailUrl(embedConfig.ThumbnailURL);

        return embedBuilder.Build();
    }

    private static MessageConfig? GetConfigByGameEvent(GameEvent gameEvent)
    {
        return gameEvent switch
        {
            ChatMessageEvent => _config.MessagesConfig?.ChatMessageConfig,
            BanEvent => _config.MessagesConfig?.BanMessageConfig,
            SessionEvent sessionEvent => sessionEvent.Login
                ? _config.MessagesConfig?.LoginMessageConfig
                : _config.MessagesConfig?.LogoutMessageConfig,
            _ => null
        };
    }

    private static Task Log(LogMessage msg)
    {
        Console.WriteLine($"Discord: {msg.Message}");
        Debug.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }

    private static void LogError(string message, Exception e)
    {
        Console.WriteLine($"{message}: {e.Message}");
        Debug.WriteLine(e);
    }
}

internal class MessageParams
{
    internal ulong ChannelId;
    internal string? Text;
    internal Embed? Embed;
}
