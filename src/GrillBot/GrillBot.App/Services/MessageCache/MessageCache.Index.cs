﻿using GrillBot.Database.Entity.Cache;

namespace GrillBot.App.Services.MessageCache;

public partial class MessageCache
{
    private Task<List<ulong>> GetMessageIdsAsync(IUser author = null, IChannel channel = null, IGuild guild = null, CancellationToken cancellationToken = default)
        => GetMessageIdsAsync(author?.Id ?? 0, channel?.Id ?? 0, guild?.Id ?? 0, cancellationToken);

    private async Task<List<ulong>> GetMessageIdsAsync(ulong authorId = 0, ulong channelId = 0, ulong guildId = 0, CancellationToken cancellationToken = default)
    {
        using var context = DbFactory.Create();

        var query = context.MessageCacheIndexes.AsNoTracking();
        if (authorId > 0) query = query.Where(o => o.AuthorId == authorId.ToString());
        if (channelId > 0) query = query.Where(o => o.ChannelId == channelId.ToString());
        if (guildId > 0) query = query.Where(o => o.GuildId == guildId.ToString());

        var ids = await query.Select(o => o.MessageId).ToListAsync(cancellationToken);
        return ids.ConvertAll(id => Convert.ToUInt64(id));
    }

    public Task<int> GetMessagesCountAsync(IUser author = null, IChannel channel = null, IGuild guild = null, CancellationToken cancellationToken = default)
        => GetMessagesCountAsync(author?.Id ?? 0, channel?.Id ?? 0, guild?.Id ?? 0, cancellationToken);

    public async Task<int> GetMessagesCountAsync(ulong authorId = 0, ulong channelId = 0, ulong guildId = 0, CancellationToken cancellationToken = default)
    {
        using var context = DbFactory.Create();

        var query = context.MessageCacheIndexes.AsNoTracking();
        if (authorId > 0) query = query.Where(o => o.AuthorId == authorId.ToString());
        if (channelId > 0) query = query.Where(o => o.ChannelId == channelId.ToString());
        if (guildId > 0) query = query.Where(o => o.GuildId == guildId.ToString());

        return await query.CountAsync(cancellationToken);
    }

    public void ClearIndexes()
    {
        using var context = DbFactory.Create();

        context.Database.ExecuteSqlRaw("TRUNCATE TABLE public.\"MessageCacheIndexes\"");
    }

    private Task RemoveIndexAsync(IMessage message, CancellationToken cancellationToken = default)
        => RemoveIndexAsync(message.Id, cancellationToken);

    private async Task RemoveIndexAsync(ulong messageId, CancellationToken cancellationToken = default)
    {
        using var context = DbFactory.Create();

        var entity = await context.MessageCacheIndexes
            .FirstOrDefaultAsync(o => o.MessageId == messageId.ToString(), cancellationToken);
        if (entity == null) return;

        context.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
    }

    private Task CreateIndexAsync(IMessage message, CancellationToken cancellationToken = default)
        => CreateIndexAsync(message.Id, message.Channel.Id, message.Author.Id, (message.Channel as IGuildChannel)?.GuildId ?? 0, cancellationToken);

    private async Task CreateIndexAsync(ulong messageId, ulong channelId, ulong authorId, ulong guildId, CancellationToken cancellationToken = default)
    {
        var entity = new MessageCacheIndex()
        {
            AuthorId = authorId.ToString(),
            MessageId = messageId.ToString(),
            ChannelId = channelId.ToString(),
            GuildId = guildId.ToString(),
        };

        using var context = DbFactory.Create();

        await context.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task CreateIndexesAsync(IEnumerable<IMessage> messages, CancellationToken cancellationToken = default)
    {
        var entities = messages.Select(o => new MessageCacheIndex()
        {
            AuthorId = o.Author.Id.ToString(),
            ChannelId = o.Channel.Id.ToString(),
            MessageId = o.Id.ToString(),
            GuildId = ((o.Channel as IGuildChannel)?.GuildId ?? 0).ToString()
        }).ToList();

        using var context = DbFactory.Create();

        await context.AddRangeAsync(entities, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task RebuildAsync(CancellationToken cancellationToken = default)
    {
        var entities = Cache.Values.Select(o => new MessageCacheIndex()
        {
            AuthorId = o.Message.Author.Id.ToString(),
            ChannelId = o.Message.Channel.Id.ToString(),
            MessageId = o.Message.Id.ToString(),
            GuildId = ((o.Message.Channel as IGuildChannel)?.GuildId ?? 0).ToString()
        }).ToList();

        if (entities.Count == 0)
            return;

        using var context = DbFactory.Create();

        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE public.\"MessageCacheIndexes\"", cancellationToken);
        await context.AddRangeAsync(entities, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}