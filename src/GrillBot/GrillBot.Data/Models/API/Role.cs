﻿using Discord;

namespace GrillBot.Data.Models.API;

public class Role
{
    /// <summary>
    /// Id of role.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Role name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Hexadecimal color of role.
    /// </summary>
    public string Color { get; set; }
}

public class RoleMappingProfile : AutoMapper.Profile
{
    public RoleMappingProfile()
    {
        CreateMap<IRole, Role>();
    }
}