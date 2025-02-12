using System.Diagnostics;
using System.Text.Json;
using Discord;
using Discord.WebSocket;
using MotorTownDiscordBot.MotorTown;

public class Program
{
    private static DiscordSocketClient _client = new DiscordSocketClient();
    private static AppConfig _config = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText("config.json"))!;
    private static MotorTown _motorTown = new MotorTown(_config.Path);

    public static async Task Main(string[] args)
    {
        try
        {
            _client.Log += Log;

            await _client.LoginAsync(TokenType.Bot, _config.Token);
            await _client.StartAsync();
            await Run();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed start: {e.Message}");
            Debug.WriteLine(e);
            Console.WriteLine("Press any key to exit");
            Console.ReadLine();
        }

    }

    private static async Task Run()
    {

        UpdatePresence();
        await foreach (var gameEvent in _motorTown.ReadAsync())
        {
            try
            {
                SendEvent(gameEvent);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to send event: {e.Message}");
                Debug.WriteLine(e);
            }
        };
    }


    private static async void UpdatePresence()
    {

        if (_motorTown.WebAPI != null)
        {
            var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
            while (await timer.WaitForNextTickAsync())
            {
                try
                {
                    int playerCount = await _motorTown.WebAPI.GetPlayerCount();
                    await _client.SetActivityAsync(new Game("with " + playerCount + " other players"));
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to update presence: {e.Message}");
                    Debug.WriteLine(e);
                }
            }
        }
    }

    private static async void SendEvent(GameEvent gameEvent)
    {
        var messageParams = GetMessageParams(gameEvent);
        if (messageParams == null) return;

        var channel = await _client.GetChannelAsync(messageParams.ChannelId);
        if (channel is not IMessageChannel textChannel) return;

        await textChannel.SendMessageAsync(messageParams.Text, false, messageParams.Embed);
    }

    private static MessageParams? GetMessageParams(GameEvent gameEvent)
    {
        MessageConfig? config = GetConfigByGameEvent(gameEvent);

        if (config is null) return null;

        MessageParams messageParams = new MessageParams();
        messageParams.ChannelId = config.ChannelId;

        if (config.TextFormat is not null)
        {
            messageParams.Text = gameEvent.FormatTemplate(config.TextFormat);
        }

        var embedConfig = config.EmbedConfig;
        if (embedConfig is not null)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            if (embedConfig.Color != null)
                embedBuilder.WithColor(Color.Parse(embedConfig.Color));

            string? titleFormat = embedConfig.TitleFormat;
            if (titleFormat != null)
                embedBuilder.WithTitle(gameEvent.FormatTemplate(titleFormat));


            string? descriptionFormat = embedConfig.DescriptionFormat;
            if (descriptionFormat != null)
                embedBuilder.WithDescription(gameEvent.FormatTemplate(descriptionFormat));


            string? thumbnailUrl = embedConfig.ThumbnailURL;
            if (thumbnailUrl != null) embedBuilder.WithThumbnailUrl(thumbnailUrl);

            messageParams.Embed = embedBuilder.Build();
        }

        return messageParams;
    }

    private static MessageConfig? GetConfigByGameEvent(GameEvent gameEvent)
    {
        var messageConfig = _config.MessagesConfig;

        if (messageConfig is null) return null;

        if (gameEvent is ChatMessageEvent chatMessage)
            return messageConfig.ChatMessageConfig;

        if (gameEvent is BanEvent banEvent) return messageConfig.BanMessageConfig;

        if (gameEvent is SessionEvent sessionEvent)
        {
            return sessionEvent.Login ? messageConfig.LoginMessageConfig : messageConfig.LogoutMessageConfig;
        }

        return null;
    }

    private static Task Log(LogMessage msg)
    {

        Console.WriteLine($"Failed to update presence: {msg.Message}");
        Debug.WriteLine(msg.ToString());

        return Task.CompletedTask;
    }
}

internal class MessageParams
{
    internal ulong ChannelId;
    internal string? Text;
    internal Embed? Embed;
}
