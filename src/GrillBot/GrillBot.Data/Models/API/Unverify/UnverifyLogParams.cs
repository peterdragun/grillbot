﻿using GrillBot.Data.Infrastructure.Validation;
using GrillBot.Data.Models.API.Common;
using GrillBot.Database;
using GrillBot.Database.Entity;
using GrillBot.Database.Enums;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NSwag.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GrillBot.Data.Models.API.Unverify;

/// <summary>
/// Paginated params of unverify logs
/// </summary>
public class UnverifyLogParams : IQueryableModel<UnverifyLog>
{
    /// <summary>
    /// Selected operations.
    /// </summary>
    public UnverifyOperation? Operation { get; set; }

    /// <summary>
    /// Guild ID
    /// </summary>
    [DiscordId]
    public string GuildId { get; set; }

    /// <summary>
    /// Who did operation. If user have lower permission, this property is ignored.
    /// </summary>
    [DiscordId]
    public string FromUserId { get; set; }

    /// <summary>
    /// Who was target of operation. If user have lower permission, this property is ignored.
    /// </summary>
    [DiscordId]
    public string ToUserId { get; set; }

    /// <summary>
    /// Range when operation did.
    /// </summary>
    public RangeParams<DateTime?> Created { get; set; }

    [JsonIgnore]
    [OpenApiIgnore]
    public List<string> MutualGuilds { get; set; }

    /// <summary>
    /// Available: Operation, Guild, FromUser, ToUser, CreatedAt
    /// Default: CreatedAt.
    /// </summary>
    public SortParams Sort { get; set; } = new() { OrderBy = "CreatedAt" };
    public PaginatedParams Pagination { get; set; } = new();

    public IQueryable<UnverifyLog> SetIncludes(IQueryable<UnverifyLog> query)
    {
        return query
            .Include(o => o.FromUser.User)
            .Include(o => o.ToUser.User)
            .Include(o => o.Guild);
    }

    public IQueryable<UnverifyLog> SetQuery(IQueryable<UnverifyLog> query)
    {
        if (Operation != null)
            query = query.Where(o => o.Operation == Operation);

        if (!string.IsNullOrEmpty(GuildId))
            query = query.Where(o => o.GuildId == GuildId);

        if (!string.IsNullOrEmpty(FromUserId))
            query = query.Where(o => o.FromUserId == FromUserId);

        if (!string.IsNullOrEmpty(ToUserId))
            query = query.Where(o => o.ToUserId == ToUserId);

        if (Created != null)
        {
            if (Created.From != null)
                query = query.Where(o => o.CreatedAt >= Created.From.Value);

            if (Created.To != null)
                query = query.Where(o => o.CreatedAt <= Created.To.Value);
        }

        if (MutualGuilds != null)
            query = query.Where(o => MutualGuilds.Contains(o.GuildId));

        return query;
    }

    public IQueryable<UnverifyLog> SetSort(IQueryable<UnverifyLog> query)
    {
        var sortQuery = Sort.OrderBy switch
        {
            "Operation" => Sort.Descending switch
            {
                true => query.OrderByDescending(o => o.Operation),
                _ => query.OrderBy(o => o.Operation)
            },
            "Guild" => Sort.Descending switch
            {
                true => query.OrderByDescending(o => o.Guild.Name),
                _ => query.OrderBy(o => o.Guild.Name)
            },
            "FromUser" => Sort.Descending switch
            {
                true => query.OrderByDescending(o => o.FromUser.User.Username).ThenByDescending(o => o.FromUser.User.Discriminator),
                _ => query.OrderBy(o => o.FromUser.User.Username).ThenBy(o => o.FromUser.User.Discriminator)
            },
            "ToUser" => Sort.Descending switch
            {
                true => query.OrderByDescending(o => o.ToUser.User.Username).ThenByDescending(o => o.ToUser.User.Discriminator),
                _ => query.OrderBy(o => o.ToUser.User.Username).ThenBy(o => o.ToUser.User.Discriminator)
            },
            _ => Sort.Descending switch
            {
                true => query.OrderByDescending(o => o.CreatedAt),
                _ => query.OrderBy(o => o.CreatedAt)
            },
        };

        if (Sort.Descending)
            return sortQuery.ThenByDescending(o => o.Id);
        else
            return sortQuery.ThenBy(o => o.Id);
    }
}
