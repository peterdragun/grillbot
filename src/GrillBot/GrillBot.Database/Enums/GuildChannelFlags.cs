﻿using System;

namespace GrillBot.Database.Enums
{
    [Flags]
    public enum GuildChannelFlags
    {
        None = 0,

        /// <summary>
        /// Ignores cache loading when bot connectiong on discord.
        /// </summary>
        IgnoreCache = 1
    }
}
