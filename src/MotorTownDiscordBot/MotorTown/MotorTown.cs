namespace MotorTownDiscordBot.MotorTown;

internal class MotorTown
{
    private string _path;
    const string logPath = @"MotorTown\Saved\ServerLog";
    private LogReader _logReader;

    public WebAPI? WebAPI;
    public GameConfig GameConfig;

    public MotorTown(string path)
    {
        _path = path;
        GameConfig = ReadGameConfig();

        if (GameConfig.WebApiEnabled)
            WebAPI = new WebAPI(GameConfig.WebApiPort, GameConfig.WebApiPassword);

        _logReader = new LogReader(Path.Combine(_path, logPath));
    }

    private GameConfig ReadGameConfig()
    {
        string configPath = Path.Combine(_path, @"DedicatedServerConfig.json");
        if (!File.Exists(configPath))
        {
            throw new Exception("Game config file not found");
        }

        var gameConfig = GameConfig.FromJSON(File.ReadAllText(configPath));
        if (gameConfig is null)
        {
            throw new Exception("Failed to read game config file");
        }

        return gameConfig;
    }

    public async IAsyncEnumerable<GameEvent> ReadAsync()
    {
        DateTime now = DateTime.Now;
        await foreach (var line in _logReader.ReadAsync())
        {
            var gameEvent = GameEvent.ParseLog(line);
            if (gameEvent is null) continue;
            if (gameEvent.TimeStamp < now) continue;
            yield return gameEvent;
        }
    }
}
