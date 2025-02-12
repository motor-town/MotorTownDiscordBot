using System.Text.Json;
using System.Text.Json.Serialization;

namespace MotorTownDiscordBot.MotorTown
{
    public class GameConfig
    {
        [JsonInclude]
        [JsonPropertyName("bEnableHostWebAPIServer")]
        public bool WebApiEnabled;

        [JsonInclude]
        [JsonPropertyName("HostWebAPIServerPort")]
        public int WebApiPort;

        [JsonInclude]
        [JsonPropertyName("HostWebAPIServerPassword")]
        public string WebApiPassword;

        public static GameConfig FromJSON(string json)
        {
            var gameConfig = JsonSerializer.Deserialize<GameConfig>(json);
            if (gameConfig is null)
            {
                throw new Exception("Failed to read game config file");
            }

            return gameConfig;
        }
    }
}