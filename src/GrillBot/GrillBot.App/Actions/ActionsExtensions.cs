﻿using Microsoft.Extensions.DependencyInjection;

namespace GrillBot.App.Actions;

public static class ActionsExtensions
{
    public static IServiceCollection AddActions(this IServiceCollection services)
    {
        return services
            .AddApiActions();
    }

    private static IServiceCollection AddApiActions(this IServiceCollection services)
    {
        // V1
        // AuditLog
        services
            .AddScoped<Api.V1.AuditLog.RemoveItem>()
            .AddScoped<Api.V1.AuditLog.GetAuditLogList>()
            .AddScoped<Api.V1.AuditLog.GetFileContent>()
            .AddScoped<Api.V1.AuditLog.CreateLogItem>();

        // Auth
        services
            .AddScoped<Api.V1.Auth.GetRedirectLink>()
            .AddScoped<Api.V1.Auth.ProcessCallback>()
            .AddScoped<Api.V1.Auth.CreateToken>();

        // AutoReply
        services
            .AddScoped<Api.V1.AutoReply.GetAutoReplyList>()
            .AddScoped<Api.V1.AutoReply.GetAutoReplyItem>()
            .AddScoped<Api.V1.AutoReply.CreateAutoReplyItem>()
            .AddScoped<Api.V1.AutoReply.UpdateAutoReplyItem>()
            .AddScoped<Api.V1.AutoReply.RemoveAutoReplyItem>();

        // Channel
        services
            .AddScoped<Api.V1.Channel.GetChannelUsers>()
            .AddScoped<Api.V1.Channel.SendMessageToChannel>()
            .AddScoped<Api.V1.Channel.GetChannelList>()
            .AddScoped<Api.V1.Channel.ClearMessageCache>()
            .AddScoped<Api.V1.Channel.GetChannelDetail>()
            .AddScoped<Api.V1.Channel.UpdateChannel>()
            .AddScoped<Api.V1.Channel.GetChannelboard>()
            .AddScoped<Api.V1.Channel.GetChannelSimpleList>();

        // Command
        services
            .AddScoped<Api.V1.Command.CreateExplicitPermission>()
            .AddScoped<Api.V1.Command.GetCommandsList>()
            .AddScoped<Api.V1.Command.GetExplicitPermissionList>()
            .AddScoped<Api.V1.Command.RemoveExplicitPermission>()
            .AddScoped<Api.V1.Command.SetExplicitPermissionState>();

        // Emote
        services
            .AddScoped<Api.V1.Emote.GetEmoteSuggestionsList>()
            .AddScoped<Api.V1.Emote.GetStatsOfEmotes>()
            .AddScoped<Api.V1.Emote.GetSupportedEmotes>()
            .AddScoped<Api.V1.Emote.MergeStats>()
            .AddScoped<Api.V1.Emote.RemoveStats>();

        // Guild
        services
            .AddScoped<Api.V1.Guild.GetAvailableGuilds>()
            .AddScoped<Api.V1.Guild.GetGuildDetail>()
            .AddScoped<Api.V1.Guild.GetGuildList>()
            .AddScoped<Api.V1.Guild.GetRoles>()
            .AddScoped<Api.V1.Guild.UpdateGuild>();
        
        // Invite
        services
            .AddScoped<Api.V1.Invite.GetInviteList>()
            .AddScoped<Api.V1.Invite.GetMetadataCount>()
            .AddScoped<Api.V1.Invite.RefreshMetadata>();
        
        // Points
        services
            .AddScoped<Api.V1.Points.GetPointsLeaderboard>()
            .AddScoped<Api.V1.Points.GetSummaries>()
            .AddScoped<Api.V1.Points.GetSummaryGraphData>()
            .AddScoped<Api.V1.Points.GetTransactionList>();

        // User
        services
            .AddScoped<Api.V1.User.GetAvailableUsers>();

        // V2
        services
            .AddScoped<Api.V2.GetTodayBirthdayInfo>()
            .AddScoped<Api.V2.GetRubbergodUserKarma>();

        return services;
    }
}
