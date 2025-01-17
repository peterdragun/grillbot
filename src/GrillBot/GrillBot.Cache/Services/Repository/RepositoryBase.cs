﻿using GrillBot.Common.Managers.Counters;

namespace GrillBot.Cache.Services.Repository;

public abstract class RepositoryBase
{
    protected GrillBotCacheContext Context { get; }
    protected CounterManager Counter { get; }

    protected RepositoryBase(GrillBotCacheContext context, CounterManager counter)
    {
        Context = context;
        Counter = counter;
    }

    protected CounterItem CreateCounter()
        => Counter.Create($"Cache.{GetType().Name.Replace("Repository", "")}");
}
