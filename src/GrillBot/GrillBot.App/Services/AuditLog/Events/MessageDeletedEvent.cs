﻿using GrillBot.App.Extensions;
using GrillBot.App.Extensions.Discord;
using GrillBot.App.Services.FileStorage;
using GrillBot.Data.Models.AuditLog;
using GrillBot.Database.Entity;
using GrillBot.Database.Enums;

namespace GrillBot.App.Services.AuditLog.Events;

public class MessageDeletedEvent : AuditEventBase
{
    private Cacheable<IMessage, ulong> Message { get; }
    private Cacheable<IMessageChannel, ulong> Channel { get; }
    private MessageCache.MessageCache MessageCache { get; }
    private FileStorageFactory FileStorageFactory { get; }

    public MessageDeletedEvent(AuditLogService auditLogService, Cacheable<IMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel,
        MessageCache.MessageCache messageCache, FileStorageFactory fileStorageFactory) : base(auditLogService)
    {
        Message = message;
        Channel = channel;
        MessageCache = messageCache;
        FileStorageFactory = fileStorageFactory;
    }

    public override Task<bool> CanProcessAsync() =>
        Task.FromResult(Channel.HasValue && Channel.Value is SocketTextChannel);

    public override async Task ProcessAsync()
    {
        var textChannel = Channel.Value as SocketTextChannel;
        if ((Message.HasValue ? Message.Value : MessageCache.GetMessage(Message.Id, true)) is not IUserMessage deletedMessage) return;
        var timeLimit = DateTime.UtcNow.AddMinutes(-1);
        var auditLog = (await textChannel.Guild.GetAuditLogsAsync(5, actionType: ActionType.MessageDeleted).FlattenAsync())
            .Where(o => o.CreatedAt.DateTime >= timeLimit)
            .FirstOrDefault(o =>
            {
                var data = (MessageDeleteAuditLogData)o.Data;
                return data.Target.Id == deletedMessage.Author.Id && data.ChannelId == textChannel.Id;
            });

        var data = new MessageDeletedData(deletedMessage);
        var jsonData = JsonConvert.SerializeObject(data, AuditLogService.JsonSerializerSettings);
        var removedBy = auditLog?.User ?? deletedMessage.Author;

        var attachments = await GetAndStoreAttachmentsAsync(deletedMessage);
        await AuditLogService.StoreItemAsync(AuditLogItemType.MessageDeleted, textChannel.Guild, textChannel, removedBy, jsonData, auditLog?.Id, null, attachments);
    }

    private async Task<List<AuditLogFileMeta>> GetAndStoreAttachmentsAsync(IUserMessage message)
    {
        if (message.Attachments.Count == 0) return null;

        var files = new List<AuditLogFileMeta>();
        var storage = FileStorageFactory.Create("Audit");

        foreach (var attachment in message.Attachments)
        {
            var content = await attachment.DownloadAsync();
            if (content == null) continue;

            var file = new AuditLogFileMeta()
            {
                Filename = attachment.Filename,
                Size = attachment.Size
            };

            var filenameWithoutExtension = file.FilenameWithoutExtension.Cut(100, true);
            var extension = file.Extension;
            file.Filename = string.Join("_", new[] {
                filenameWithoutExtension,
                attachment.Id.ToString(),
                message.Author.Id.ToString()
            }) + extension;

            await storage.StoreFileAsync("DeletedAttachments", file.Filename, content);
            files.Add(file);
        }

        return files.Count == 0 ? null : files;
    }
}