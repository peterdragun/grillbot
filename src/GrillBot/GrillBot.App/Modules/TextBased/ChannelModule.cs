﻿using Discord.Commands;
using GrillBot.App.Infrastructure.Preconditions.TextBased;

namespace GrillBot.App.Modules.TextBased;

[Group("channel")]
public class ChannelModule : Infrastructure.ModuleBase
{
    [Command("board")]
    [TextCommandDeprecated(AlternativeCommand = "/channel board")]
    public Task GetChannelBoardAsync() => Task.CompletedTask;

    [Command]
    [TextCommandDeprecated(AlternativeCommand = "/channel info")]
    public Task GetStatisticsOfChannelAsync(SocketTextChannel _) => Task.CompletedTask;
}
