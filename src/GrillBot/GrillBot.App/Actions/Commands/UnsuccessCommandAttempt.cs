﻿using Discord.Interactions;
using GrillBot.App.Services.AuditLog;
using GrillBot.App.Services.DirectApi;
using GrillBot.Cache.Services.Managers;
using GrillBot.Common.Extensions.Discord;
using GrillBot.Common.Managers.Localization;
using GrillBot.Data.Models.API.AuditLog.Filters;
using GrillBot.Data.Models.AuditLog;
using GrillBot.Database.Enums;

namespace GrillBot.App.Actions.Commands;

public class UnsuccessCommandAttempt : CommandAction
{
    private ITextsManager Texts { get; }
    private InteractionService InteractionService { get; }
    private IDirectApiService DirectApi { get; }
    private GrillBotDatabaseBuilder DatabaseBuilder { get; }
    private AuditLogService AuditLogService { get; }
    private DataCacheManager DataCacheManager { get; }

    public UnsuccessCommandAttempt(ITextsManager texts, InteractionService interactionService, IDirectApiService directApi, GrillBotDatabaseBuilder databaseBuilder,
        AuditLogService auditLogService, DataCacheManager dataCacheManager)
    {
        Texts = texts;
        InteractionService = interactionService;
        DirectApi = directApi;
        DatabaseBuilder = databaseBuilder;
        AuditLogService = auditLogService;
        DataCacheManager = dataCacheManager;
    }

    public async Task ProcessAsync(SocketMessage message)
    {
        var parts = message.Content[1..].Split(' ');

        var reference = new MessageReference(message.Id, message.Channel.Id, failIfNotExists: false);
        var commandMention = await FindLocalCommandMentionAsync(parts, message.Channel);
        if (string.IsNullOrEmpty(commandMention) && !await ExistsRubbergodCommandAsync(parts)) return;

        var locale = await GetLastUserLocaleAsync(message.Author);
        var text = Texts["ClickOnCommand", locale] + (string.IsNullOrEmpty(commandMention) ? "" : $" ({commandMention})");
        await message.Channel.SendMessageAsync(text, messageReference: reference);
    }

    private async Task<string> FindLocalCommandMentionAsync(IReadOnlyCollection<string> parts, IChannel channel)
    {
        var guild = await AuditLogService.GetGuildFromChannelAsync(channel, channel.Id);
        var commands = await InteractionService.RestClient.GetGuildApplicationCommands(guild.Id);
        var commandMentions = commands.GetCommandMentions();

        for (var i = 0; i < parts.Count; i++)
        {
            var command = string.Join(" ", parts.Take(i + 1));
            if (commandMentions.TryGetValue(command, out var mention))
                return mention;
        }

        return null;
    }

    private async Task<bool> ExistsRubbergodCommandAsync(string[] parts)
    {
        var commands = await DataCacheManager.GetValueAsync("RubbergodCommands");
        if (string.IsNullOrEmpty(commands) || commands == "[]")
        {
            commands = await DirectApi.SendCommandAsync("Rubbergod", CommandBuilder.CreateSlashCommandListCommand());
            commands ??= "[]";

            await DataCacheManager.SetValueAsync("RubbergodCommands", commands, DateTime.Now.AddDays(7));
        }

        var rubbergodCommands = JsonConvert.DeserializeObject<List<string>>(commands)!;
        return parts
            .Select((_, i) => string.Join(" ", parts.Take(i + 1)))
            .Any(cmd => rubbergodCommands.Any(x => x.StartsWith(cmd)));
    }

    private async Task<string> GetLastUserLocaleAsync(IUser user)
    {
        var filter = new AuditLogListParams
        {
            Pagination = { Page = 0, PageSize = 1 },
            Sort = { Descending = true, OrderBy = "CreatedAt" },
            Types = new List<AuditLogItemType> { AuditLogItemType.InteractionCommand },
            ProcessedUserIds = new List<string> { user.Id.ToString() }
        };

        await using var repository = DatabaseBuilder.CreateRepository();
        var data = await repository.AuditLog.GetSimpleDataAsync(filter);
        if (data.Count == 0) return "cs";

        var logItem = JsonConvert.DeserializeObject<InteractionCommandExecuted>(data[0].Data, AuditLogWriter.SerializerSettings)!;
        return TextsManager.FixLocale(logItem.Locale);
    }
}
