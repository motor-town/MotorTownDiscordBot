namespace Tests
{
    public class ProgramTests
    {
        [Fact()]
        public void ParseChatLogTest()
        {
            var chatMessage = (ChatMessage?)Program.ParseLog("[2025.02.06-19.12.49] [CHAT] McRay: biraz garip geliyor\n");

            Assert.NotNull(chatMessage);
            Assert.IsType<ChatMessage>(chatMessage);
            Assert.Equal(new DateTime(2025, 2, 6, 19, 12, 49), chatMessage.TimeStamp);
            Assert.Equal("McRay", chatMessage.Player);
            Assert.Equal("biraz garip geliyor", chatMessage.Message);
        }

        [Fact()]
        public void ParseLoginLogTest()
        {
            var sessionEvent = (SessionEvent?)Program.ParseLog("[2025.02.06-17.12.35] Player Login: McRay (76561197997411952)");

            Assert.NotNull(sessionEvent);
            Assert.IsType<SessionEvent>(sessionEvent);
            Assert.Equal(new DateTime(2025, 2, 6, 17, 12, 35), sessionEvent.TimeStamp);
            Assert.Equal("McRay", sessionEvent.Player);
            Assert.True(sessionEvent.Login);
        }

        [Fact()]
        public void ParseLogoutLogTest()
        {
            var sessionEvent = (SessionEvent?)Program.ParseLog("[2025.02.06-22.13.57] Player Logout: McRay");

            Assert.NotNull(sessionEvent);
            Assert.IsType<SessionEvent>(sessionEvent);
            Assert.Equal(new DateTime(2025, 2, 6, 22, 13, 57), sessionEvent.TimeStamp);
            Assert.Equal("McRay", sessionEvent.Player);
            Assert.False(sessionEvent.Login);
        }

        [Fact()]
        public void ParseBanLogTest()
        {
            var banEvent = (BanEvent?)Program.ParseLog("[2025.02.06-22.13.57] [ADMIN] Arend BAN McRay");

            Assert.NotNull(banEvent);
            Assert.IsType<BanEvent>(banEvent);
            Assert.Equal(new DateTime(2025, 2, 6, 22, 13, 57), banEvent.TimeStamp);
            Assert.Equal("McRay", banEvent.Player);
            Assert.Equal("Arend", banEvent.Admin);
        }

        [Fact()]
        public void ParseSrtingFormatTest()
        {
            ChatMessage chatMessage = new ChatMessage(DateTime.Now, "Eric", "Hello World!");
            string template = "{{player}} said '{{message}}'";
            string value = chatMessage.FormatTemplate(template);

            Assert.Equal("Eric said 'Hello World!'", value);
        }
    }
}