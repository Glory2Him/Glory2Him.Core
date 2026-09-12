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
using System.Linq;
using G2H.StorageClient.Clients;
using G2H.StorageClient.Tests.Acceptance.Brokers.Storages;
using G2H.StorageClient.Tests.Acceptance.Models.Users;
using Microsoft.Extensions.Configuration;
using Tynamix.ObjectFiller;

namespace G2H.StorageClient.Tests.Acceptance.Clients
{
    public partial class OperationServiceTests
    {
        private readonly IEFCoreClient efCoreClient;

        public OperationServiceTests()
        {
            // The same shape as the sibling suite in G2H.StorageClient.Tests.Integrations: the
            // JSON holds the LocalDB fallback a developer runs against, and the environment
            // layers over it so a build that has no LocalDB — the Linux CI job — can point the
            // suite at the server it started for itself (design 12.10 rules 2 and 4). An
            // in-memory literal could be reached by neither.
            var configurationBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            IConfiguration configuration = configurationBuilder.Build();
            TestDbContext dbContext = new TestDbContext(configuration);
            this.efCoreClient = new EFCoreClient(dbContext);
        }

        private static int GetRandomNumber() =>
            new IntRange(min: 2, max: 10).GetValue();

        private static string GetRandomString() =>
            new MnemonicString().GetValue();

        private static List<User> CreateRandomUsers(int count) =>
            CreateUserFiller().Create(count).ToList();

        private static User CreateRandomUser() =>
            CreateUserFiller().Create();

        private static Filler<User> CreateUserFiller()
        {
            var filler = new Filler<User>();
            filler.Setup().OnProperty(user => user.Id).Use(() => Guid.NewGuid());

            return filler;
        }
    }
}
