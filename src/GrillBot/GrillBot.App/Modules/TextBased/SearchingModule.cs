﻿using Discord.Commands;
using GrillBot.App.Infrastructure.Preconditions.TextBased;
using GrillBot.App.Modules.Implementations.Searching;
using GrillBot.App.Services;
using GrillBot.App.Services.User;
using GrillBot.Data.Extensions.Discord;

namespace GrillBot.App.Modules.TextBased;

[Group("hledam")]
[Name("Hledání (něčeho, třeba týmu)")]
[RequireUserPerms(ContextType.Guild)]
public class SearchingModule : Infrastructure.ModuleBase
{
    private SearchingService Service { get; }
    private UserService UserService { get; }

    public SearchingModule(SearchingService service, UserService userService)
    {
        Service = service;
        UserService = userService;
    }

    [Command("")]
    [Summary("Vytvoří hledání.")]
    public async Task CreateSearchAsync([Remainder][Name("obsah")] string _)
    {
        try
        {
            await Service.CreateAsync(Context.Guild, Context.User, Context.Channel, Context.Message);
            await Context.Message.AddReactionAsync(Emojis.Ok);
        }
        catch (ValidationException ex)
        {
            await ReplyAsync(ex.Message);
            await Context.Message.AddReactionAsync(Emojis.Nok);
        }
    }

    [Command("remove")]
    [Summary("Smaže hledání.")]
    public async Task RemoveSearchAsync(long id)
    {
        var isAdmin = Context.User is IGuildUser guildUser && (guildUser.GuildPermissions.Administrator || guildUser.GuildPermissions.ManageMessages);
        isAdmin = isAdmin || await UserService.IsUserBotAdminAsync(Context.User);

        try
        {
            await Service.RemoveSearchAsync(id, Context.User, isAdmin);
            await Context.Message.AddReactionAsync(Emojis.Ok);
        }
        catch (UnauthorizedAccessException ex)
        {
            await Context.Message.AddReactionAsync(Emojis.Nok);
            await ReplyAsync(ex.Message);
        }
    }

    [Command("")]
    [Summary("Vypíše hledání v daném kanálu.")]
    public async Task GetSearchingsAsync([Name("kanal")] ISocketMessageChannel channel = null)
    {
        if (channel == null) channel = Context.Channel;

        var data = await Service.GetSearchListAsync(Context.Guild, channel, 0);

        if (data.Count == 0)
        {
            await ReplyAsync($"V kanálu {channel.GetMention()} zatím nikdo nic nehledá.");
            return;
        }

        var embed = new EmbedBuilder()
            .WithSearching(data, channel, Context.Guild, 0, Context.User);

        var message = await ReplyAsync(embed: embed.Build());
        await message.AddReactionsAsync(new[] { Emojis.MoveToPrev, Emojis.MoveToNext });
    }
}
