﻿using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GrillBot.App.Services
{
    public class DiscordHealthCheck : IHealthCheck
    {
        private DiscordSocketClient DiscordSocketClient { get; }

        public DiscordHealthCheck(DiscordSocketClient discordSocketClient)
        {
            DiscordSocketClient = discordSocketClient;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            if (DiscordSocketClient.ConnectionState != ConnectionState.Connected)
                return Task.FromResult(HealthCheckResult.Unhealthy($"Discord connection state is in '{DiscordSocketClient.ConnectionState}' state."));

            if (DiscordSocketClient.Latency >= TimeSpan.FromSeconds(2).TotalMilliseconds)
                return Task.FromResult(HealthCheckResult.Degraded($"Discord connection is degraded. Current latency is {DiscordSocketClient.Latency}ms"));

            return Task.FromResult(HealthCheckResult.Healthy());
        }
    }
}