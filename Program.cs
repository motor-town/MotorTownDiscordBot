using System.Globalization;
using System.Text.Json;
using Discord;
using Discord.WebSocket;
using MotorTown;
using Scriban;

public class Program
{
    private static DiscordSocketClient _client = new DiscordSocketClient();
    private static FileInfo? _file;
    private static long _lastMaxOffset = 0;
    private static AConfigurationClass _config = JsonSerializer.Deserialize<AConfigurationClass>(File.ReadAllText("config.json"))!;
    private static WebAPI? _webAPI;

    public static async Task Main(string[] args)
    {
        try
        {

            if (_config.WebApiConfig != null)
            {
                _webAPI = new WebAPI(_config.WebApiConfig.Port, _config.WebApiConfig.Password);
            }

            _client.Log += Log;
            WatchDirectory();
            await _client.LoginAsync(TokenType.Bot, _config.Token);
            await _client.StartAsync();
            UpdatePresence();
            await PushDiscord();
            return;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Console.WriteLine("Press any key to exit");
            Console.ReadLine();
            return;
        }

    }

    private static async void UpdatePresence()
    {

        if (_webAPI != null)
        {
            var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
            while (await timer.WaitForNextTickAsync())
            {
                int playerCount = await _webAPI.GetPlayerCount();
                await _client.SetActivityAsync(new Game("with " + playerCount + " other players"));
            }
        }
    }

    private static async Task PushDiscord()
    {
        DateTime now = DateTime.Now;
        await foreach (var line in ReadLinesAsync())
        {
            var gameEvent = ParseLog(line);
            if (gameEvent != null && gameEvent.TimeStamp >= now)
            {
                await SendEvent(gameEvent).ConfigureAwait(false);
            }
        }

        return;
    }

    private static void WatchDirectory()
    {
        string logFolderPath = _config.GetPath();
        var file = getLastLogFile(logFolderPath);
        if (file is not null)
        {
            ReadFile(file.FullName);
        }

        var watcher = new FileSystemWatcher(logFolderPath);
        watcher.Created += OnCreated;

        watcher.Filter = "*.log";
        watcher.EnableRaisingEvents = true;
        return;
    }

    private static async Task SendEvent(GameEvent gameEvent)
    {
        var messageParams = GetMessageParams(gameEvent);
        if (messageParams == null) return;

        var channel = await _client.GetChannelAsync(messageParams.ChannelId);
        if (channel is IMessageChannel textChannel)
        {
            await textChannel.SendMessageAsync(messageParams.Text, false, messageParams.Embed);
        }

        return;
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

        if (gameEvent is ChatMessage chatMessage)
            return messageConfig.ChatMessageConfig;

        if (gameEvent is BanEvent banEvent) return messageConfig.BanMessageConfig;

        if (gameEvent is SessionEvent sessionEvent)
        {
            return sessionEvent.Login ? messageConfig.LoginMessageConfig : messageConfig.LogoutMessageConfig;
        }

        return null;
    }

    public static GameEvent? ParseLog(string line)
    {
        string[] sections = line.Split(' ');

        DateTime dateTime = DateTime.ParseExact(sections.ElementAt(0)!, "[yyyy.MM.dd-HH.mm.ss]", CultureInfo.InvariantCulture);

        if (sections.ElementAt(1) == "[CHAT]")
        {
            string player = sections.ElementAt(2).TrimEnd(':');
            string message = string.Join(" ", sections.Skip(3)).TrimEnd('\n');
            return new ChatMessage(dateTime, player, message);
        }

        if (sections.ElementAt(1) == "Player"
            && sections.ElementAt(2) == "Login:")
        {
            return new SessionEvent(dateTime, sections.ElementAt(3), true);
        }


        if (sections.ElementAt(1) == "Player"
            && sections.ElementAt(2) == "Logout:")
        {
            return new SessionEvent(dateTime, sections.ElementAt(3), false);
        }

        if (sections.ElementAt(1) == "[ADMIN]")
        {
            return new BanEvent(dateTime, sections.ElementAt(4), sections.ElementAt(2));
        }

        return null;
    }

    private static Task Log(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }

    private static async IAsyncEnumerable<string> ReadLinesAsync()
    {
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        while (await timer.WaitForNextTickAsync())
        {
            if (_file is null)
            {
                continue;
            }

            Stream stream = _file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using (StreamReader reader = new StreamReader(stream))
            {
                //if the file size has not changed, idle

                if (reader.BaseStream.Length == _lastMaxOffset)
                    continue;

                //seek to the last max offset

                reader.BaseStream.Seek(_lastMaxOffset, SeekOrigin.Begin);

                //read out of the file until the EOF
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    yield return line.TrimEnd('\n');
                }

                //update the last max offset

                _lastMaxOffset = reader.BaseStream.Position;
            }
        }
    }

    private static FileInfo? getLastLogFile(string path)
    {
        DirectoryInfo d = new DirectoryInfo(path); //Assuming Test is your Folder

        FileInfo[] Files = d.GetFiles("*.log"); //Getting Text files

        Files.OrderBy(file => file.LastWriteTime);

        return Files.Last();
    }

    private static void OnCreated(object sender, FileSystemEventArgs e)
    {
        ReadFile(e.FullPath);
    }

    private static void ReadFile(string path)
    {
        _file = new FileInfo(path);
        _lastMaxOffset = _file.Length;
        Console.WriteLine($"Reading: {_file.FullName}");
    }

}

internal class MessageParams
{
    internal ulong ChannelId;
    internal string? Text;
    internal Embed? Embed;
}

public abstract class GameEvent
{
    protected GameEvent(DateTime dateTime, string player)
    {
        this.TimeStamp = dateTime;
        this.Player = player;
    }
    public DateTime TimeStamp { get; set; }
    public string Player { get; set; }

    public string FormatTemplate(string template)
    {
        return Template.Parse(template).Render(this);
    }
}
public class ChatMessage : GameEvent
{
    public ChatMessage(DateTime dateTime, string player, string Message) : base(dateTime, player)
    {
        this.Message = Message;
    }
    public string Message { get; set; }
}

public class SessionEvent : GameEvent
{
    public SessionEvent(DateTime dateTime, string player, bool Login) : base(dateTime, player)
    {
        this.Login = Login;
    }
    public bool Login { get; set; }
}
public class BanEvent : GameEvent
{
    public BanEvent(DateTime dateTime, string player, string admin) : base(dateTime, player)
    {
        this.Admin = admin;
    }

    public string Admin { get; set; }
}