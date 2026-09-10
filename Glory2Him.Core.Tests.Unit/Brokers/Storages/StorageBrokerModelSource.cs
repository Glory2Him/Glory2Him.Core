// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’"
// https://john.bible/john-14-6
// If Jesus is who He said He is, what does that mean for you, today?
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using Glory2Him.Core.Brokers.Storages.Sql;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;

namespace Glory2Him.Core.Tests.Unit.Brokers.Storages
{
    /// <summary>
    /// The built <see cref="StorageBroker"/> model, for the index guards that assert on
    /// configuration rather than behaviour.
    ///
    /// <para>Built once for the whole run. The model is immutable and identical for every
    /// caller, and building it is by far the most expensive thing these tests do, so each
    /// guard constructing its own was the same work repeated per class.</para>
    ///
    /// <para>The connection-string key lives here for the same reason: it is the production
    /// key <see cref="StorageBroker"/> reads, and holding it in one place means a rename
    /// breaks one line rather than every guard at once.</para>
    ///
    /// <para><b>Two models, deliberately.</b> <see cref="Model"/> is the RUNTIME model, which EF
    /// strips of everything query execution does not need — check constraints among them, and
    /// asking for one throws rather than answering empty. <see cref="DesignTimeModel"/> is the
    /// unstripped one the migration pipeline reads, so a guard on a constraint has to take that
    /// one. Indexes survive both, so the index guards stay on the cheaper model.</para>
    /// </summary>
    internal static class StorageBrokerModelSource
    {
        private static readonly Lazy<IModel> LazyModel = new Lazy<IModel>(BuildModel);

        private static readonly Lazy<IModel> LazyDesignTimeModel =
            new Lazy<IModel>(BuildDesignTimeModel);

        public static IModel Model => LazyModel.Value;

        public static IModel DesignTimeModel => LazyDesignTimeModel.Value;

        private static IModel BuildModel()
        {
            // A connection string is required for OnConfiguring to complete, but no connection
            // is opened: EF builds the model lazily from the configuration alone.
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Glory2HimConnectionString"] =
                        "Server=(local);Database=ModelOnly;Integrated Security=true;",
                })
                .Build();

            using var storageBroker = new StorageBroker(configuration);

            return storageBroker.Model;
        }

        private static IModel BuildDesignTimeModel()
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Glory2HimConnectionString"] =
                        "Server=(local);Database=ModelOnly;Integrated Security=true;",
                })
                .Build();

            using var storageBroker = new StorageBroker(configuration);

            return storageBroker.GetService<IDesignTimeModel>().Model;
        }
    }
}
