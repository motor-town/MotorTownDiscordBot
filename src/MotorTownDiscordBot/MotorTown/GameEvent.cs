namespace MotorTownDiscordBot.MotorTown;

using System.Globalization;
using Scriban;

public abstract class GameEvent
{
    protected GameEvent(string text, DateTime dateTime, string player)
    {
        TimeStamp = dateTime;
        Player = player;
        Text = text;
    }

    public readonly DateTime TimeStamp;
    public readonly string Player;
    public readonly string Text;

    public string FormatTemplate(string template)
    {
        return Template.Parse(template).Render(this);
    }

    public static GameEvent? ParseLog(string line)
    {
        string[] sections = line.Split(' ');

        DateTime dateTime = DateTime.ParseExact(sections.ElementAt(0)!, "[yyyy.MM.dd-HH.mm.ss]", CultureInfo.InvariantCulture);

        if (sections.ElementAt(1) == "[CHAT]")
        {
            string player = sections.ElementAt(2).TrimEnd(':');
            string message = string.Join(" ", sections.Skip(3)).TrimEnd('\n');
            return new ChatMessageEvent(line, dateTime, player, message);
        }

        if (sections.ElementAt(1) == "Player"
            && sections.ElementAt(2) == "Login:")
        {
            var playerId = sections.Last().Trim('(', ')');
            return new SessionEvent(line, dateTime, sections.ElementAt(3), playerId, true);
        }


        if (sections.ElementAt(1) == "Player"
            && sections.ElementAt(2) == "Logout:")
        {
            return new SessionEvent(line, dateTime, sections.ElementAt(3), "", false);
        }

        if (sections.ElementAt(1) == "[ADMIN]")
        {
            return new BanEvent(line, dateTime, sections.ElementAt(4), sections.ElementAt(2));
        }

        return null;
    }
}
public class ChatMessageEvent : GameEvent
{
    public ChatMessageEvent(string text, DateTime dateTime, string player, string Message) : base(text, dateTime, player)
    {
        Message = Message;
    }

    public readonly string Message;
}

public class SessionEvent : GameEvent
{
    public SessionEvent(string text, DateTime dateTime, string player, string playerId, bool login) : base(text, dateTime, player)
    {
        Login = login;
        PlayerId = playerId;
    }
    public readonly bool Login;

    public string? PlayerId { get; set; }
}
public class BanEvent : GameEvent
{
    public BanEvent(string text, DateTime dateTime, string player, string admin) : base(text, dateTime, player)
    {
        Admin = admin;
    }

    public readonly string Admin;
}