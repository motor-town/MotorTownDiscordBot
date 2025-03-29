namespace MotorTownDiscordBot;

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using MotorTownDiscordBot.MotorTown;

public class BotInteraction
{
    private readonly DiscordSocketClient _client;
    private readonly WebAPI _webAPI;

    public BotInteraction(DiscordSocketClient client, WebAPI webAPI)
    {
        _client = client;
        _webAPI = webAPI;
        _client.SlashCommandExecuted += SlashCommandHandler;
    }

    public async Task RegisterCommands()
    {
        try
        {
            var commands = new[]
            {
                CreateCommand("kick", "Kick player on the server", "player-id"),
                CreateCommand("ban", "Ban player on the server", "player-id"),
                CreateCommand("unban", "Unban player on the server", "player-id"),
                CreateCommand("player-list", "List of players on the server"),
                CreateCommand("ban-list", "List of banned players on the server"),
                CreateCommand("announce", "Send announcement message to server chat", "message")
            };

            await _client.BulkOverwriteGlobalApplicationCommandsAsync(commands.Select(cmd => cmd.Build()).ToArray());
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
        await Task.Run(async () =>
        {
            try
            {
                await HandleInteraction(command).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                LogException(e);
            }
        });
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
            case "player-list":
                await HandlePlayerListCommand(command);
                break;
            case "ban-list":
                await HandleBanListCommand(command);
                break;
            case "announce":
                await HandleAnnounceCommand(command);
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
        var playerId = GetCommandOptionValue(command, "player-id");
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
        var playerId = GetCommandOptionValue(command, "player-id");
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
        var playerId = GetCommandOptionValue(command, "player-id");
        if (string.IsNullOrEmpty(playerId))
        {
            await command.RespondAsync("Player ID is required");
            return;
        }

        await _webAPI.PlayerBan(playerId);
        await command.RespondAsync($"Player ({playerId}) banned");
    }

    private static string GetCommandOptionValue(SocketSlashCommand command, string optionName)
    {
        return command.Data.Options.FirstOrDefault(o => o.Name == optionName)?.Value?.ToString();
    }

    private static void LogException(Exception exception)
    {
        Debug.WriteLine(exception);
        Console.WriteLine(exception.Message);
    }
}