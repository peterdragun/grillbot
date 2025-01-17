﻿using System.Diagnostics.CodeAnalysis;
using GrillBot.App.Actions.Api.V1.AutoReply;
using GrillBot.App.Services;
using GrillBot.Common.Managers;
using GrillBot.Data.Exceptions;
using GrillBot.Tests.Infrastructure.Common;

namespace GrillBot.Tests.App.Actions.Api.V1.AutoReply;

[TestClass]
public class RemoveAutoReplyItemTests : ApiActionTest<RemoveAutoReplyItem>
{
    protected override RemoveAutoReplyItem CreateAction()
    {
        var texts = new TextsBuilder()
            .AddText("AutoReply/NotFound", "cs", "NotFound")
            .Build();
        var discordClient = DiscordHelper.CreateClient();
        var initManager = new InitManager(TestServices.LoggerFactory.Value);
        var service = new AutoReplyService(discordClient, DatabaseBuilder, initManager);

        return new RemoveAutoReplyItem(ApiRequestContext, DatabaseBuilder, texts, service);
    }

    [TestMethod]
    public async Task ProcessAsync_Found()
    {
        await Repository.AddAsync(new Database.Entity.AutoReplyItem { Id = 1, Flags = 2, Reply = "Reply", Template = "Template" });
        await Repository.CommitAsync();

        await Action.ProcessAsync(1);
    }

    [TestMethod]
    [ExpectedException(typeof(NotFoundException))]
    [ExcludeFromCodeCoverage]
    public async Task ProcessAsync_NotFound()
    {
        await Action.ProcessAsync(1);
    }
}
