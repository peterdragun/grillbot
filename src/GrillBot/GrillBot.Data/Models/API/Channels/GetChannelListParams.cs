﻿using Discord;
using GrillBot.Data.Infrastructure.Validation;
using GrillBot.Data.Models.API.Common;
using GrillBot.Database;
using GrillBot.Database.Entity;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace GrillBot.Data.Models.API.Channels;

public class GetChannelListParams : IQueryableModel<GuildChannel>
{
    [DiscordId]
    public string GuildId { get; set; }
    public string NameContains { get; set; }
    public ChannelType? ChannelType { get; set; }

    /// <summary>
    /// Available: Name, Type, MessageCount
    /// </summary>
    public SortParams Sort { get; set; } = new() { OrderBy = "Name" };
    public PaginatedParams Pagination { get; } = new();

    public IQueryable<GuildChannel> SetIncludes(IQueryable<GuildChannel> query)
    {
        return query
            .Include(o => o.Guild)
            .Include(o => o.Users.Where(o => o.Count > 0)).ThenInclude(o => o.User.User);
    }

    public IQueryable<GuildChannel> SetQuery(IQueryable<GuildChannel> query)
    {
        if (!string.IsNullOrEmpty(GuildId))
            query = query.Where(o => o.GuildId == GuildId);

        if (!string.IsNullOrEmpty(NameContains))
            query = query.Where(o => o.Name.Contains(NameContains));

        if (ChannelType != null)
            query = query.Where(o => o.ChannelType == ChannelType.Value);

        return query;
    }

    public IQueryable<GuildChannel> SetSort(IQueryable<GuildChannel> query)
    {
        return Sort.OrderBy switch
        {
            "Type" => Sort.Descending switch
            {
                true => query.OrderByDescending(o => o.ChannelType).ThenByDescending(o => o.Name),
                _ => query.OrderBy(o => o.ChannelType).ThenBy(o => o.Name)
            },
            "MessageCount" => Sort.Descending switch
            {
                true => query.OrderByDescending(o => o.Users.Sum(x => x.Count)).ThenByDescending(o => o.Name),
                _ => query.OrderBy(o => o.Users.Sum(x => x.Count)).ThenBy(o => o.Name)
            },
            _ => Sort.Descending switch
            {
                true => query.OrderByDescending(o => o.Name),
                _ => query.OrderBy(o => o.Name)
            }
        };
    }
}
