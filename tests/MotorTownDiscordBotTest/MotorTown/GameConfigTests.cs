namespace MotorTownDiscordBot.MotorTown.Tests
{
    public class GameConfigTests
    {
        [Fact()]
        public void FromJSONTest()
        {
            var gameConfig = GameConfig.FromJSON(@"{
                ""bEnableHostWebAPIServer"": true,
                ""HostWebAPIServerPort"": 8080,
                ""HostWebAPIServerPassword"": ""password""
            }");

            Assert.True(gameConfig.WebApiEnabled);
            Assert.Equal(8080, gameConfig.WebApiPort);
            Assert.Equal("password", gameConfig.WebApiPassword);
        }
    }
}