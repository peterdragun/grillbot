﻿using System;
using GrillBot.Common.Managers.Counters;
using GrillBot.Database.Entity;
using GrillBot.Database.Enums;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using GrillBot.Database.Enums.Internal;
using GrillBot.Database.Models;

namespace GrillBot.Database.Services.Repository;

public class ChannelRepository : RepositoryBase
{
    public ChannelRepository(GrillBotContext dbContext, CounterManager counter) : base(dbContext, counter)
    {
    }

    private IQueryable<GuildChannel> GetBaseQuery(bool includeDeleted, bool disableTracking, ChannelsIncludeUsersMode includeUsersMode)
    {
        var query = Context.Channels
            .Include(o => o.Guild)
            .AsQueryable();

        query = includeUsersMode switch
        {
            ChannelsIncludeUsersMode.IncludeAll => query.Include(o => o.Users).ThenInclude(o => o.User!.User),
            ChannelsIncludeUsersMode.IncludeExceptInactive =>
                query.Include(o => o.Users.Where(x => x.Count > 0 && (x.User!.User!.Flags & (long)UserFlags.NotUser) == 0)).ThenInclude(o => o.User!.User),
            _ => query
        };

        if (disableTracking)
            query = query.AsNoTracking();

        if (!includeDeleted)
            query = query.Where(o => (o.Flags & (long)ChannelFlags.Deleted) == 0);

        return query;
    }

    public async Task<GuildChannel?> FindChannelByIdAsync(ulong channelId, ulong? guildId = null, bool disableTracking = false,
        ChannelsIncludeUsersMode includeUsersMode = ChannelsIncludeUsersMode.None, bool includeParent = false, bool includeDeleted = false)
    {
        using (CreateCounter())
        {
            var query = GetBaseQuery(includeDeleted, disableTracking, includeUsersMode);
            if (guildId != null)
                query = query.Where(o => o.GuildId == guildId.ToString());
            if (includeParent)
                query = query.Include(o => o.ParentChannel);

            return await query.FirstOrDefaultAsync(o => o.ChannelId == channelId.ToString());
        }
    }

    public async Task<List<GuildChannel>> GetVisibleChannelsAsync(ulong guildId, List<string> channelIds, bool disableTracking = false,
        bool showInvisible = false)
    {
        using (Counter.Create("Database"))
        {
            var query = GetBaseQuery(false, disableTracking, ChannelsIncludeUsersMode.IncludeExceptInactive)
                .Where(o =>
                    o.GuildId == guildId.ToString() &&
                    channelIds.Contains(o.ChannelId) &&
                    o.Users.Count > 0 &&
                    o.ChannelType != ChannelType.Category
                );

            if (!showInvisible)
                query = query.Where(o => (o.Flags & (long)ChannelFlags.StatsHidden) == 0);

            return await query.ToListAsync();
        }
    }

    public async Task<List<GuildChannel>> GetAllChannelsAsync(bool disableTracking = false, bool includeDeleted = true, bool includeUsers = false, List<ChannelType>? channelTypes = null)
    {
        using (Counter.Create("Database"))
        {
            var usersIncludeMode = includeUsers ? ChannelsIncludeUsersMode.IncludeAll : ChannelsIncludeUsersMode.None;
            var query = GetBaseQuery(includeDeleted, disableTracking, usersIncludeMode);
            if (channelTypes?.Count > 0)
                query = query.Where(o => channelTypes.Contains(o.ChannelType));

            return await query.ToListAsync();
        }
    }

    public async Task<List<GuildChannel>> GetAllChannelsAsync(List<string> guildIds, bool ignoreThreads, bool disableTracking = false, ChannelFlags filterFlags = ChannelFlags.None)
    {
        using (Counter.Create("Database"))
        {
            var query = GetBaseQuery(false, disableTracking, ChannelsIncludeUsersMode.None)
                .Where(o => guildIds.Contains(o.GuildId));

            if (ignoreThreads)
                query = query.Where(o => !new[] { ChannelType.NewsThread, ChannelType.PrivateThread, ChannelType.PublicThread }.Contains(o.ChannelType));
            if (filterFlags > ChannelFlags.None)
                query = query.Where(o => (o.Flags & (long)filterFlags) != 0);

            return await query.ToListAsync();
        }
    }

    public async Task<GuildChannel> GetOrCreateChannelAsync(IGuildChannel channel, ChannelsIncludeUsersMode includeUsersMode = ChannelsIncludeUsersMode.None)
    {
        using (Counter.Create("Database"))
        {
            var entity = await GetBaseQuery(true, false, includeUsersMode)
                .FirstOrDefaultAsync(o => o.GuildId == channel.GuildId.ToString() && o.ChannelId == channel.Id.ToString());

            if (entity != null)
                return entity;

            entity = GuildChannel.FromDiscord(channel, channel.GetChannelType() ?? ChannelType.DM);
            await Context.AddAsync(entity);

            return entity;
        }
    }

    public async Task<long> GetMessagesCountOfUserAsync(IGuildUser user)
    {
        using (Counter.Create("Database"))
        {
            var query = Context.UserChannels.AsNoTracking()
                .Where(o =>
                    o.Count > 0 &&
                    o.GuildId == user.GuildId.ToString() &&
                    o.UserId == user.Id.ToString() &&
                    (o.Channel.Flags & (long)ChannelFlags.Deleted) == 0 &&
                    o.Channel.ChannelType != ChannelType.Category
                );

            return await query.SumAsync(o => o.Count);
        }
    }

    public async Task<(GuildUserChannel? lastActive, GuildUserChannel? mostActive)> GetTopChannelStatsOfUserAsync(IGuildUser user)
    {
        using (Counter.Create("Database"))
        {
            var baseQuery = Context.UserChannels.AsNoTracking()
                .Where(o =>
                    o.GuildId == user.GuildId.ToString() &&
                    (o.Channel.Flags & (long)ChannelFlags.StatsHidden) == 0 &&
                    o.Channel.ChannelType == ChannelType.Text &&
                    o.Count > 0 &&
                    (o.Channel.Flags & (long)ChannelFlags.Deleted) == 0 &&
                    o.UserId == user.Id.ToString()
                );

            var lastActive = await baseQuery.OrderByDescending(o => o.LastMessageAt).FirstOrDefaultAsync();
            var mostActive = await baseQuery.OrderByDescending(o => o.Count).FirstOrDefaultAsync();

            return (lastActive, mostActive);
        }
    }

    public async Task<List<GuildChannel>> GetChildChannelsAsync(ulong parentChannelId, ulong? guildId = null)
    {
        using (Counter.Create("Database"))
        {
            var query = Context.Channels
                .Where(o =>
                    new[] { ChannelType.NewsThread, ChannelType.PrivateThread, ChannelType.PublicThread }.Contains(o.ChannelType) &&
                    o.ParentChannelId == parentChannelId.ToString()
                );

            if (guildId != null)
                query = query.Where(o => o.GuildId == guildId.ToString());

            return await query.ToListAsync();
        }
    }

    public async Task<GuildChannel?> FindThreadAsync(IThreadChannel thread)
    {
        using (Counter.Create("Database"))
        {
            var query = Context.Channels
                .Where(o =>
                    o.GuildId == thread.GuildId.ToString() &&
                    new[] { ChannelType.NewsThread, ChannelType.PrivateThread, ChannelType.PublicThread }.Contains(o.ChannelType) &&
                    o.ChannelId == thread.Id.ToString()
                );

            if (thread.CategoryId != null)
                query = query.Where(o => o.ParentChannelId == thread.CategoryId.ToString());

            return await query.FirstOrDefaultAsync();
        }
    }

    public async Task<PaginatedResponse<GuildChannel>> GetChannelListAsync(IQueryableModel<GuildChannel> model, PaginatedParams pagination)
    {
        using (Counter.Create("Database"))
        {
            var query = CreateQuery(model, true);
            return await PaginatedResponse<GuildChannel>.CreateWithEntityAsync(query, pagination);
        }
    }

    public async Task<PaginatedResponse<GuildUserChannel>> GetUserChannelListAsync(ulong channelId, PaginatedParams pagination)
    {
        using (Counter.Create("Database"))
        {
            var query = Context.UserChannels.AsNoTracking()
                .Include(o => o.User!.User)
                .OrderByDescending(o => o.Count)
                .ThenByDescending(o => o.LastMessageAt)
                .Where(o => o.ChannelId == channelId.ToString() && o.Count > 0);

            return await PaginatedResponse<GuildUserChannel>.CreateWithEntityAsync(query, pagination);
        }
    }

    public async Task<Dictionary<string, (long count, DateTime firstMessageAt, DateTime lastMessageAt)>> GetAvailableStatsAsync(IGuild guild, IEnumerable<string> availableChannelIds,
        bool showInvisible = false)
    {
        using (CreateCounter())
        {
            var query = Context.UserChannels.AsNoTracking()
                .Where(o => o.Count > 0 && o.GuildId == guild.Id.ToString() && availableChannelIds.Contains(o.ChannelId));

            if (!showInvisible)
                query = query.Where(o => (o.Channel.Flags & (long)ChannelFlags.StatsHidden) == 0);

            var groupQuery = query.GroupBy(o => o.ChannelId)
                .Select(o => new
                {
                    ChannelId = o.Key,
                    Count = o.Sum(x => x.Count),
                    LastMessageAt = o.Max(x => x.LastMessageAt),
                    FirstMessageAt = o.Min(x => x.FirstMessageAt)
                });

            return await groupQuery.ToDictionaryAsync(o => o.ChannelId, o => (o.Count, o.FirstMessageAt, o.LastMessageAt));
        }
    }
}
