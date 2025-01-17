﻿using Discord;
using System.Diagnostics.CodeAnalysis;

namespace GrillBot.Tests.Infrastructure.Discord;

[ExcludeFromCodeCoverage]
public class ThreadBuilder : BuilderBase<IThreadChannel>
{
    public ThreadBuilder(ulong id, string name)
    {
        SetId(id);
        SetName(name);
    }

    public ThreadBuilder SetId(ulong id)
    {
        Mock.Setup(o => o.Id).Returns(id);
        Mock.Setup(o => o.CreatedAt).Returns(SnowflakeUtils.FromSnowflake(id));
        return this;
    }

    public ThreadBuilder SetName(string name)
    {
        Mock.Setup(o => o.Name).Returns(name);
        return this;
    }

    public ThreadBuilder SetGuild(IGuild guild)
    {
        Mock.Setup(o => o.Guild).Returns(guild);
        Mock.Setup(o => o.GuildId).Returns(guild.Id);
        return this;
    }

    public ThreadBuilder SetType(ThreadType type)
    {
        Mock.Setup(o => o.Type).Returns(type);
        return this;
    }

    public ThreadBuilder SetParentChannel(IGuildChannel channel)
    {
        Mock.Setup(o => o.CategoryId).Returns(channel.Id);
        return this;
    }
}
