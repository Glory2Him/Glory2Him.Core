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
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Glory2Him.Core.Models.Foundations.Tags.Exceptions;
using Glory2Him.Core.Models.Orchestrations.Associations;
using Glory2Him.Core.Models.Orchestrations.Associations.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Associations
{
    public partial class AssociationOrchestrationServiceTests
    {
        public static TheoryData<SecurityContext?> UnauthenticatedSecurityContexts() =>
            new TheoryData<SecurityContext?>
            {
                null,
                new SecurityContext { IsAuthenticated = false, Roles = Array.Empty<string>() },
            };

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowValidationExceptionOnUpsertIfUserIsNotAuthenticatedAndLogItAsync(
            SecurityContext? unauthenticatedSecurityContext)
        {
            // given
            this.ambientSecurityContext = unauthenticatedSecurityContext!;

            Association rawRequest = CreateRawAddRequest();

            var unauthorizedAssociationOrchestrationException =
                new UnauthorizedAssociationOrchestrationException(
                    message: "The current user is not authenticated.");

            var expectedValidationException =
                new AssociationOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: unauthorizedAssociationOrchestrationException);

            // when
            ValueTask<AssociationSuggestionResult> upsertTask =
                this.associationOrchestrationService.UpsertAssociationAsync(
                    rawRequest,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(upsertTask.AsTask);

            // then: refused before any endpoint is read or any row looked up
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnUpsertIfCallerIsBlockedFromContributingAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.ReadOnly);

            Association rawRequest = CreateRawAddRequest();

            var unauthorizedAssociationOrchestrationException =
                new UnauthorizedAssociationOrchestrationException(
                    message: "The current user is blocked from contributing content item associations.");

            var expectedValidationException =
                new AssociationOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: unauthorizedAssociationOrchestrationException);

            // when
            ValueTask<AssociationSuggestionResult> upsertTask =
                this.associationOrchestrationService.UpsertAssociationAsync(
                    rawRequest,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(upsertTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnUpsertIfAssociationIsNullAndLogItAsync()
        {
            // given
            Association nullAssociation = null;

            var nullAssociationOrchestrationException =
                new NullAssociationOrchestrationException(
                    message: "Content item association is null.");

            var expectedValidationException =
                new AssociationOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: nullAssociationOrchestrationException);

            // when
            ValueTask<AssociationSuggestionResult> upsertTask =
                this.associationOrchestrationService.UpsertAssociationAsync(
                    nullAssociation,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(upsertTask.AsTask);

            // then: null is caught before the envelope is even created
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnUpsertIfAnEndpointKeyIsEmptyAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Association rawRequest = CreateRawAddRequest();
            rawRequest.EntityBKeyId = Guid.Empty;

            var invalidAssociationOrchestrationException =
                new InvalidAssociationOrchestrationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationOrchestrationException.AddData(
                key: nameof(Association.EntityBKeyId),
                values: "Id is required");

            var expectedValidationException =
                new AssociationOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidAssociationOrchestrationException);

            // when
            ValueTask<AssociationSuggestionResult> upsertTask =
                this.associationOrchestrationService.UpsertAssociationAsync(
                    rawRequest,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(upsertTask.AsTask);

            // then: rejected before any endpoint is resolved
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnUpsertIfAnEndpointDoesNotExistAndLogItAsync()
        {
            // given: the endpoint's own service reports a missing/non-visible row as a validation
            // failure; the orchestration turns that into a not-found endpoint, never re-surfacing
            // the ContentItem's own exception type.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Association rawRequest = CreateRawAddRequest();

            var contentItemValidationException =
                new ContentItemValidationException(
                    message: "not found",
                    innerException: new Xeption());

            var notFoundAssociationOrchestrationException =
                new NotFoundAssociationOrchestrationException(
                    message: "The A endpoint was not found.");

            var expectedValidationException =
                new AssociationOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: notFoundAssociationOrchestrationException);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(
                    rawRequest.EntityAKeyId,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(contentItemValidationException);

            // when
            ValueTask<AssociationSuggestionResult> upsertTask =
                this.associationOrchestrationService.UpsertAssociationAsync(
                    rawRequest,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(upsertTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.associationServiceMock.Verify(service =>
                service.FindAssociationByPairAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowNotFoundForTheBEndpointWhenTheSecondEndpointDoesNotExistAndLogItAsync()
        {
            // given: the A endpoint resolves but the B endpoint's service reports it missing. The
            // failure must be attributed to the B endpoint by name (not A), and reported as a
            // not-found endpoint rather than the Tag's own exception type.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Association rawRequest = CreateRawAddRequest();

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(
                    rawRequest.EntityAKeyId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new ContentItem
                        {
                            Id = rawRequest.EntityAKeyId,
                            GroupId = Guid.NewGuid(),
                            ContentType = ContentType.Story,
                        });

            this.tagServiceMock.Setup(service =>
                service.RetrieveTagByIdAsync(
                    rawRequest.EntityBKeyId,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new TagValidationException(
                            message: "not found",
                            innerException: new Xeption()));

            var notFoundAssociationOrchestrationException =
                new NotFoundAssociationOrchestrationException(
                    message: "The B endpoint was not found.");

            var expectedValidationException =
                new AssociationOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: notFoundAssociationOrchestrationException);

            // when
            ValueTask<AssociationSuggestionResult> upsertTask =
                this.associationOrchestrationService.UpsertAssociationAsync(
                    rawRequest,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(upsertTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.associationServiceMock.Verify(service =>
                service.FindAssociationByPairAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnUpsertIfAnEndpointTypeIsUnsupportedAndLogItAsync()
        {
            // given: Attachment has no foundation service yet — it cannot be an endpoint
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Association rawRequest = CreateRawAddRequest();
            rawRequest.EntityAType = EntityType.Attachment;

            var invalidAssociationOrchestrationException =
                new InvalidAssociationOrchestrationException(
                    message: "Entity type Attachment is not supported as an association endpoint.");

            var expectedValidationException =
                new AssociationOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidAssociationOrchestrationException);

            // when
            ValueTask<AssociationSuggestionResult> upsertTask =
                this.associationOrchestrationService.UpsertAssociationAsync(
                    rawRequest,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(upsertTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.associationServiceMock.Verify(service =>
                service.FindAssociationByPairAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // Each far end the gate maps, with the switch it asks of the ContentItem host's winning
        // setting (§ARC16.2.1's table). There is no Attachment row: no Attachment endpoint
        // resolves yet, so no pair reaches that switch (AssociationOrchestrationService.md §1
        // rule 2). The last case holds the item on B, so the gate is held in both orientations.
        public static TheoryData<EntityType, EntityType, string> FacetsRefusedBySetting() =>
            new TheoryData<EntityType, EntityType, string>
            {
                { EntityType.ContentItem, EntityType.Tag, nameof(ContentItemSetting.TagsAllowed) },
                { EntityType.ContentItem, EntityType.Reaction, nameof(ContentItemSetting.ReactionsAllowed) },
                { EntityType.ContentItem, EntityType.Comment, nameof(ContentItemSetting.CommentsAllowed) },

                {
                    EntityType.ContentItem,
                    EntityType.BibleReference,
                    nameof(ContentItemSetting.BibleReferenceAllowed)
                },

                { EntityType.ContentItem, EntityType.Link, nameof(ContentItemSetting.LinksAllowed) },
                { EntityType.Reaction, EntityType.ContentItem, nameof(ContentItemSetting.ReactionsAllowed) },
            };

        [Theory]
        [MemberData(nameof(FacetsRefusedBySetting))]
        public async Task ShouldThrowValidationExceptionOnUpsertIfTheSettingRefusesTheFacetAndLogItAsync(
            EntityType entityAType,
            EntityType entityBType,
            string refusingSwitch)
        {
            // given: the item's winning setting allows every facet but the one its far end names,
            // so only that switch can refuse the pair. The caller claims a content type the item
            // does not carry, and the setting is asked under the one resolution derives.
            Association rawRequest = CreateRawUpsertRequestBetween(entityAType, entityBType);
            ContentItem resolvedContentItem = SetupMethodPathEndpointReads(rawRequest);
            rawRequest.EntityAContentType = ContentType.Testimony;
            rawRequest.EntityBContentType = ContentType.Testimony;

            ContentItemSetting refusingSetting =
                CreateAllowingContentItemSetting(resolvedContentItem.ContentType);

            typeof(ContentItemSetting).GetProperty(refusingSwitch).SetValue(refusingSetting, false);
            List<ContentItemSettingKey> expectedSettingKeys = CreateSettingKeysFor(resolvedContentItem);

            this.accessBrokerMock.Setup(broker =>
                broker.RetrieveEffectiveContentItemSettingsAsync(
                    It.Is(SameSettingKeysAs(expectedSettingKeys)),
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(new List<EffectiveContentItemSetting>
                        {
                            new EffectiveContentItemSetting
                            {
                                ContentItemId = resolvedContentItem.Id,
                                ContentItemSetting = refusingSetting,
                            },
                        });

            var invalidAssociationOrchestrationException =
                new InvalidAssociationOrchestrationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationOrchestrationException.AddData(
                key: refusingSwitch,
                values: "Value does not allow this association");

            var expectedValidationException =
                new AssociationOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidAssociationOrchestrationException);

            // when
            ValueTask<AssociationSuggestionResult> upsertTask =
                this.associationOrchestrationService.UpsertAssociationAsync(
                    rawRequest,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(
                    upsertTask.AsTask);

            // then: refused by the switch it names, and nothing is probed or written
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.accessBrokerMock.Verify(broker =>
                broker.RetrieveEffectiveContentItemSettingsAsync(
                    It.Is(SameSettingKeysAs(expectedSettingKeys)),
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.associationServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData("Moved")]
        [InlineData("Lovely")]
        [InlineData("Beloved")]
        public async Task ShouldThrowValidationExceptionOnUpsertIfTheItemIsLimitedToLoveAndLogItAsync(
            string reactionName)
        {
            // given: the item allows reactions but is limited to Love, and the reaction given is
            // another one. Two of them hold "love" inside a longer name, so the match is on the
            // whole name rather than a part of it.
            Association rawRequest =
                CreateRawUpsertRequestBetween(EntityType.ContentItem, EntityType.Reaction);

            ContentItem resolvedContentItem =
                SetupMethodPathEndpointReads(rawRequest, reactionName);

            ContentItemSetting loveOnlySetting =
                CreateAllowingContentItemSetting(resolvedContentItem.ContentType);

            loveOnlySetting.LimitReactionsToLoveOnly = true;
            List<ContentItemSettingKey> expectedSettingKeys = CreateSettingKeysFor(resolvedContentItem);

            this.accessBrokerMock.Setup(broker =>
                broker.RetrieveEffectiveContentItemSettingsAsync(
                    It.Is(SameSettingKeysAs(expectedSettingKeys)),
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(new List<EffectiveContentItemSetting>
                        {
                            new EffectiveContentItemSetting
                            {
                                ContentItemId = resolvedContentItem.Id,
                                ContentItemSetting = loveOnlySetting,
                            },
                        });

            var invalidAssociationOrchestrationException =
                new InvalidAssociationOrchestrationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationOrchestrationException.AddData(
                key: nameof(ContentItemSetting.LimitReactionsToLoveOnly),
                values: "Value allows only the Love reaction");

            var expectedValidationException =
                new AssociationOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidAssociationOrchestrationException);

            // when
            ValueTask<AssociationSuggestionResult> upsertTask =
                this.associationOrchestrationService.UpsertAssociationAsync(
                    rawRequest,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(
                    upsertTask.AsTask);

            // then: refused by the narrowing it names, and nothing is probed or written
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.accessBrokerMock.Verify(broker =>
                broker.RetrieveEffectiveContentItemSettingsAsync(
                    It.Is(SameSettingKeysAs(expectedSettingKeys)),
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.associationServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldNotNarrowAFarEndOtherThanAReactionOnAnItemLimitedToLoveAsync()
        {
            // given: the negative control the narrowing needs. Limiting an item to Love narrows
            // its reactions and nothing else (§ARC16.2.1), so a tag on that item is asked only
            // TagsAllowed, which is on. A narrowing that ignored the far end's type would refuse it.
            Association rawRequest = CreateRawUpsertRequestBetween(EntityType.ContentItem, EntityType.Tag);
            ContentItem resolvedContentItem = SetupMethodPathEndpointReads(rawRequest);

            ContentItemSetting loveOnlySetting =
                CreateAllowingContentItemSetting(resolvedContentItem.ContentType);

            loveOnlySetting.LimitReactionsToLoveOnly = true;
            List<ContentItemSettingKey> expectedSettingKeys = CreateSettingKeysFor(resolvedContentItem);

            this.accessBrokerMock.Setup(broker =>
                broker.RetrieveEffectiveContentItemSettingsAsync(
                    It.Is(SameSettingKeysAs(expectedSettingKeys)),
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(new List<EffectiveContentItemSetting>
                        {
                            new EffectiveContentItemSetting
                            {
                                ContentItemId = resolvedContentItem.Id,
                                ContentItemSetting = loveOnlySetting,
                            },
                        });

            var insertedId = Guid.NewGuid();

            this.associationServiceMock.Setup(service =>
                service.AddAssociationAsync(
                    It.IsAny<Association>(),
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync((Association association, CancellationToken _) =>
                        {
                            association.Id = insertedId;

                            return association;
                        });

            // when
            AssociationSuggestionResult actualResult =
                await this.associationOrchestrationService.UpsertAssociationAsync(
                    rawRequest,
                    TestContext.Current.CancellationToken);

            // then: admitted, and the free pair inserted
            actualResult.Status.Should().Be(AssociationSuggestionStatus.Created);
            actualResult.AssociationId.Should().Be(insertedId);

            this.accessBrokerMock.Verify(broker =>
                broker.RetrieveEffectiveContentItemSettingsAsync(
                    It.Is(SameSettingKeysAs(expectedSettingKeys)),
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.accessBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnUpsertIfTheItemsSettingDoesNotResolveAndLogItAsync()
        {
            // given: no setting resolves for the item under its type. The broker answers per key,
            // and its answer here holds one allowing row for each term of the key that misses on
            // that term alone — the same item under another type, and another item under the
            // same type — so a gate that took a row for any other key would admit the pair.
            Association rawRequest = CreateRawUpsertRequestBetween(EntityType.ContentItem, EntityType.Tag);
            ContentItem resolvedContentItem = SetupMethodPathEndpointReads(rawRequest);
            List<ContentItemSettingKey> expectedSettingKeys = CreateSettingKeysFor(resolvedContentItem);

            var nearMissSettings = new List<EffectiveContentItemSetting>
            {
                new EffectiveContentItemSetting
                {
                    ContentItemId = resolvedContentItem.Id,
                    ContentItemSetting = CreateAllowingContentItemSetting(ContentType.Testimony),
                },

                new EffectiveContentItemSetting
                {
                    ContentItemId = Guid.NewGuid(),
                    ContentItemSetting = CreateAllowingContentItemSetting(resolvedContentItem.ContentType),
                },
            };

            this.accessBrokerMock.Setup(broker =>
                broker.RetrieveEffectiveContentItemSettingsAsync(
                    It.Is(SameSettingKeysAs(expectedSettingKeys)),
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(nearMissSettings);

            var invalidAssociationOrchestrationException =
                new InvalidAssociationOrchestrationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationOrchestrationException.AddData(
                key: nameof(ContentItemSetting.TagsAllowed),
                values: "Value could not be resolved");

            var expectedValidationException =
                new AssociationOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidAssociationOrchestrationException);

            // when
            ValueTask<AssociationSuggestionResult> upsertTask =
                this.associationOrchestrationService.UpsertAssociationAsync(
                    rawRequest,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(
                    upsertTask.AsTask);

            // then: the gate never falls open — refused, and nothing is probed or written
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.accessBrokerMock.Verify(broker =>
                broker.RetrieveEffectiveContentItemSettingsAsync(
                    It.Is(SameSettingKeysAs(expectedSettingKeys)),
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.associationServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldNotGateABibleReferenceHostAsync()
        {
            // given: a tag on a passage. A BibleReference host's settings entity is not built, so
            // the gate has nothing to ask of it, and a Tag has no settings entity at all
            // (§ARC16.2.1, the BibleReference host is a gap). No setting is read for the pair.
            Association rawRequest =
                CreateRawUpsertRequestBetween(EntityType.BibleReference, EntityType.Tag);

            SetupMethodPathEndpointReads(rawRequest);
            var insertedId = Guid.NewGuid();

            this.associationServiceMock.Setup(service =>
                service.AddAssociationAsync(
                    It.IsAny<Association>(),
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync((Association association, CancellationToken _) =>
                        {
                            association.Id = insertedId;

                            return association;
                        });

            // when
            AssociationSuggestionResult actualResult =
                await this.associationOrchestrationService.UpsertAssociationAsync(
                    rawRequest,
                    TestContext.Current.CancellationToken);

            // then: written as the add writes a free pair, with no setting asked
            actualResult.Status.Should().Be(AssociationSuggestionStatus.Created);
            actualResult.AssociationId.Should().Be(insertedId);

            this.accessBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldNotGateAContentItemFarEndAsync()
        {
            // given: two items paired, as in series membership. Each is a ContentItem host, but its
            // far end is a ContentItem too, which maps no switch, so neither orientation is gated
            // and no setting is read (the task's edge case; §ARC16.2.1's table).
            Association rawRequest =
                CreateRawUpsertRequestBetween(EntityType.ContentItem, EntityType.ContentItem);

            SetupMethodPathEndpointReads(rawRequest);
            var insertedId = Guid.NewGuid();

            this.associationServiceMock.Setup(service =>
                service.AddAssociationAsync(
                    It.IsAny<Association>(),
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync((Association association, CancellationToken _) =>
                        {
                            association.Id = insertedId;

                            return association;
                        });

            // when
            AssociationSuggestionResult actualResult =
                await this.associationOrchestrationService.UpsertAssociationAsync(
                    rawRequest,
                    TestContext.Current.CancellationToken);

            // then
            actualResult.Status.Should().Be(AssociationSuggestionStatus.Created);
            actualResult.AssociationId.Should().Be(insertedId);

            this.accessBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
