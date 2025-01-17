﻿using GrillBot.App.Services.AuditLog;
using GrillBot.Data.Models.AuditLog;
using GrillBot.Database.Entity;
using GrillBot.Database.Enums;
using GrillBot.Tests.Infrastructure.Common;
using GrillBot.Tests.Infrastructure.Discord;

namespace GrillBot.Tests.App.Services.AuditLog;

[TestClass]
public class AuditLogWriterTests : ServiceTest<AuditLogWriter>
{
    protected override AuditLogWriter CreateService()
    {
        return new AuditLogWriter(DatabaseBuilder);
    }

    [TestMethod]
    public async Task StoreAsync_Success()
    {
        var user = new UserBuilder(Consts.UserId, Consts.Username, Consts.Discriminator).Build();
        var guild = new GuildBuilder(Consts.GuildId, Consts.GuildName).Build();
        var guildUser = new GuildUserBuilder(Consts.UserId, Consts.Username, Consts.Discriminator).Build();

        var items = new List<AuditLogDataWrapper>
        {
            new(AuditLogItemType.Warning, "{}", guild, processedUser: user, discordAuditLogItemId: "12345"),
            new(AuditLogItemType.Warning, "{}", files: new List<AuditLogFileMeta> { new() { Filename = "File", Size = 1 } }),
            new(AuditLogItemType.Warning, "{}", guild, discordAuditLogItemId: "12345"),
            new(AuditLogItemType.Warning, "{}", guild, discordAuditLogItemId: "12345", createdAt: DateTime.Now),
            new(AuditLogItemType.Warning, "{}", guild, processedUser: guildUser, discordAuditLogItemId: "12345", createdAt: DateTime.MinValue),
            new(AuditLogItemType.Warning, "{}", guild, processedUser: guildUser, discordAuditLogItemId: "12345")
        };

        await Service.StoreAsync(items);
        Assert.IsTrue(true);
    }

    [TestMethod]
    public async Task StoreItemAsync_Success()
    {
        var user = new UserBuilder(Consts.UserId, Consts.Username, Consts.Discriminator).Build();
        var guild = new GuildBuilder(Consts.GuildId, Consts.GuildName).Build();

        await Service.StoreAsync(new AuditLogDataWrapper(AuditLogItemType.Warning, "{}", guild, processedUser: user, discordAuditLogItemId: "12345"));
        Assert.IsTrue(true);
    }
}
