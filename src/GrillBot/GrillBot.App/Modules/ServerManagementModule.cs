﻿using Discord;
using Discord.Commands;
using GrillBot.App.Infrastructure.Preconditions;
using GrillBot.Data;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GrillBot.App.Modules
{
    [CommandEnabledCheck("Nelze provést příkaz ze skupiny správy serveru, protože je deaktivován.")]
    [RequireContext(ContextType.Guild, ErrorMessage = "Tento příkaz lze provést jen na serveru.")]
    public class ServerManagementModule : Infrastructure.ModuleBase
    {
        private IConfiguration Configuration { get; }

        public ServerManagementModule(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        [Command("clean")]
        [Summary("Smaže zprávy v příslušném kanálu. Pokud nebyl zadán kanál jako parametr, tak bude použit kanál, kde byl zavolán příkaz.")]
        [RequireBotPermission(GuildPermission.ManageMessages, ErrorMessage = "Nemohu mazat zprávy, protože nemám oprávnění na mazání zpráv.")]
        [RequireBotPermission(GuildPermission.AddReactions, ErrorMessage = "Nemohu provést tento příkaz, protože nemám oprávnění přidat reakce indikující stav.")]
        [RequireBotPermission(GuildPermission.ReadMessageHistory, ErrorMessage = "Nemohu mazat zprávy, protože nemám oprávnění na čtení historie.")]
        [RequireUserPermission(GuildPermission.ManageMessages, ErrorMessage = "Tento příkaz může použít pouze uživatel, který má oprávnění mazat zprávy.")]
        public async Task CleanAsync([Name("pocet")] int take, [Name("kanal")] ITextChannel channel = null)
        {
            await Context.Message.AddReactionAsync(Emote.Parse(Configuration["Discord:Emotes:Loading"]));

            if (channel == null)
            {
                channel = Context.Channel as ITextChannel;
                take++;
            }

            var options = new RequestOptions()
            {
                AuditLogReason = $"Clean command from GrillBot. Executed {Context.User} in #{channel.Name}",
                RetryMode = RetryMode.AlwaysRetry,
                Timeout = 30000
            };

            var messages = (await channel.GetMessagesAsync(take, options: options).FlattenAsync())
                .Where(o => o.Id != Context.Message.Id);

            var older = messages.Where(o => (DateTime.UtcNow - o.CreatedAt).TotalDays >= 14.0);
            var newer = messages.Where(o => (DateTime.UtcNow - o.CreatedAt).TotalDays < 14.0);

            await channel.DeleteMessagesAsync(newer, options);

            foreach (var msg in older)
            {
                await msg.DeleteAsync(options);
            }

            await ReplyAsync($"Bylo úspěšně smazáno zpráv: **{messages.Count()}**\nStarších, než 2 týdny: **{older.Count()}**\nNovějších, než 2 týdny: **{newer.Count()}**");
            await Context.Message.RemoveAllReactionsAsync();
            await Context.Message.AddReactionAsync(Emojis.Ok);
        }

        [Group("pin")]
        public class PinManagementSubmodule : Infrastructure.ModuleBase
        {
            private IConfiguration Configuration { get; }

            public PinManagementSubmodule(IConfiguration configuration)
            {
                Configuration = configuration;
            }

            [Command("purge")]
            [Summary("Odepne zprávy z kanálu.")]
            [RequireBotPermission(ChannelPermission.ManageMessages, ErrorMessage = "Nemohu provádet odepnutí zpráv, protože nemám oprávnění pracovat se zprávami.")]
            [RequireBotPermission(GuildPermission.AddReactions, ErrorMessage = "Nemohu provést tento příkaz, protože nemám oprávnění přidat reakce indikující stav.")]
            [RequireBotPermission(GuildPermission.ReadMessageHistory, ErrorMessage = "Nemohu mazat zprávy, protože nemám oprávnění na čtení historie.")]
            [RequireUserPermission(GuildPermission.ManageMessages, ErrorMessage = "Tento příkaz může použít pouze uživatel, který má oprávnění mazat zprávy.")]
            public async Task PurgePinsAsync(ITextChannel channel = null, params ulong[] messageIds)
            {
                await Context.Message.AddReactionAsync(Emote.Parse(Configuration["Discord:Emotes:Loading"]));

                if (channel == null)
                    channel = Context.Channel as ITextChannel;

                uint unpinned = 0;
                uint unknown = 0;
                uint notPinned = 0;

                foreach (var id in messageIds)
                {
                    if (await channel.GetMessageAsync(id) is not IUserMessage message)
                    {
                        unknown++;
                        continue;
                    }

                    if (!message.IsPinned)
                    {
                        notPinned++;
                        continue;
                    }

                    await message.UnpinAsync();
                    unpinned++;
                }

                await ReplyAsync($"Zprávy byly úspěšně odepnuty.\nCelkem zpráv: **{messageIds.Length}**\nOdepnutých: **{unpinned}**\nNepřipnutých: **{notPinned}**\nNeexistujících: **{unknown}**");

                await Context.Message.RemoveAllReactionsAsync();
                await Context.Message.AddReactionAsync(Emojis.Ok);
            }

            [Command("purge")]
            [Summary("Odepne z kanálu určitý počet zpráv.")]
            [RequireBotPermission(ChannelPermission.ManageMessages, ErrorMessage = "Nemohu provádet odepnutí zpráv, protože nemám oprávnění pracovat se zprávami.")]
            [RequireBotPermission(GuildPermission.AddReactions, ErrorMessage = "Nemohu provést tento příkaz, protože nemám oprávnění přidat reakce indikující stav.")]
            [RequireBotPermission(GuildPermission.ReadMessageHistory, ErrorMessage = "Nemohu mazat zprávy, protože nemám oprávnění na čtení historie.")]
            [RequireUserPermission(GuildPermission.ManageMessages, ErrorMessage = "Tento příkaz může použít pouze uživatel, který má oprávnění mazat zprávy.")]
            public async Task PurgePinsAsync(int count, ITextChannel channel = null)
            {
                await Context.Message.AddReactionAsync(Emote.Parse(Configuration["Discord:Emotes:Loading"]));

                if (channel == null)
                    channel = Context.Channel as ITextChannel;

                var pins = await channel.GetPinnedMessagesAsync();
                count = Math.Min(pins.Count, count);

                var pinCandidates = pins.Take(count).OfType<IUserMessage>().ToList();
                foreach (var pin in pinCandidates)
                {
                    await pin.UnpinAsync();
                }

                await ReplyAsync($"Zprávy byly úspěšně odepnuty.\nCelkem připnutých zpráv: **{pins.Count}**\nOdepnutých: **{pinCandidates.Count}**");
                await Context.Message.RemoveAllReactionsAsync();
                await Context.Message.AddReactionAsync(Emojis.Ok);
            }
        }
    }
}
