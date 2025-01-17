﻿using Discord.Commands;
using Discord.Interactions;
using GrillBot.App.Infrastructure.TypeReaders.Interactions;
using GrillBot.App.Infrastructure.TypeReaders.TextBased;

namespace GrillBot.App.Infrastructure.TypeReaders;

public static class TypeReadersExtensions
{
    public static void RegisterTypeReaders(this CommandService commandService)
    {
        commandService.AddTypeReader<IMessage>(new MessageTypeReader(), true);
        commandService.AddTypeReader<IEmote>(new EmotesTypeReader());
        commandService.AddTypeReader<IUser>(new UserTypeReader(), true);
        commandService.AddTypeReader<DateTime>(new DateTimeTypeReader(), true);
    }

    public static void RegisterTypeConverters(this InteractionService service)
    {
        service.AddTypeConverter<IMessage>(new MessageTypeConverter());
        service.AddTypeConverter<IEmote>(new EmotesTypeConverter());
        service.AddTypeConverter<bool>(new BooleanTypeConverter());
        service.AddTypeConverter<DateTime>(new DateTimeTypeConverter());
        service.AddTypeConverter<IUser[]>(new UsersTypeConverter());
    }
}
