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
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Brokers.DateTimes;
using Glory2Him.Core.Brokers.EventEnvelopes;
using Glory2Him.Core.Brokers.Events;
using Glory2Him.Core.Brokers.Identifiers;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Brokers.Storages.Sql;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Services.Foundations.Associations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Glory2Him.Core.Tests.Integration.Brokers
{
    /// <summary>
    /// Stands up a real <see cref="StorageBroker"/> against LocalDB and wires it into a real
    /// <see cref="AssociationService"/>, with only the non-storage brokers faked.
    ///
    /// <para>The point is the collection read filter. Its unit tests run over
    /// <c>.AsQueryable()</c> on an in-memory array — LINQ to Objects — which executes the
    /// predicate as delegates and translates nothing. They prove the logic and say nothing
    /// about whether EF can turn it into SQL. This fixture is the only thing that answers
    /// that, because the filter closes over two <c>HashSet</c>s and dereferences a nullable
    /// enum inside the expression tree, over columns mapped with
    /// <c>HasConversion&lt;string&gt;()</c>.</para>
    /// </summary>
    public sealed class AssociationQueryBroker : IDisposable
    {
        private readonly StorageBroker storageBroker;

        public AssociationQueryBroker()
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();

            this.storageBroker = new StorageBroker(configuration);
            EnsureSchema(this.storageBroker);

            DateTimeBrokerMock = new Mock<IDateTimeBroker>();
            SecurityAuditBrokerMock = new Mock<ISecurityAuditBroker>();
            EventEnvelopeBrokerMock = new Mock<IEventEnvelopeBroker>();

            AssociationService = new AssociationService(
                storageBroker: this.storageBroker,
                dateTimeBroker: DateTimeBrokerMock.Object,
                identifierBroker: new Mock<IIdentifierBroker>().Object,
                eventBroker: new Mock<IEventBroker>().Object,
                eventEnvelopeBroker: EventEnvelopeBrokerMock.Object,
                securityAuditBroker: SecurityAuditBrokerMock.Object,
                loggingBroker: new Mock<ILoggingBroker>().Object);
        }

        // The schema is built from the CURRENT model rather than by running migrations.
        //
        // That is deliberate on two counts. First, these tests are about whether EF can
        // translate a predicate against the mapping as it stands, so the model is the right
        // source of truth — running migrations would test the migration history instead.
        // Second, `Database.Migrate()` cannot run here at all: `ApprovalSetting` carries a
        // property with no migration behind it, so EF raises PendingModelChangesWarning and
        // refuses. That drift is pre-existing, unrelated to associations, and has its own
        // follow-up; this is not the place to paper over it silently, hence the note.
        //
        // Dropped and recreated once per test run so a stale shape from an earlier run
        // cannot make a passing test meaningless.
        private static readonly object SchemaLock = new object();
        private static bool schemaCreated;

        private static void EnsureSchema(StorageBroker storageBroker)
        {
            lock (SchemaLock)
            {
                if (schemaCreated)
                {
                    return;
                }

                storageBroker.Database.EnsureDeleted();
                storageBroker.Database.EnsureCreated();
                schemaCreated = true;
            }
        }

        internal IAssociationService AssociationService { get; }

        internal Mock<IDateTimeBroker> DateTimeBrokerMock { get; }

        internal Mock<ISecurityAuditBroker> SecurityAuditBrokerMock { get; }

        internal Mock<IEventEnvelopeBroker> EventEnvelopeBrokerMock { get; }

        /// <summary>
        /// Makes the caller the service sees. The collection read reaches the security
        /// context through the envelope the envelope broker mints, so that is what carries
        /// the roles.
        /// </summary>
        public void ActAs(string actorUserId, params string[] roles)
        {
            var securityContext = new SecurityContext
            {
                IsAuthenticated = true,
                Roles = roles
            };

            EventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.IsAny<Association>()))
                    .ReturnsAsync((Association content) =>
                        new EventEnvelope<Association>
                        {
                            Content = content,
                            SecurityContext = securityContext,
                            Metadata = new EventMetadata { EventId = Guid.NewGuid() }
                        });

            SecurityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(actorUserId);

            DateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(DateTimeOffset.UtcNow);
        }

        public async ValueTask InsertAsync(params Association[] associations)
        {
            foreach (Association association in associations)
            {
                await this.storageBroker.InsertAssociationAsync(association, CancellationToken.None);
            }
        }

        /// <summary>
        /// Removes every row this fixture inserted. Each test seeds and clears its own rows so
        /// the database can be reused without cross-test interference.
        /// </summary>
        public async ValueTask ClearAsync(IEnumerable<Association> associations)
        {
            foreach (Association association in associations)
            {
                Association stored =
                    await this.storageBroker.SelectAssociationByIdAsync(
                        association.Id, CancellationToken.None);

                if (stored is not null)
                {
                    await this.storageBroker.DeleteAssociationAsync(stored, CancellationToken.None);
                }
            }
        }

        public void Dispose() =>
            this.storageBroker.Dispose();
    }
}
