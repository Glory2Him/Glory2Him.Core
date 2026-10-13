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
using System.Linq.Expressions;
using System.Threading;
using Glory2Him.Core.Brokers.Integrities;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Glory2Him.Core.Models.Foundations.LiveUpdates;
using Glory2Him.Core.Models.Foundations.LiveUpdates.Exceptions;
using Glory2Him.Core.Models.Orchestrations.LiveUpdates.Exceptions;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Services.Foundations.ContentItems;
using Glory2Him.Core.Services.Foundations.LiveUpdates;
using Glory2Him.Core.Services.Orchestrations.LiveUpdates;
using KellermanSoftware.CompareNetObjects;
using Moq;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.LiveUpdates
{
    public partial class LiveUpdateOrchestrationServiceTests
    {
        // The name EventBroker signs an Association-Approved fact under. It is inside the HMAC, so
        // an ear verifying under any other name refuses every genuine fact it is delivered.
        private const string AssociationApprovedEventName = "AssociationApproved";

        // Pinned to a member that is not 0, so a setting key that never took the version's type
        // cannot match it by default.
        private const ContentType VisibleVersionContentType = ContentType.Devotional;

        private readonly Mock<IContentItemService> contentItemServiceMock;
        private readonly Mock<ILiveUpdateService> liveUpdateServiceMock;
        private readonly Mock<IAccessBroker> accessBrokerMock;
        private readonly Mock<IEnvelopeIntegrityBroker> envelopeIntegrityBrokerMock;
        private readonly Mock<ILoggingBroker> loggingBrokerMock;
        private readonly ILiveUpdateOrchestrationService liveUpdateOrchestrationService;

        public LiveUpdateOrchestrationServiceTests()
        {
            this.contentItemServiceMock = new Mock<IContentItemService>();
            this.liveUpdateServiceMock = new Mock<ILiveUpdateService>();
            this.accessBrokerMock = new Mock<IAccessBroker>();
            this.envelopeIntegrityBrokerMock = new Mock<IEnvelopeIntegrityBroker>();
            this.loggingBrokerMock = new Mock<ILoggingBroker>();

            this.liveUpdateOrchestrationService = new LiveUpdateOrchestrationService(
                contentItemService: this.contentItemServiceMock.Object,
                liveUpdateService: this.liveUpdateServiceMock.Object,
                accessBroker: this.accessBrokerMock.Object,
                envelopeIntegrityBroker: this.envelopeIntegrityBrokerMock.Object,
                loggingBroker: this.loggingBrokerMock.Object);
        }

        // Each family the content item foundation raises, beside what this service rethrows it as
        // (AssociationOrchestrationService.Exceptions.cs): its validation and dependency
        // validation failures as this service's dependency validation failure, its dependency
        // and service failures as this service's dependency failure, each around the
        // foundation's own inner exception.
        public static TheoryData<Xeption, Xeption> ContentItemFailures()
        {
            string randomMessage = GetRandomString();
            var innerException = new Xeption(message: randomMessage);

            return new TheoryData<Xeption, Xeption>
            {
                {
                    new ContentItemValidationException(
                        message: randomMessage, innerException: innerException),

                    CreateDependencyValidationException(innerException)
                },
                {
                    new ContentItemDependencyValidationException(
                        message: randomMessage, innerException: innerException),

                    CreateDependencyValidationException(innerException)
                },
                {
                    new ContentItemDependencyException(
                        message: randomMessage, innerException: innerException),

                    CreateDependencyException(innerException)
                },
                {
                    new ContentItemServiceException(
                        message: randomMessage, innerException: innerException),

                    CreateDependencyException(innerException)
                },
            };
        }

        // The live update foundation's failures that are not a refusal: its dependency and its
        // service exception, each this service's dependency failure.
        public static TheoryData<Xeption> LiveUpdateFailures()
        {
            string randomMessage = GetRandomString();
            var innerException = new Xeption(message: randomMessage);

            return new TheoryData<Xeption>
            {
                new LiveUpdateDependencyException(
                    message: randomMessage, innerException: innerException),

                new LiveUpdateServiceException(
                    message: randomMessage, innerException: innerException),
            };
        }

        private static LiveUpdateOrchestrationDependencyValidationException
            CreateDependencyValidationException(Xeption innerException) =>
            new LiveUpdateOrchestrationDependencyValidationException(
                message: "Live update orchestration dependency validation error occurred, " +
                    "fix the errors and try again.",
                innerException: innerException);

        private static LiveUpdateOrchestrationDependencyException CreateDependencyException(
            Xeption innerException) =>
            new LiveUpdateOrchestrationDependencyException(
                message: "Live update orchestration dependency error occurred, contact support.",
                innerException: innerException);

        // A reader's reaction on a content item, as the row stands once it is approved: endpoint A
        // the item, at its group, and endpoint B the reaction. Every id is drawn on its own, so no
        // two of them can stand in for each other.
        private static Association CreateContentItemReaction() =>
            new Association
            {
                Id = Guid.NewGuid(),
                EntityAType = EntityType.ContentItem,
                EntityAKeyId = Guid.NewGuid(),
                EntityAGroupId = Guid.NewGuid(),
                EntityAScope = Scope.AllVersions,
                EntityAContentType = VisibleVersionContentType,
                EntityBType = EntityType.Reaction,
                EntityBKeyId = Guid.NewGuid(),
                EntityBGroupId = Guid.NewGuid(),
                EntityBScope = Scope.ThisVersionOnly,
                UserId = GetRandomString(),
                CreatedBy = GetRandomString(),
                UpdatedBy = GetRandomString(),
                ApprovalStatus = ApprovalStatus.Approved,
                IsPublished = true,
            };

        private static EventEnvelope<Association> CreateFactEnvelope(Association association) =>
            CreateFactEnvelope(association, securityContext: new SecurityContext());

        private static EventEnvelope<Association> CreateFactEnvelope(
            Association association,
            SecurityContext securityContext) =>
            new EventEnvelope<Association>
            {
                Content = association,
                SecurityContext = securityContext,
                Metadata = new EventMetadata { EventId = Guid.NewGuid() },
            };

        // The canonically visible version of a group: an id of its own, distinct from any id the
        // row carries, under the pinned content type.
        private static PublicContentItemGroup CreateVisibleVersion(Guid groupId) =>
            new PublicContentItemGroup(
                ContentItemId: Guid.NewGuid(),
                GroupId: groupId,
                ContentType: VisibleVersionContentType);

        // The one key the forwarder is to ask: the visible version's own type and id.
        private static List<ContentItemSettingKey> CreateSettingKeysFor(
            PublicContentItemGroup visibleVersion) =>
            new List<ContentItemSettingKey>
            {
                new ContentItemSettingKey
                {
                    ContentType = visibleVersion.ContentType,
                    ContentItemId = visibleVersion.ContentItemId,
                },
            };

        // The gather's answer for the visible version: its winning setting, with ShowReactions as
        // given.
        private static List<EffectiveContentItemSetting> CreateWinningSettingsFor(
            PublicContentItemGroup visibleVersion,
            bool showReactions) =>
            new List<EffectiveContentItemSetting>
            {
                CreateEffectiveSetting(
                    contentItemId: visibleVersion.ContentItemId,
                    contentType: visibleVersion.ContentType,
                    showReactions: showReactions),
            };

        private static EffectiveContentItemSetting CreateEffectiveSetting(
            Guid contentItemId,
            ContentType contentType,
            bool showReactions) =>
            new EffectiveContentItemSetting
            {
                ContentItemId = contentItemId,

                ContentItemSetting = new ContentItemSetting
                {
                    Id = Guid.NewGuid(),
                    ContentType = contentType,
                    ContentItemId = contentItemId,
                    ReactionsAllowed = true,
                    ShowReactions = showReactions,
                },
            };

        private static LiveUpdate CreateReactionsChangeFor(PublicContentItemGroup visibleVersion) =>
            new LiveUpdate
            {
                Type = LiveUpdateType.ContentItemReactionsChanged,
                ContentItemId = visibleVersion.ContentItemId,
            };

        // An override for one item: it names a content item of its own, so a message that took
        // the row's item would be seen to name one.
        private static ContentItemSetting CreateContentItemSettingOverride() =>
            new ContentItemSetting
            {
                Id = Guid.NewGuid(),
                ContentType = VisibleVersionContentType,
                ContentItemId = Guid.NewGuid(),
                ReactionsAllowed = true,
                ShowReactions = true,
            };

        private static EventEnvelope<ContentItemSetting> CreateSettingFactEnvelope(
            ContentItemSetting contentItemSetting) =>
            CreateSettingFactEnvelope(contentItemSetting, securityContext: new SecurityContext());

        private static EventEnvelope<ContentItemSetting> CreateSettingFactEnvelope(
            ContentItemSetting contentItemSetting,
            SecurityContext securityContext) =>
            new EventEnvelope<ContentItemSetting>
            {
                Content = contentItemSetting,
                SecurityContext = securityContext,
                Metadata = new EventMetadata { EventId = Guid.NewGuid() },
            };

        // The settings body's one message: it names no content item (LiveUpdates.md rule 5).
        private static LiveUpdate CreateSettingChange() =>
            new LiveUpdate
            {
                Type = LiveUpdateType.ContentItemSettingChanged,
                ContentItemId = null,
            };

        // the keys by value, whatever collection carries them
        private static Expression<Func<IReadOnlyList<ContentItemSettingKey>, bool>> SameSettingKeysAs(
            IReadOnlyList<ContentItemSettingKey> expectedSettingKeys) =>
            actualSettingKeys =>
                new CompareLogic(new ComparisonConfig { IgnoreObjectTypes = true })
                    .Compare(expectedSettingKeys, actualSettingKeys).AreEqual;

        private static Expression<Func<LiveUpdate, bool>> SameLiveUpdateAs(
            LiveUpdate expectedLiveUpdate) =>
            actualLiveUpdate =>
                new CompareLogic().Compare(expectedLiveUpdate, actualLiveUpdate).AreEqual;

        // THE CASES THE REACTIONS BODY WITHHOLDS (§SEC14.8 rules 1 and 3), one per kind. Every
        // reaction ear runs the one body #900 built, so every ear must withhold what it withholds.
        public enum WithheldReactionsCase
        {
            EndpointANotAContentItem,
            EndpointBNotAReaction,
            NoVisibleVersion,
            ReactionsHidden,
            SettingUnanswered
        }

        // The failures beneath the reactions body, one per family the ears map (§EVN11).
        public enum ReactionsBodyFailure
        {
            ContentItemValidation,
            ContentItemDependencyValidation,
            ContentItemDependency,
            ContentItemService,
            Gather,
            LiveUpdateValidation,
            LiveUpdateDependency,
            LiveUpdateService,
            Timeout
        }

        // The row a withheld case's fact carries. Every read beneath the body answers as a send
        // would need, save the one answer the case withholds on, so a body that skipped that one
        // check would send — and the test that expects nothing sent would see it.
        private Association ArrangeWithheldReactionsCase(
            WithheldReactionsCase withheldReactionsCase,
            CancellationToken cancellationToken)
        {
            Association contentItemReaction = CreateContentItemReaction();

            PublicContentItemGroup visibleVersion =
                CreateVisibleVersion(groupId: contentItemReaction.EntityAGroupId);

            List<ContentItemSettingKey> expectedSettingKeys = CreateSettingKeysFor(visibleVersion);

            PublicContentItemGroup foundVersion =
                withheldReactionsCase is WithheldReactionsCase.NoVisibleVersion
                    ? null
                    : visibleVersion;

            List<EffectiveContentItemSetting> winningSettings = withheldReactionsCase switch
            {
                WithheldReactionsCase.ReactionsHidden =>
                    CreateWinningSettingsFor(visibleVersion, showReactions: false),

                WithheldReactionsCase.SettingUnanswered =>
                    new List<EffectiveContentItemSetting>(),

                _ => CreateWinningSettingsFor(visibleVersion, showReactions: true),
            };

            if (withheldReactionsCase is WithheldReactionsCase.EndpointANotAContentItem)
            {
                contentItemReaction.EntityAType = EntityType.Comment;
            }

            if (withheldReactionsCase is WithheldReactionsCase.EndpointBNotAReaction)
            {
                contentItemReaction.EntityBType = EntityType.Tag;
            }

            this.contentItemServiceMock.Setup(service =>
                service.FindPublicContentItemGroupAsync(
                    contentItemReaction.EntityAGroupId,
                    cancellationToken))
                        .ReturnsAsync(foundVersion);

            this.accessBrokerMock.Setup(broker =>
                broker.RetrieveEffectiveContentItemSettingsAsync(
                    It.Is(SameSettingKeysAs(expectedSettingKeys)),
                    cancellationToken))
                        .ReturnsAsync(winningSettings);

            return contentItemReaction;
        }

        // Makes the read beneath the body fail as the family has it, and answers what the ear is
        // to rethrow (LiveUpdateOrchestrationService.Exceptions.cs): the foundations' refusals as
        // a dependency validation failure, their other failures and a timeout as a dependency
        // failure, and a gather's raw failure as a service failure, since a broker raises no
        // family of its own (§ARC12.5).
        private Xeption ArrangeReactionsBodyFailure(
            Association contentItemReaction,
            ReactionsBodyFailure reactionsBodyFailure,
            CancellationToken cancellationToken)
        {
            string randomMessage = GetRandomString();
            var innerException = new Xeption(message: randomMessage);

            PublicContentItemGroup visibleVersion =
                CreateVisibleVersion(groupId: contentItemReaction.EntityAGroupId);

            List<ContentItemSettingKey> expectedSettingKeys = CreateSettingKeysFor(visibleVersion);
            LiveUpdate expectedLiveUpdate = CreateReactionsChangeFor(visibleVersion);

            (Exception thrownException, Xeption expectedException) = reactionsBodyFailure switch
            {
                ReactionsBodyFailure.ContentItemValidation => (
                    new ContentItemValidationException(
                        message: randomMessage, innerException: innerException),
                    CreateDependencyValidationException(innerException)),

                ReactionsBodyFailure.ContentItemDependencyValidation => (
                    new ContentItemDependencyValidationException(
                        message: randomMessage, innerException: innerException),
                    CreateDependencyValidationException(innerException)),

                ReactionsBodyFailure.ContentItemDependency => (
                    new ContentItemDependencyException(
                        message: randomMessage, innerException: innerException),
                    CreateDependencyException(innerException)),

                ReactionsBodyFailure.ContentItemService => (
                    new ContentItemServiceException(
                        message: randomMessage, innerException: innerException),
                    CreateDependencyException(innerException)),

                ReactionsBodyFailure.Gather => CreateGatherFailure(randomMessage),

                ReactionsBodyFailure.LiveUpdateValidation => (
                    new LiveUpdateValidationException(
                        message: randomMessage, innerException: innerException),
                    CreateDependencyValidationException(innerException)),

                ReactionsBodyFailure.LiveUpdateDependency => (
                    new LiveUpdateDependencyException(
                        message: randomMessage, innerException: innerException),
                    CreateDependencyException(innerException)),

                ReactionsBodyFailure.LiveUpdateService => (
                    new LiveUpdateServiceException(
                        message: randomMessage, innerException: innerException),
                    CreateDependencyException(innerException)),

                _ => CreateTimeoutFailure(),
            };

            bool isReadFailure =
                reactionsBodyFailure is ReactionsBodyFailure.ContentItemValidation
                    or ReactionsBodyFailure.ContentItemDependencyValidation
                    or ReactionsBodyFailure.ContentItemDependency
                    or ReactionsBodyFailure.ContentItemService
                    or ReactionsBodyFailure.Timeout;

            if (isReadFailure)
            {
                this.contentItemServiceMock.Setup(service =>
                    service.FindPublicContentItemGroupAsync(
                        contentItemReaction.EntityAGroupId,
                        cancellationToken))
                            .ThrowsAsync(thrownException);

                return expectedException;
            }

            this.contentItemServiceMock.Setup(service =>
                service.FindPublicContentItemGroupAsync(
                    contentItemReaction.EntityAGroupId,
                    cancellationToken))
                        .ReturnsAsync(visibleVersion);

            if (reactionsBodyFailure is ReactionsBodyFailure.Gather)
            {
                this.accessBrokerMock.Setup(broker =>
                    broker.RetrieveEffectiveContentItemSettingsAsync(
                        It.Is(SameSettingKeysAs(expectedSettingKeys)),
                        cancellationToken))
                            .ThrowsAsync(thrownException);

                return expectedException;
            }

            this.accessBrokerMock.Setup(broker =>
                broker.RetrieveEffectiveContentItemSettingsAsync(
                    It.Is(SameSettingKeysAs(expectedSettingKeys)),
                    cancellationToken))
                        .ReturnsAsync(CreateWinningSettingsFor(visibleVersion, showReactions: true));

            this.liveUpdateServiceMock.Setup(service =>
                service.SendLiveUpdateAsync(
                    It.Is(SameLiveUpdateAs(expectedLiveUpdate)),
                    cancellationToken))
                        .ThrowsAsync(thrownException);

            return expectedException;
        }

        private static (Exception, Xeption) CreateGatherFailure(string randomMessage)
        {
            var gatherException = new Exception(randomMessage);

            var failedLiveUpdateOrchestrationServiceException =
                new FailedLiveUpdateOrchestrationServiceException(
                    message: "Failed live update orchestration service error occurred, " +
                        "please contact support.",
                    innerException: gatherException,
                    data: gatherException.Data);

            return (
                gatherException,
                new LiveUpdateOrchestrationServiceException(
                    message: "Live update orchestration service error occurred, contact support.",
                    innerException: failedLiveUpdateOrchestrationServiceException));
        }

        // The read is cancelled while the caller's own token is not: a timeout beneath the ear
        // rather than the caller giving up.
        private static (Exception, Xeption) CreateTimeoutFailure()
        {
            var timeoutException = new TimeoutException("The dependency operation timed out.");

            var timeoutLiveUpdateOrchestrationException =
                new TimeoutLiveUpdateOrchestrationException(
                    message: "Failed live update orchestration timeout error occurred, " +
                        "contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            return (
                new OperationCanceledException(),
                CreateDependencyException(timeoutLiveUpdateOrchestrationException));
        }

        private static string GetRandomString() =>
            new MnemonicString(wordCount: GetRandomNumber()).GetValue();

        private static int GetRandomNumber() =>
            new IntRange(min: 2, max: 10).GetValue();

        private static Expression<Func<Xeption, bool>> SameExceptionAs(Xeption expectedException) =>
            actualException => actualException.SameExceptionAs(expectedException);
    }
}
