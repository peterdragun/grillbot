﻿using Discord.WebSocket;
using GrillBot.App.Extensions;
using GrillBot.Data.Extensions.Discord;
using GrillBot.Database.Entity;
using GrillBot.Database.Enums;
using GrillBot.Database.Services;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace GrillBot.App.Services.Unverify
{
    public class UnverifyChecker
    {
        private GrillBotContextFactory DbFactory { get; }

        private TimeSpan UnverifyMinimalTime { get; }
        private TimeSpan SelfunverifyMinimalTime { get; }
        private int MaxKeepAccessCount { get; }
        private int MaxMemberPerCall { get; }

        public UnverifyChecker(GrillBotContextFactory dbFactory, IConfiguration configuration)
        {
            DbFactory = dbFactory;

            var unverifyConfig = configuration.GetSection("Unverify");
            UnverifyMinimalTime = TimeSpan.FromMinutes(unverifyConfig.GetValue<int>("MinimalTimes:Unverify"));
            SelfunverifyMinimalTime = TimeSpan.FromMinutes(unverifyConfig.GetValue<int>("MinimalTimes:Selfunverify"));
            MaxKeepAccessCount = unverifyConfig.GetValue<int>("MaxKeepAccessCount");
            MaxMemberPerCall = unverifyConfig.GetValue<int>("MaxMembersPerCall");
        }

        public void ValidateUnverifyGroup(List<SocketUser> users)
        {
            if (users.Count > MaxMemberPerCall)
                throw new ValidationException($"Příliš mnoho lidí. Maximálně lze zpracovat {MaxMemberPerCall}");
        }

        public async Task ValidateUnverifyAsync(SocketGuildUser user, SocketGuild guild, bool selfunverify, DateTime end, int keeped)
        {
            if (keeped > MaxKeepAccessCount)
                throw new ValidationException($"Nelze si ponechat více, než {MaxKeepAccessCount} rolí a kanálů.");

            if (guild.OwnerId == user.Id)
                throw new ValidationException($"Nelze provést odebrání přístupu, protože uživatel **{user.GetDisplayName()}** je vlastník tohoto serveru.");

            if (user.GuildPermissions.Administrator)
                throw new ValidationException($"Nelze provést odebrání přístupu, protože uživatel **{user.GetDisplayName()}** má administrátorská oprávnění.");

            if (!selfunverify)
                ValidateRoles(guild, user);

            ValidateUnverifyDate(end, selfunverify);
            using var context = DbFactory.Create();

            var dbUser = await context.GuildUsers
                .Include(o => o.Unverify)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.GuildId == guild.Id.ToString() && o.UserId == user.Id.ToString()) ?? new GuildUser() { User = new User() };

            if (dbUser?.Unverify != null)
                throw new ValidationException($"Nelze provést odebrání přístupu, protože uživatel **{user.GetDisplayName()}** již má odebraný přístup do **{dbUser.Unverify.EndAt.ToCzechFormat()}**.");

            if (!selfunverify && dbUser.User.HaveFlags(UserFlags.BotAdmin))
                throw new ValidationException($"Nelze provést odebrání přístupu, protože uživatel **{user.GetFullName()}** je administrátor bota.");
        }

        public void ValidateUnverifyDate(DateTime end, bool selfunverify)
        {
            var diff = end - DateTime.Now;

            if (diff.TotalMinutes < 0)
                throw new ValidationException("Konec unverify musí být v budoucnosti.");

            var minimal = selfunverify ? SelfunverifyMinimalTime : UnverifyMinimalTime;
            if (diff < minimal)
                throw new ValidationException($"Minimální čas pro unverify je {minimal.Humanize(culture: new CultureInfo("cs-CZ"))}");
        }

        private static void ValidateRoles(SocketGuild guild, SocketGuildUser user)
        {
            var botRolePosition = guild.CurrentUser.Roles.Max(o => o.Position);
            var userMaxRolePosition = user.Roles.Max(o => o.Position);

            if (userMaxRolePosition > botRolePosition)
            {
                var higherRoles = user.Roles.Where(o => o.Position > botRolePosition).Select(o => o.Name);
                var higherRoleNames = string.Join(", ", higherRoles);

                throw new ValidationException($"Nelze provést odebírání přístupu, protože uživatel **{user.GetFullName()}** má vyšší role. **({higherRoleNames})**");
            }
        }
    }
}
