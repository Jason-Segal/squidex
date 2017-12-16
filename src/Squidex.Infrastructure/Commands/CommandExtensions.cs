﻿// ==========================================================================
//  CommandingExtensions.cs
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex Group
//  All rights reserved.
// ==========================================================================

using System;
using System.Threading.Tasks;
using Squidex.Infrastructure.Tasks;

namespace Squidex.Infrastructure.Commands
{
    public static class CommandExtensions
    {
        public static Task CreateAsync<T>(this IAggregateHandler handler, CommandContext context, Action<T> creator) where T : class, IDomainObject
        {
            return handler.CreateAsync<T>(context, creator.ToAsync());
        }

        public static Task UpdateAsync<T>(this IAggregateHandler handler, CommandContext context, Action<T> updater) where T : class, IDomainObject
        {
            return handler.UpdateAsync<T>(context, updater.ToAsync());
        }

        public static Task CreateSyncedAsync<T>(this IAggregateHandler handler, CommandContext context, Action<T> creator) where T : class, IDomainObject
        {
            return handler.CreateSyncedAsync<T>(context, creator.ToAsync());
        }

        public static Task UpdateSyncedAsync<T>(this IAggregateHandler handler, CommandContext context, Action<T> updater) where T : class, IDomainObject
        {
            return handler.UpdateSyncedAsync<T>(context, updater.ToAsync());
        }

        public static Task HandleAsync(this ICommandMiddleware commandMiddleware, CommandContext context)
        {
            return commandMiddleware.HandleAsync(context, () => TaskHelper.Done);
        }
    }
}