﻿using GrillBot.App.Services.AuditLog;
using GrillBot.Common.Models;
using GrillBot.Data.Models.AuditLog;
using GrillBot.Database.Enums;

namespace GrillBot.App.Actions.Api.V1.Emote;

public class RemoveStats : ApiAction
{
    private GrillBotDatabaseBuilder DatabaseBuilder { get; }
    private AuditLogWriter AuditLogWriter { get; }

    public RemoveStats(ApiRequestContext apiContext, GrillBotDatabaseBuilder databaseBuilder, AuditLogWriter auditLogWriter) : base(apiContext)
    {
        DatabaseBuilder = databaseBuilder;
        AuditLogWriter = auditLogWriter;
    }

    public async Task<int> ProcessAsync(string emoteId)
    {
        await using var repository = DatabaseBuilder.CreateRepository();

        var emotes = await repository.Emote.FindStatisticsByEmoteIdAsync(emoteId);
        if (emotes.Count == 0) return 0;

        var auditLogItem = new AuditLogDataWrapper(AuditLogItemType.Info, $"Statistiky emotu {emoteId} byly smazány. Smazáno záznamů: {emotes.Count}", null, null, ApiContext.LoggedUser);
        await AuditLogWriter.StoreAsync(auditLogItem);

        repository.RemoveCollection(emotes);
        return await repository.CommitAsync();
    }
}
