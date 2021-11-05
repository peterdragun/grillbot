﻿using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GrillBot.Data.Models.API.Users
{
    public class UserListItem
    {
        public string Id { get; set; }
        public bool HaveApi { get; set; }
        public int Flags { get; set; }
        public bool HaveBirthday { get; set; }
        public string Username { get; set; }
        public UserStatus DiscordStatus { get; set; }

        /// <summary>
        /// Guild names where user is/was.
        /// </summary>
        public Dictionary<string, bool> Guilds { get; set; }

        public UserListItem() { }

        public UserListItem(Database.Entity.User user, DiscordSocketClient discordClient, IUser dcUser)
        {
            Id = user.Id;
            HaveApi = user.ApiToken != null;
            HaveBirthday = user.Birthday != null;
            Flags = user.Flags;
            Username = dcUser == null ? user.Username : $"{user.Username}#{dcUser.Discriminator}";
            DiscordStatus = dcUser?.Status ?? UserStatus.Offline;

            Guilds = user.Guilds.OrderBy(o => o.Guild.Name).ToDictionary(
                o => o.Guild.Name,
                o => discordClient.GetGuild(Convert.ToUInt64(o.GuildId))?.GetUser(Convert.ToUInt64(o.UserId)) != null
            );
        }
    }
}
