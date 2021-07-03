﻿using Discord.WebSocket;
using GrillBot.App.Infrastructure;
using GrillBot.Database.Entity;
using GrillBot.Database.Enums;
using GrillBot.Database.Services;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace GrillBot.App.Services.Sync
{
    public class DiscordSyncService : ServiceBase
    {
        private GrillBotContextFactory DbFactory { get; }

        public DiscordSyncService(DiscordSocketClient client, GrillBotContextFactory dbFactory) : base(client)
        {
            DiscordClient.Ready += OnReadyAsync;
            DiscordClient.JoinedGuild += OnGuildAvailableAsync;
            DiscordClient.GuildAvailable += OnGuildAvailableAsync;
            DiscordClient.GuildUpdated += GuildUpdatedAsync;
            DiscordClient.UserJoined += OnUserJoinedAsync;
            DiscordClient.UserUpdated += OnUserUpdatedAsync;
            DiscordClient.GuildMemberUpdated += OnGuildMemberUpdated;
            DiscordClient.ChannelUpdated += OnChannelUpdatedAsync;

            DbFactory = dbFactory;
        }

        private async Task OnChannelUpdatedAsync(SocketChannel before, SocketChannel after)
        {
            if (after is not SocketTextChannel textChannel || ((SocketTextChannel)before).Name == textChannel.Name)
                return;

            using var context = DbFactory.Create();
            await SyncChannelAsync(context, textChannel);
            await context.SaveChangesAsync();
        }

        private async Task OnGuildMemberUpdated(SocketGuildUser before, SocketGuildUser after)
        {
            if (before.Nickname == after.Nickname) return;

            using var context = DbFactory.Create();
            await SyncGuildUserAsync(context, after);
            await context.SaveChangesAsync();
        }

        private async Task OnUserUpdatedAsync(SocketUser before, SocketUser after)
        {
            if (before.Username == after.Username) return;

            using var context = DbFactory.Create();
            await SyncUserAsync(context, after);
            await context.SaveChangesAsync();
        }

        private async Task OnUserJoinedAsync(SocketGuildUser user)
        {
            using var context = DbFactory.Create();

            await SyncGuildUserAsync(context, user);
            await context.SaveChangesAsync();
        }

        private async Task GuildUpdatedAsync(SocketGuild before, SocketGuild after)
        {
            if (before.Name == after.Name) return;

            using var context = DbFactory.Create();
            await SyncGuildAsync(context, after);
            await context.SaveChangesAsync();
        }

        private async Task OnGuildAvailableAsync(SocketGuild guild)
        {
            using var context = DbFactory.Create();

            await SyncGuildAsync(context, guild);
            await context.SaveChangesAsync();
        }

        private async Task OnReadyAsync()
        {
            using var context = DbFactory.Create();

            foreach (var guild in DiscordClient.Guilds)
            {
                await SyncGuildAsync(context, guild);
                await guild.DownloadUsersAsync();
                foreach (var user in guild.Users)
                {
                    await SyncGuildUserAsync(context, user);
                }

                foreach (var channel in guild.TextChannels)
                {
                    await SyncChannelAsync(context, channel);
                }
            }

            var application = await DiscordClient.GetApplicationInfoAsync();
            var botOwner = await context.Users.AsQueryable().FirstOrDefaultAsync(o => o.Id == application.Owner.Id.ToString());
            if (botOwner == null)
            {
                botOwner = new User()
                {
                    Id = application.Owner.Id.ToString(),
                    Username = application.Owner.Username
                };

                await context.AddAsync(botOwner);
            }
            botOwner.Flags |= (int)UserFlags.BotAdmin;

            await context.SaveChangesAsync();
        }

        private static async Task SyncGuildAsync(GrillBotContext context, SocketGuild guild)
        {
            var dbGuild = await context.Guilds.AsQueryable().FirstOrDefaultAsync(o => o.Id == guild.Id.ToString());
            if (dbGuild == null) return;

            dbGuild.Name = guild.Name;
        }

        private static async Task SyncUserAsync(GrillBotContext context, SocketUser user)
        {
            var dbUser = await context.Users.AsQueryable().FirstOrDefaultAsync(o => o.Id == user.Id.ToString());
            if (dbUser == null) return;

            dbUser.Username = user.Username;
        }

        private static async Task SyncGuildUserAsync(GrillBotContext context, SocketGuildUser user)
        {
            var dbUser = await context.GuildUsers.AsQueryable()
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.UserId == user.Id.ToString() && o.GuildId == user.Guild.Id.ToString());
            if (dbUser == null) return;

            dbUser.Nickname = user.Nickname;
            dbUser.User.Username = user.Username;
        }

        private static async Task SyncChannelAsync(GrillBotContext context, SocketTextChannel channel)
        {
            var dbChannel = await context.Channels.AsQueryable().FirstOrDefaultAsync(o => o.ChannelId == channel.Id.ToString() && o.GuildId == channel.Guild.Id.ToString());
            if (dbChannel == null) return;

            dbChannel.Name = channel.Name;
        }
    }
}
