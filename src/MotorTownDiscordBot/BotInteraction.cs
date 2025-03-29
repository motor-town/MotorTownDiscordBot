namespace MotorTownDiscordBot;

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using MotorTownDiscordBot.MotorTown;
using Newtonsoft.Json.Linq;

public class BotInteraction
{
    private readonly DiscordSocketClient _client;
    private readonly WebAPI _webAPI;
    private readonly ulong _guildId;

    public BotInteraction(DiscordSocketClient client, WebAPI webAPI)
    {
        _client = client;
        _webAPI = webAPI;

        // Guild-ID aus der Konfigurationsdatei laden
        var config = JObject.Parse(File.ReadAllText("config.json"));
        _guildId = ulong.Parse(config["guildId"]?.ToString() ?? throw new Exception("Guild ID not found in config"));

        _client.SlashCommandExecuted += SlashCommandHandler;
    }

    public async Task RegisterCommands()
    {
        try
        {
            var commands = new[]
            {
                CreateCommand("kick", "Kick a player from the server", "playerid"),
                CreateCommand("ban", "Ban a player from the server", "playerid"),
                CreateCommand("unban", "Unban a player from the server", "playerid"),
                CreateCommand("playerlist", "Get a list of players on the server"),
                CreateCommand("banlist", "Get a list of banned players"),
                CreateCommand("announce", "Send an announcement to the server", "message"),
                CreateCommand("onlineplayers", "Show all online players in an embed")
            };

            // Guild-Slash-Commands registrieren
            await _client.Rest.BulkOverwriteGuildApplicationCommandsAsync(_guildId, commands.Select(cmd => cmd.Build()).ToArray());
            Console.WriteLine("Slash commands registered successfully.");
        }
        catch (HttpException exception)
        {
            LogException(exception);
        }
    }

    private SlashCommandBuilder CreateCommand(string name, string description, string optionName = null)
    {
        var command = new SlashCommandBuilder()
            .WithName(name)
            .WithDescription(description);

        if (!string.IsNullOrEmpty(optionName))
        {
            command.AddOption(optionName, ApplicationCommandOptionType.String, $"Enter {optionName}");
        }

        return command;
    }

    private async Task SlashCommandHandler(SocketSlashCommand command)
    {
        try
        {
            await HandleInteraction(command);
        }
        catch (Exception e)
        {
            LogException(e);
            await command.RespondAsync("An error occurred while processing the command.");
        }
    }

    public async Task HandleInteraction(SocketSlashCommand command)
    {
        switch (command.Data.Name)
        {
            case "kick":
                await HandleKickCommand(command);
                break;
            case "ban":
                await HandleBanCommand(command);
                break;
            case "unban":
                await HandleUnbanCommand(command);
                break;
            case "playerlist":
                await HandlePlayerListCommand(command);
                break;
            case "banlist":
                await HandleBanListCommand(command);
                break;
            case "announce":
                await HandleAnnounceCommand(command);
                break;
            case "onlineplayers":
                await HandleOnlinePlayersCommand(command);
                break;
            default:
                await command.RespondAsync("Unknown command");
                break;
        }
    }

    private async Task HandleAnnounceCommand(SocketSlashCommand command)
    {
        var message = GetCommandOptionValue(command, "message");
        if (string.IsNullOrEmpty(message))
        {
            await command.RespondAsync("Message is required");
            return;
        }

        await _webAPI.SendMessage(message);
        await command.RespondAsync("Message sent");
    }

    private async Task HandleBanListCommand(SocketSlashCommand command)
    {
        var players = await _webAPI.GetPlayerBanList();
        if (players == null || players.Length == 0)
        {
            await command.RespondAsync("No players banned on the server");
            return;
        }

        var names = players.Select(p => $"{p.name} ({p.unique_id})").ToArray();
        await command.RespondAsync(string.Join('\n', names));
    }

    private async Task HandlePlayerListCommand(SocketSlashCommand command)
    {
        var players = await _webAPI.GetPlayerList();
        if (players == null || players.Length == 0)
        {
            await command.RespondAsync("No players on the server");
            return;
        }

        var names = players.Select(p => $"{p.name} ({p.unique_id})").ToArray();
        await command.RespondAsync(string.Join('\n', names));
    }

    private async Task HandleKickCommand(SocketSlashCommand command)
    {
        var playerId = GetCommandOptionValue(command, "playerid");
        if (string.IsNullOrEmpty(playerId))
        {
            await command.RespondAsync("Player ID is required");
            return;
        }

        await _webAPI.PlayerKick(playerId);
        await command.RespondAsync("Player kicked");
    }

    private async Task HandleUnbanCommand(SocketSlashCommand command)
    {
        var playerId = GetCommandOptionValue(command, "playerid");
        if (string.IsNullOrEmpty(playerId))
        {
            await command.RespondAsync("Player ID is required");
            return;
        }

        await _webAPI.PlayerUnban(playerId);
        await command.RespondAsync($"Player ({playerId}) unbanned");
    }

    private async Task HandleBanCommand(SocketSlashCommand command)
    {
        var playerId = GetCommandOptionValue(command, "playerid");
        if (string.IsNullOrEmpty(playerId))
        {
            await command.RespondAsync("Player ID is required");
            return;
        }

        await _webAPI.PlayerBan(playerId);
        await command.RespondAsync($"Player ({playerId}) banned");
    }

    private async Task HandleOnlinePlayersCommand(SocketSlashCommand command)
    {
        var players = await _webAPI.GetPlayerList();
        if (players == null || players.Length == 0)
        {
            await command.RespondAsync("No players are currently online.");
            return;
        }

        var embedBuilder = new EmbedBuilder()
            .WithTitle("Online Players")
            .WithColor(Color.Green)
            .WithDescription(string.Join('\n', players.Select(p => $"{p.name} ({p.unique_id})")));

        await command.RespondAsync(embed: embedBuilder.Build());
    }

    private static string GetCommandOptionValue(SocketSlashCommand command, string optionName)
    {
        return command.Data.Options?.FirstOrDefault(o => o.Name == optionName)?.Value?.ToString();
    }

    private static void LogException(Exception exception)
    {
        Debug.WriteLine(exception);
        Console.WriteLine($"Error: {exception.Message}");
    }
}