﻿using Discord.Commands;
using GrillBot.App.Infrastructure;
using GrillBot.Common.Extensions.Discord;
using GrillBot.Common.Managers;

namespace GrillBot.App.Handlers;

[Initializable]
public class CommandHandler
{
    private CommandService CommandService { get; }
    private IServiceProvider Provider { get; }
    private IConfiguration Configuration { get; }
    private InitManager InitManager { get; }
    private DiscordSocketClient DiscordClient { get; }

    public CommandHandler(DiscordSocketClient client, CommandService commandService, IServiceProvider provider, IConfiguration configuration,
        InitManager initManager)
    {
        CommandService = commandService;
        Provider = provider;
        Configuration = configuration;
        InitManager = initManager;
        DiscordClient = client;

        CommandService.CommandExecuted += OnCommandExecutedAsync;
        DiscordClient.MessageReceived += OnCommandTriggerTryAsync;
    }

    private async Task OnCommandTriggerTryAsync(SocketMessage message)
    {
        if (!InitManager.Get()) return;
        if (!message.TryLoadMessage(out var userMessage)) return;
        if (userMessage == null) return;

        var context = new SocketCommandContext(DiscordClient, (SocketUserMessage)userMessage);

        var argumentPosition = 0;
        var prefix = Configuration.GetValue<string>("Discord:Commands:Prefix");
        if (userMessage.IsCommand(ref argumentPosition, DiscordClient.CurrentUser, prefix))
            await CommandService.ExecuteAsync(context, argumentPosition, Provider);
    }

    private static async Task OnCommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
    {
        // Null is success, because some modules returns null after success and library always returns ExecuteResult.
        result ??= ExecuteResult.FromSuccess();

        if (!result.IsSuccess && result.Error != null)
        {
            var reply = "";

            switch (result.Error.Value)
            {
                case CommandError.ObjectNotFound when result is ParseResult parseResult && typeof(IUser).IsAssignableFrom(parseResult.ErrorParameter.Type):
                    reply = "Bohužel jsem nenalezl uživatele, kterého jsi zadal/a.";
                    break;

                case CommandError.UnmetPrecondition:
                case CommandError.Unsuccessful:
                case CommandError.ParseFailed:
                case CommandError.ObjectNotFound:
                    reply = result.ErrorReason;
                    break;
                case CommandError.Exception:
                    await context.Message.AddReactionAsync(new Emoji("❌"));
                    break;
                case CommandError.BadArgCount:
                case CommandError.UnknownCommand:
                case CommandError.MultipleMatches:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(null, nameof(result.Error));
            }

            // Reply to command message with mentioning user
            if (!string.IsNullOrEmpty(reply))
                await context.Message.ReplyAsync(reply, allowedMentions: new AllowedMentions { MentionRepliedUser = true });
        }
    }
}
