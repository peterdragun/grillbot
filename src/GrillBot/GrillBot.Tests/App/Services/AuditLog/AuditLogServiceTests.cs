﻿using Discord;
using GrillBot.App.Services.AuditLog;
using GrillBot.App.Services.Discord;
using GrillBot.App.Services.MessageCache;
using GrillBot.Database.Entity;
using GrillBot.Database.Enums;
using GrillBot.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GrillBot.Tests.App.Services.AuditLog;

[TestClass]
public class AuditLogServiceTests : ServiceTest<AuditLogService>
{
    protected override AuditLogService CreateService()
    {
        var discordClient = DiscordHelper.CreateClient();
        var dbFactory = new DbContextBuilder();
        var initializationService = new DiscordInitializationService(LoggingHelper.CreateLogger<DiscordInitializationService>());
        var messageCache = new MessageCache(discordClient, initializationService);
        var storage = FileStorageHelper.Create();
        DbContext = dbFactory.Create();

        return new AuditLogService(discordClient, dbFactory, messageCache, storage, initializationService);
    }

    public override void Cleanup()
    {
        DbContext.AuditLogs.RemoveRange(DbContext.AuditLogs.AsEnumerable());
        DbContext.Users.RemoveRange(DbContext.Users.AsEnumerable());
        DbContext.Guilds.RemoveRange(DbContext.Guilds.AsEnumerable());
        DbContext.Channels.RemoveRange(DbContext.Channels.AsEnumerable());
        DbContext.GuildUsers.RemoveRange(DbContext.GuildUsers.AsEnumerable());
        DbContext.SaveChanges();
    }

    [TestMethod]
    public async Task StoreItemAsync_Success()
    {
        var user = DataHelper.CreateDiscordUser();
        var guild = DataHelper.CreateGuild();
        var guildUser = DataHelper.CreateGuildUser();

        await Service.StoreItemAsync(AuditLogItemType.Warning, guild, null, user, "{}", (ulong)12345);
        await Service.StoreItemAsync(AuditLogItemType.Warning, null, null, user, "{}", null, CancellationToken.None, new() { new() { Filename = "File", Size = 1 } });
        await Service.StoreItemAsync(AuditLogItemType.Warning, guild, null, null, "{}", "12345");
        await Service.StoreItemAsync(AuditLogItemType.Warning, guild, null, null, "{}", "12345", CancellationToken.None);
        await Service.StoreItemAsync(AuditLogItemType.Warning, guild, null, guildUser, "{}", "12345", CancellationToken.None);
        await Service.StoreItemAsync(AuditLogItemType.Warning, guild, null, guildUser, "{}", "12345");
        Assert.IsTrue(true);
    }

    [TestMethod]
    [ExcludeFromCodeCoverage]
    [ExpectedException(typeof(NotSupportedException))]
    public async Task StoreItemAsync_NotSupportedId()
    {
        await Service.StoreItemAsync(AuditLogItemType.Warning, null, null, null, "{}", new object());
    }

    private async Task FillDataAsync()
    {
        var item = new AuditLogItem()
        {
            ChannelId = "12345",
            CreatedAt = DateTime.UtcNow,
            Data = "{}",
            GuildId = "12345",
            ProcessedUserId = "12345",
            Type = Database.Enums.AuditLogItemType.Command,
            Id = 12345,
            DiscordAuditLogItemId = "12345",

        };

        await DbContext.AddAsync(item);
        await DbContext.AddAsync(new Guild() { Id = "12345", Name = "Guild" });
        await DbContext.AddAsync(new GuildChannel() { Name = "Channel", GuildId = "12345", ChannelId = "12345" });
        await DbContext.AddAsync(new GuildUser() { GuildId = "12345", UserId = "12345", Nickname = "Test" });
        await DbContext.AddAsync(new Database.Entity.User() { Id = "12345", Username = "Username", Discriminator = "1234" });
        await DbContext.SaveChangesAsync();
    }

    [TestMethod]
    public async Task GetDiscordAuditLogIdsAsync_WithFilters()
    {
        await FillDataAsync();

        var channel = DataHelper.CreateChannel();
        var guild = DataHelper.CreateGuild();
        var types = new[] { AuditLogItemType.InteractionCommand };

        var result = await Service.GetDiscordAuditLogIdsAsync(DbContext, guild, channel, types, DateTime.MinValue);
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task GetDiscordAuditLogIdsAsync_WithoutFilters()
    {
        await FillDataAsync();

        var result = await Service.GetDiscordAuditLogIdsAsync(DbContext, null, null, null, DateTime.MinValue);
        Assert.AreEqual(1, result.Count);
    }
}
