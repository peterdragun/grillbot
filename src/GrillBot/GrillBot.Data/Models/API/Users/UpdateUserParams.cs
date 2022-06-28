﻿using GrillBot.Database.Enums;
using System;
using GrillBot.Common.Extensions;

namespace GrillBot.Data.Models.API.Users;

public class UpdateUserParams
{
    public bool BotAdmin { get; set; }
    public string Note { get; set; }
    public bool WebAdminAllowed { get; set; }
    public TimeSpan? SelfUnverifyMinimalTime { get; set; }
    public bool PublicAdminBlocked { get; set; }
    public bool CommandsDisabled { get; set; }
    public bool PointsDisabled { get; set; }

    public int GetNewFlags(int currentFlags)
    {
        return currentFlags
            .UpdateFlags((int)UserFlags.BotAdmin, BotAdmin)
            .UpdateFlags((int)UserFlags.WebAdmin, WebAdminAllowed)
            .UpdateFlags((int)UserFlags.PublicAdministrationBlocked, PublicAdminBlocked)
            .UpdateFlags((int)UserFlags.CommandsDisabled, CommandsDisabled)
            .UpdateFlags((int)UserFlags.PointsDisabled, PointsDisabled);
    }
}
