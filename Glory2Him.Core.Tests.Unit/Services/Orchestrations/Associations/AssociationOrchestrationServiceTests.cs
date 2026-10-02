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
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Brokers.EventEnvelopes;
using Glory2Him.Core.Brokers.Integrities;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Glory2Him.Core.Models.Foundations.Comments;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.Reactions;
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Services.Foundations.Associations;
using Glory2Him.Core.Services.Foundations.BibleReferences;
using Glory2Him.Core.Services.Foundations.Comments;
using Glory2Him.Core.Services.Foundations.ContentItems;
using Glory2Him.Core.Services.Foundations.Links;
using Glory2Him.Core.Services.Foundations.Reactions;
using Glory2Him.Core.Services.Foundations.Tags;
using Glory2Him.Core.Services.Orchestrations.Associations;
using KellermanSoftware.CompareNetObjects;
using Moq;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Associations
{
    public partial class AssociationOrchestrationServiceTests
    {
        private readonly Mock<IAssociationService> associationServiceMock;
        private readonly Mock<IContentItemService> contentItemServiceMock;
        private readonly Mock<ITagService> tagServiceMock;
        private readonly Mock<IReactionService> reactionServiceMock;
        private readonly Mock<IBibleReferenceService> bibleReferenceServiceMock;
        private readonly Mock<ICommentService> commentServiceMock;
        private readonly Mock<ILinkService> linkServiceMock;
        private readonly Mock<IAccessBroker> accessBrokerMock;
        private readonly Mock<IEventEnvelopeBroker> eventEnvelopeBrokerMock;
        private readonly Mock<IEnvelopeIntegrityBroker> envelopeIntegrityBrokerMock;
        private readonly Mock<ILoggingBroker> loggingBrokerMock;
        private readonly IAssociationOrchestrationService associationOrchestrationService;
        private SecurityContext ambientSecurityContext;

        public AssociationOrchestrationServiceTests()
        {
            this.associationServiceMock = new Mock<IAssociationService>();
            this.contentItemServiceMock = new Mock<IContentItemService>();
            this.tagServiceMock = new Mock<ITagService>();
            this.reactionServiceMock = new Mock<IReactionService>();
            this.bibleReferenceServiceMock = new Mock<IBibleReferenceService>();
            this.commentServiceMock = new Mock<ICommentService>();
            this.linkServiceMock = new Mock<ILinkService>();
            this.accessBrokerMock = new Mock<IAccessBroker>();
            this.eventEnvelopeBrokerMock = new Mock<IEventEnvelopeBroker>();
            this.envelopeIntegrityBrokerMock = new Mock<IEnvelopeIntegrityBroker>();
            this.loggingBrokerMock = new Mock<ILoggingBroker>();

            // Allowing by default, for the reason the integrity broker is valid by default: every
            // write that resolves a ContentItem endpoint meets the facet gate (§ARC16.2.1), and a
            // test about anything else would otherwise be asserting the gate. Each key asked is
            // answered with a setting that admits every facet. The gate's own tests override it.
            this.accessBrokerMock.Setup(broker =>
                broker.RetrieveEffectiveContentItemSettingsAsync(
                    It.IsAny<IReadOnlyList<ContentItemSettingKey>>(),
                    It.IsAny<CancellationToken>()))
                        .Returns((IReadOnlyList<ContentItemSettingKey> contentItemSettingKeys, CancellationToken _) =>
                            new ValueTask<IReadOnlyList<EffectiveContentItemSetting>>(
                                contentItemSettingKeys
                                    .Select(contentItemSettingKey => new EffectiveContentItemSetting
                                    {
                                        ContentItemId = contentItemSettingKey.ContentItemId,
                                        ContentItemSetting =
                                            CreateAllowingContentItemSetting(contentItemSettingKey.ContentType),
                                    })
                                    .ToList()));

            // Valid by default. The verification tests override it; every other test on the event
            // path would otherwise be asserting the guard rather than its own subject.
            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    It.IsAny<EventEnvelope<It.IsAnyType>>(),
                    It.IsAny<string>(),
                    It.IsAny<EnvelopeDirection>()))
                        .ReturnsAsync(true);

            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.IsAny<Association>()))
                    .Returns((Association content) =>
                        new ValueTask<EventEnvelope<Association>>(
                            new EventEnvelope<Association>
                            {
                                Content = content,
                                SecurityContext = this.ambientSecurityContext,
                                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
                            }));

            this.associationOrchestrationService = new AssociationOrchestrationService(
                associationService: this.associationServiceMock.Object,
                contentItemService: this.contentItemServiceMock.Object,
                tagService: this.tagServiceMock.Object,
                reactionService: this.reactionServiceMock.Object,
                bibleReferenceService: this.bibleReferenceServiceMock.Object,
                commentService: this.commentServiceMock.Object,
                linkService: this.linkServiceMock.Object,
                accessBroker: this.accessBrokerMock.Object,
                eventEnvelopeBroker: this.eventEnvelopeBrokerMock.Object,
                envelopeIntegrityBroker: this.envelopeIntegrityBrokerMock.Object,
                loggingBroker: this.loggingBrokerMock.Object);
        }

        public static TheoryData<Xeption> AssociationDependencyValidationExceptions()
        {
            string randomMessage = GetRandomString();
            var innerException = new Xeption(message: randomMessage);

            return new TheoryData<Xeption>
            {
                new Glory2Him.Core.Models.Foundations.Associations.Exceptions
                    .AssociationValidationException(message: randomMessage, innerException: innerException),

                new Glory2Him.Core.Models.Foundations.Associations.Exceptions
                    .AssociationDependencyValidationException(message: randomMessage, innerException: innerException),
            };
        }

        public static TheoryData<Xeption> AssociationDependencyExceptions()
        {
            string randomMessage = GetRandomString();
            var innerException = new Xeption(message: randomMessage);

            return new TheoryData<Xeption>
            {
                new Glory2Him.Core.Models.Foundations.Associations.Exceptions
                    .AssociationDependencyException(message: randomMessage, innerException: innerException),

                new Glory2Him.Core.Models.Foundations.Associations.Exceptions
                    .AssociationServiceException(message: randomMessage, innerException: innerException),
            };
        }

        // A raw add request: only the endpoint types and key ids the caller supplies. A ContentItem
        // on A (the versioned, content-typed side) and a Tag on B (a non-versioned side) exercise
        // both resolution shapes.
        private static Association CreateRawAddRequest()
        {
            return new Association
            {
                EntityAType = EntityType.ContentItem,
                EntityAKeyId = Guid.NewGuid(),
                EntityBType = EntityType.Tag,
                EntityBKeyId = Guid.NewGuid(),
                UserId = null,
            };
        }

        // An add request as an HONEST publisher sends it over the substrate: the raw endpoints
        // plus what those endpoints really are — a Story in its version group on A, and nothing
        // on the Tag on B. Anything that resolved the endpoints before publishing states exactly
        // this. SetupEventPathEndpointReads resolves A to the group stated here.
        private static Association CreateHonestAddRequest()
        {
            Association addRequest = CreateRawAddRequest();
            addRequest.EntityAContentType = ContentType.Story;
            addRequest.EntityAGroupId = Guid.NewGuid();
            addRequest.EntityBContentType = null;

            return addRequest;
        }

        // An honest add request between two named endpoint types, for a pair CreateHonestAddRequest
        // does not cover. Each endpoint is a ContentItem — a Story in its own version group — or a
        // non-versioned type, which states no content type and no group.
        private static Association CreateHonestAddRequestBetween(
            EntityType entityAType,
            EntityType entityBType)
        {
            return new Association
            {
                EntityAType = entityAType,
                EntityAKeyId = Guid.NewGuid(),
                EntityAGroupId = entityAType == EntityType.ContentItem ? Guid.NewGuid() : Guid.Empty,
                EntityAContentType = entityAType == EntityType.ContentItem ? ContentType.Story : null,
                EntityBType = entityBType,
                EntityBKeyId = Guid.NewGuid(),
                EntityBGroupId = entityBType == EntityType.ContentItem ? Guid.NewGuid() : Guid.Empty,
                EntityBContentType = entityBType == EntityType.ContentItem ? ContentType.Story : null,
                UserId = null,
            };
        }

        // A request envelope as the substrate hands one over: content, the signed caller, and the
        // event id ProcessedEvents deduplicates on. Integrity is left to the broker mock.
        private static EventEnvelope<Association> CreateRequestEnvelope(
            Association association,
            SecurityContext securityContext = null) =>
            new EventEnvelope<Association>
            {
                Content = association,
                SecurityContext = securityContext ?? CreateAuthenticatedSecurityContext(),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() },
            };

        // The event path's endpoint reads, keyed on the INBOUND envelope: a ContentItem (Story) on
        // A, in the group the request states, and a Tag on B. Handed back so a test can assert what was derived from it.
        private ContentItem SetupEventPathEndpointReads(
            Association addRequest,
            EventEnvelope<Association> inboundEnvelope)
        {
            var resolvedContentItem = new ContentItem
            {
                Id = addRequest.EntityAKeyId,
                GroupId = addRequest.EntityAGroupId,
                ContentType = ContentType.Story,
            };

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(
                    addRequest.EntityAKeyId,
                    inboundEnvelope,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(resolvedContentItem);

            this.tagServiceMock.Setup(service =>
                service.RetrieveTagByIdAsync(
                    addRequest.EntityBKeyId,
                    inboundEnvelope,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new Tag { Id = addRequest.EntityBKeyId });

            return resolvedContentItem;
        }

        private static AssociationPairMatch CreatePairMatch(
            ApprovalStatus approvalStatus,
            bool isDeleted)
        {
            return new AssociationPairMatch
            {
                Id = Guid.NewGuid(),
                ApprovalStatus = approvalStatus,
                IsDeleted = isDeleted,
                CreatedBy = $"author-{Guid.NewGuid()}",
                DeletedBy = isDeleted ? $"deleter-{Guid.NewGuid()}" : null,
            };
        }

        // A raw upsert request between any two endpoint types: the endpoint types and key ids the
        // caller supplies, and nothing else.
        private static Association CreateRawUpsertRequestBetween(
            EntityType entityAType,
            EntityType entityBType)
        {
            return new Association
            {
                EntityAType = entityAType,
                EntityAKeyId = Guid.NewGuid(),
                EntityBType = entityBType,
                EntityBKeyId = Guid.NewGuid(),
            };
        }

        // The method path's endpoint reads for a request between any two of the types the flow
        // resolves, each keyed on its endpoint's key id and the test's token. A ContentItem is a
        // Story and a Link is a version, each in a group of its own, so a group never equals the key
        // it was resolved from. Hands back the ContentItem, if either endpoint is one, so a test can
        // assert what was derived from it.
        private ContentItem SetupMethodPathEndpointReads(
            Association rawRequest,
            string reactionName = null)
        {
            ContentItem resolvedContentItem = null;

            foreach ((EntityType entityType, Guid keyId) in new[]
            {
                (rawRequest.EntityAType, rawRequest.EntityAKeyId),
                (rawRequest.EntityBType, rawRequest.EntityBKeyId),
            })
            {
                switch (entityType)
                {
                    case EntityType.ContentItem:
                        resolvedContentItem = new ContentItem
                        {
                            Id = keyId,
                            GroupId = Guid.NewGuid(),
                            ContentType = ContentType.Story,
                        };

                        this.contentItemServiceMock.Setup(service =>
                            service.RetrieveContentItemByIdAsync(
                                keyId,
                                TestContext.Current.CancellationToken))
                                    .ReturnsAsync(resolvedContentItem);

                        break;

                    case EntityType.Tag:
                        this.tagServiceMock.Setup(service =>
                            service.RetrieveTagByIdAsync(keyId, TestContext.Current.CancellationToken))
                                .ReturnsAsync(new Tag { Id = keyId });

                        break;

                    case EntityType.Reaction:
                        this.reactionServiceMock.Setup(service =>
                            service.RetrieveReactionByIdAsync(keyId, TestContext.Current.CancellationToken))
                                .ReturnsAsync(new Reaction
                                {
                                    Id = keyId,
                                    Name = reactionName ?? GetRandomString(),
                                });

                        break;

                    case EntityType.Comment:
                        this.commentServiceMock.Setup(service =>
                            service.RetrieveCommentByIdAsync(keyId, TestContext.Current.CancellationToken))
                                .ReturnsAsync(new Comment { Id = keyId });

                        break;

                    case EntityType.BibleReference:
                        this.bibleReferenceServiceMock.Setup(service =>
                            service.RetrieveBibleReferenceByIdAsync(
                                keyId,
                                TestContext.Current.CancellationToken))
                                    .ReturnsAsync(new BibleReference { Id = keyId });

                        break;

                    case EntityType.Link:
                        this.linkServiceMock.Setup(service =>
                            service.RetrieveLinkByIdAsync(keyId, TestContext.Current.CancellationToken))
                                .ReturnsAsync(new Link { Id = keyId, GroupId = Guid.NewGuid() });

                        break;
                }
            }

            return resolvedContentItem;
        }

        // A winning setting that admits every facet the gate asks: each <Facet>Allowed switch on,
        // and no narrowing to Love. Every Show<Facet> switch is left off, so a gate that asked the
        // display switch instead (§ARC16.2.1) would refuse.
        private static ContentItemSetting CreateAllowingContentItemSetting(ContentType contentType) =>
            new ContentItemSetting
            {
                Id = Guid.NewGuid(),
                ContentType = contentType,
                TagsAllowed = true,
                ReactionsAllowed = true,
                CommentsAllowed = true,
                BibleReferenceAllowed = true,
                LinksAllowed = true,
                AttachmentsAllowed = true,
                LimitReactionsToLoveOnly = false,
            };

        // The settings the gate is to ask for: the item's own key id under its derived type.
        private static List<ContentItemSettingKey> CreateSettingKeysFor(ContentItem contentItem) =>
            new List<ContentItemSettingKey>
            {
                new ContentItemSettingKey
                {
                    ContentType = contentItem.ContentType,
                    ContentItemId = contentItem.Id,
                },
            };

        // the keys by value, whatever collection carries them
        private static Expression<Func<IReadOnlyList<ContentItemSettingKey>, bool>> SameSettingKeysAs(
            IReadOnlyList<ContentItemSettingKey> expectedSettingKeys) =>
            actualSettingKeys =>
                new CompareLogic(new ComparisonConfig { IgnoreObjectTypes = true })
                    .Compare(expectedSettingKeys, actualSettingKeys).AreEqual;

        private static SecurityContext CreateAuthenticatedSecurityContext(params string[] roles) =>
            new SecurityContext
            {
                IsAuthenticated = true,
                Roles = roles
            };

        private static string GetRandomString() =>
            new MnemonicString(wordCount: GetRandomNumber()).GetValue();

        private static int GetRandomNumber() =>
            new IntRange(min: 2, max: 10).GetValue();

        private static Expression<Func<Xeption, bool>> SameExceptionAs(Xeption expectedException) =>
            actualException => actualException.SameExceptionAs(expectedException);
    }
}
