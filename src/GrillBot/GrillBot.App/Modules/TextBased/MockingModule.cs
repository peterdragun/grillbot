﻿using Discord;
using Discord.Commands;
using GrillBot.App.Infrastructure.Commands;
using GrillBot.App.Services;
using System.Threading.Tasks;

namespace GrillBot.App.Modules.TextBased;

[Name("Mockování zpráv")]
[Infrastructure.Preconditions.RequireUserPermission(new[] { ChannelPermission.SendMessages }, false)]
public class MockingModule : Infrastructure.ModuleBase
{
    private MockingService MockingService { get; }

    public MockingModule(MockingService mockingService)
    {
        MockingService = mockingService;
    }

    [Command("mock")]
    [Summary("Mockuje existující zprávu (pro použití je třeba na cílovou zprávu tímto příkazem odpovědět).")]
    public async Task<RuntimeResult> MockAsync()
    {
        var referencedMsg = Context.Message.ReferencedMessage;

        if (referencedMsg == null)
        {
            await ReplyAsync("Chybí zpráva k mockování nebo odpověď na mockovanou zprávu.");
            return null;
        }

        // Easter egg. If user is mocking bot, send peepoangry instead
        if (Context.Message.ReferencedMessage.Author.Id == Context.Client.CurrentUser.Id)
            return new CommandRedirectResult($"angry {Context.User.Id}");

        var message = referencedMsg.ToString();

        // We are mocking referenced message. Reply to the author
        // of the original referenced message instead of replying to mocker
        await Context.Channel.SendMessageAsync(
            MockingService.CreateMockingString(message),
            options: RequestOptions.Default,
            allowedMentions: new AllowedMentions() { MentionRepliedUser = true },
            messageReference: new MessageReference(Context.Message.ReferencedMessage.Id, Context.Channel.Id, Context.Guild?.Id)
        );

        return null;
    }

    [Command("mock")]
    [Summary("Mockuje zadanou zprávu.")]
    public Task MockAsync([Remainder][Name("zpráva")] string message) =>
            ReplyAsync(MockingService.CreateMockingString(message));

}