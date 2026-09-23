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
using Glory2Him.Core.Brokers.Integrities;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Brokers.Storages.Sql;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Glory2Him.Core.Models.Foundations.Comments;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.Reactions;
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Services.Foundations.Associations;
using Glory2Him.Core.Services.Foundations.BibleReferences;
using Glory2Him.Core.Services.Foundations.Comments;
using Glory2Him.Core.Services.Foundations.ContentItems;
using Glory2Him.Core.Services.Foundations.Links;
using Glory2Him.Core.Services.Foundations.Reactions;
using Glory2Him.Core.Services.Foundations.Tags;
using Glory2Him.Core.Services.Orchestrations.Associations;
using Moq;

namespace Glory2Him.Core.Tests.Integration.Brokers
{
    /// <summary>
    /// Stands up a real <see cref="StorageBroker"/> against LocalDB, wires every one of the seven
    /// foundation services the orchestration composes onto that ONE broker, and builds a real
    /// <see cref="AssociationOrchestrationService"/> over them.
    ///
    /// <para>All seven share a single broker on purpose: that is the request-scoped registration
    /// §ARC16.8 says the composite rests on. Under separate contexts the composed query does not
    /// degrade quietly — it fails at enumeration, which is the loud failure that rule wants.</para>
    ///
    /// <para>What this fixture exists to prove is §ARC12.2.1 rule 6's case and nothing else: the
    /// composite evaluator splices each endpoint entity's own collection read into the association
    /// query as a correlated sub-query, and only the real catalogue can say whether EF translates
    /// that rather than throwing or silently evaluating it on the client. The LINQ-to-Objects unit
    /// tests execute the same expression as delegates and translate nothing.</para>
    /// </summary>
    public sealed class AssociationCompositeQueryBroker : IDisposable
    {
        private readonly StorageBroker storageBroker;

        public AssociationCompositeQueryBroker()
        {
            this.storageBroker = new StorageBroker(
                IntegrationDatabase.BuildConfiguration(catalogueSuffix: "_composite"));

            IntegrationDatabase.EnsureSchema(this.storageBroker);

            DateTimeBrokerMock = new Mock<IDateTimeBroker>();
            SecurityAuditBrokerMock = new Mock<ISecurityAuditBroker>();
            EventEnvelopeBrokerMock = new Mock<IEventEnvelopeBroker>();

            AssociationOrchestrationService = new AssociationOrchestrationService(
                associationService: BuildFoundation(
                    (storage, dateTime, identifier, events, envelopes, audit, access, integrity, logging) =>
                        new AssociationService(
                            storage, dateTime, identifier, events, envelopes, audit, access, integrity, logging)),
                contentItemService: BuildFoundation(
                    (storage, dateTime, identifier, events, envelopes, audit, access, integrity, logging) =>
                        new ContentItemService(
                            storage, dateTime, identifier, events, envelopes, audit, access, integrity, logging)),
                tagService: BuildFoundation(
                    (storage, dateTime, identifier, events, envelopes, audit, access, integrity, logging) =>
                        new TagService(
                            storage, dateTime, identifier, events, envelopes, audit, access, integrity, logging)),
                reactionService: BuildFoundation(
                    (storage, dateTime, identifier, events, envelopes, audit, access, integrity, logging) =>
                        new ReactionService(
                            storage, dateTime, identifier, events, envelopes, audit, access, integrity, logging)),
                bibleReferenceService: BuildFoundation(
                    (storage, dateTime, identifier, events, envelopes, audit, access, integrity, logging) =>
                        new BibleReferenceService(
                            storage, dateTime, identifier, events, envelopes, audit, access, integrity, logging)),
                commentService: BuildFoundation(
                    (storage, dateTime, identifier, events, envelopes, audit, access, integrity, logging) =>
                        new CommentService(
                            storage, dateTime, identifier, events, envelopes, audit, access, integrity, logging)),
                linkService: BuildFoundation(
                    (storage, dateTime, identifier, events, envelopes, audit, access, integrity, logging) =>
                        new LinkService(
                            storage, dateTime, identifier, events, envelopes, audit, access, integrity, logging)),
                eventEnvelopeBroker: EventEnvelopeBrokerMock.Object,
                loggingBroker: new Mock<ILoggingBroker>().Object);
        }

        internal IAssociationOrchestrationService AssociationOrchestrationService { get; }

        internal Mock<IDateTimeBroker> DateTimeBrokerMock { get; }

        internal Mock<ISecurityAuditBroker> SecurityAuditBrokerMock { get; }

        internal Mock<IEventEnvelopeBroker> EventEnvelopeBrokerMock { get; }

        /// <summary>
        /// Makes the caller every one of the seven services sees. Each reaches its security
        /// context through the envelope the envelope broker mints for its OWN entity type, so all
        /// seven are stubbed — an unstubbed one hands back a null envelope and the read dies on
        /// dereference rather than on anything this fixture is measuring.
        /// </summary>
        public void ActAs(string actorUserId, params string[] roles)
        {
            var securityContext = new SecurityContext
            {
                IsAuthenticated = true,
                Roles = roles
            };

            SetupEnvelopeFor<Association>(securityContext);
            SetupEnvelopeFor<ContentItem>(securityContext);
            SetupEnvelopeFor<Tag>(securityContext);
            SetupEnvelopeFor<Reaction>(securityContext);
            SetupEnvelopeFor<BibleReference>(securityContext);
            SetupEnvelopeFor<Comment>(securityContext);
            SetupEnvelopeFor<Link>(securityContext);

            SecurityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(actorUserId);

            DateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(DateTimeOffset.UtcNow);
        }

        public async ValueTask SeedAsync(params ContentItem[] contentItems)
        {
            foreach (ContentItem contentItem in contentItems)
            {
                await this.storageBroker.InsertContentItemAsync(contentItem, CancellationToken.None);
            }
        }

        public async ValueTask SeedAsync(params Tag[] tags)
        {
            foreach (Tag tag in tags)
            {
                await this.storageBroker.InsertTagAsync(tag, CancellationToken.None);
            }
        }

        public async ValueTask SeedAsync(params Association[] associations)
        {
            foreach (Association association in associations)
            {
                await this.storageBroker.InsertAssociationAsync(association, CancellationToken.None);
            }
        }

        /// <summary>
        /// Removes every row this fixture inserted, so the catalogue can be reused without one
        /// test's rows turning up in another's result set.
        /// </summary>
        public async ValueTask ClearAsync(
            IEnumerable<Association> associations,
            IEnumerable<ContentItem> contentItems,
            IEnumerable<Tag> tags)
        {
            foreach (Association association in associations)
            {
                Association storedAssociation =
                    await this.storageBroker.SelectAssociationByIdAsync(
                        association.Id, CancellationToken.None);

                if (storedAssociation is not null)
                {
                    await this.storageBroker.DeleteAssociationAsync(
                        storedAssociation, CancellationToken.None);
                }
            }

            foreach (ContentItem contentItem in contentItems)
            {
                ContentItem storedContentItem =
                    await this.storageBroker.SelectContentItemByIdAsync(
                        contentItem.Id, CancellationToken.None);

                if (storedContentItem is not null)
                {
                    await this.storageBroker.DeleteContentItemAsync(
                        storedContentItem, CancellationToken.None);
                }
            }

            foreach (Tag tag in tags)
            {
                Tag storedTag =
                    await this.storageBroker.SelectTagByIdAsync(tag.Id, CancellationToken.None);

                if (storedTag is not null)
                {
                    await this.storageBroker.DeleteTagAsync(storedTag, CancellationToken.None);
                }
            }
        }

        public void Dispose()
        {
            IntegrationDatabase.Drop(this.storageBroker);
            this.storageBroker.Dispose();
        }

        private void SetupEnvelopeFor<T>(SecurityContext securityContext)
            where T : class =>
            EventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.IsAny<T>()))
                    .ReturnsAsync((T content) =>
                        new EventEnvelope<T>
                        {
                            Content = content,
                            SecurityContext = securityContext,
                            Metadata = new EventMetadata { EventId = Guid.NewGuid() }
                        });

        // Every foundation takes the identical nine brokers, and the ONE that has to be the same
        // object across all seven is the storage broker — hence a single builder rather than seven
        // near-identical constructor calls drifting apart.
        private TService BuildFoundation<TService>(
            Func<IStorageBroker, IDateTimeBroker, IIdentifierBroker, IEventBroker, IEventEnvelopeBroker,
                ISecurityAuditBroker, IAccessBroker, IEnvelopeIntegrityBroker, ILoggingBroker, TService> build) =>
            build(
                this.storageBroker,
                DateTimeBrokerMock.Object,
                new Mock<IIdentifierBroker>().Object,
                new Mock<IEventBroker>().Object,
                EventEnvelopeBrokerMock.Object,
                SecurityAuditBrokerMock.Object,

                // left bare: a collection read asks for no approval decision
                new Mock<IAccessBroker>().Object,

                // left bare: a direct-path call never reaches the substrate signature check
                new Mock<IEnvelopeIntegrityBroker>().Object,
                new Mock<ILoggingBroker>().Object);
    }

    /// <summary>
    /// Binds <see cref="AssociationCompositeQueryBroker"/> to a collection so xUnit builds it
    /// once and disposes it once, and so its tests — which read every row of several tables —
    /// are serialised against one another.
    /// </summary>
    [CollectionDefinition(AssociationCompositeIntegrationCollection.Name)]
    public sealed class AssociationCompositeIntegrationCollection
        : ICollectionFixture<AssociationCompositeQueryBroker>
    {
        public const string Name = "Association composite integration";
    }
}
