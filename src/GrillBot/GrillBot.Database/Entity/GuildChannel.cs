﻿using Discord;
using GrillBot.Database.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GrillBot.Database.Entity
{
    public class GuildChannel
    {
        [StringLength(30)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string ChannelId { get; set; }

        [StringLength(30)]
        public string GuildId { get; set; }

        [ForeignKey(nameof(GuildId))]
        public Guild Guild { get; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public int Flags { get; set; }

        public ISet<SearchItem> SearchItems { get; set; }
        public ISet<GuildUserChannel> Channels { get; set; }

        public GuildChannel()
        {
            SearchItems = new HashSet<SearchItem>();
            Channels = new HashSet<GuildUserChannel>();
        }

        public static GuildChannel FromDiscord(IGuild guild, IChannel channel)
        {
            return new GuildChannel()
            {
                ChannelId = channel.Id.ToString(),
                GuildId = guild.Id.ToString(),
                Name = channel.Name
            };
        }

        public bool HasFlags(GuildChannelFlags flags) => (Flags & (int)flags) != 0;
    }
}
