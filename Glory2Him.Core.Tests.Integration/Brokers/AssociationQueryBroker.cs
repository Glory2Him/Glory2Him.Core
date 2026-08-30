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
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Brokers.DateTimes;
using Glory2Him.Core.Brokers.EventEnvelopes;
using Glory2Him.Core.Brokers.Events;
using Glory2Him.Core.Brokers.Identifiers;
using Glory2Him.Core.Brokers.Integrities;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Brokers.Storages.Sql;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Services.Foundations.Associations;
using Microsoft.EntityFrameworkCore;
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
            this.storageBroker = new StorageBroker(IntegrationDatabase.BuildConfiguration());
            IntegrationDatabase.EnsureSchema(this.storageBroker);

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

                // left bare: this fixture only exercises the collection read, which never
                // asks for an approval decision
                accessBroker: new Mock<IAccessBroker>().Object,

                // left bare: the collection read is a direct-path call, so it never reaches
                // the substrate signature check
                envelopeIntegrityBroker: new Mock<IEnvelopeIntegrityBroker>().Object,
                loggingBroker: new Mock<ILoggingBroker>().Object);
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
        /// Attempts an insert and returns the exception the database raised, or <c>null</c>
        /// when the row was accepted.
        ///
        /// <para>Detaching on failure is not tidiness. A rejected <c>SaveChanges</c> leaves the
        /// entity tracked in the <c>Added</c> state, and this fixture shares one context across
        /// the whole collection — the next save would retry the rejected row and fail a test
        /// that has nothing to do with it.</para>
        /// </summary>
        public async ValueTask<Exception> TryInsertAsync(Association association)
        {
            try
            {
                await this.storageBroker.InsertAssociationAsync(
                    association, CancellationToken.None);

                return null;
            }
            catch (Exception exception)
            {
                this.storageBroker.Entry(association).State = EntityState.Detached;

                return exception;
            }
        }

        /// <summary>
        /// Marks a row deleted the way a soft remove does, leaving it in the table. The
        /// pair index is filtered on <c>IsDeleted = 0</c>, so this is what frees the key.
        /// </summary>
        public async ValueTask SoftDeleteAsync(Association association)
        {
            association.IsDeleted = true;

            await this.storageBroker.UpdateAssociationAsync(
                association, CancellationToken.None);
        }

        /// <summary>
        /// Returns the definition SQL Server actually stored for a check constraint, or
        /// <c>null</c> when no constraint of that name exists.
        ///
        /// <para>This reads the DEPLOYED object rather than the configuration that produced
        /// it, which is the only way to assert that what the model declares is what the
        /// database ended up with.</para>
        /// </summary>
        public async ValueTask<string> GetCheckConstraintDefinitionAsync(string constraintName)
        {
            List<string> definitions = await this.storageBroker.Database
                .SqlQuery<string>(
                    $@"SELECT definition AS [Value]
                       FROM sys.check_constraints
                       WHERE name = {constraintName}")
                .ToListAsync();

            return definitions.Count == 0 ? null : definitions[0];
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

        // xUnit disposes a collection fixture once, after the last test in the collection —
        // deterministic teardown, unlike the ProcessExit hook this replaced.
        public void Dispose()
        {
            IntegrationDatabase.Drop(this.storageBroker);
            this.storageBroker.Dispose();
        }
    }

    /// <summary>
    /// Binds <see cref="AssociationQueryBroker"/> to a collection so xUnit builds it once,
    /// shares it across every test in the collection, and disposes it once at the end.
    ///
    /// <para>This also serialises those tests. They all query every row in the table, so
    /// running them concurrently would let one test's seeded rows appear in another's
    /// results.</para>
    /// </summary>
    [CollectionDefinition(AssociationIntegrationCollection.Name)]
    public sealed class AssociationIntegrationCollection
        : ICollectionFixture<AssociationQueryBroker>
    {
        public const string Name = "Association integration";
    }
}
