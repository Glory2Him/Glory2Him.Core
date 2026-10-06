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
using System.Threading.Tasks;
using FluentAssertions;
using Force.DeepCloner;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Glory2Him.Core.Models.Orchestrations.Associations;
using Glory2Him.Core.Models.Orchestrations.Associations.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Associations
{
    public partial class AssociationOrchestrationServiceTests
    {
        [Theory]
        [InlineData(EntityType.ContentItem, EntityType.Reaction)]
        [InlineData(EntityType.Reaction, EntityType.ContentItem)]
        public async Task ShouldRemoveTheReadersReactionByItsPairAsync(
            EntityType entityAType,
            EntityType entityBType)
        {
            // given: a signed-in reader holding Love on an item withdraws Love from it. Their row
            // is found by its pair, as the reader, and soft-deleted by its id. The pair is the
            // upsert's caller shape, so it may name the reaction on either endpoint (§ARC16.8.1).
            string readerUserId = GetRandomString();
            this.ambientSecurityContext = CreateReaderSecurityContext(readerUserId);
            Association removalRequest = CreateRawUpsertRequestBetween(entityAType, entityBType);
            SetupInboundEnvelopeFor(removalRequest);
            ContentItem resolvedContentItem = SetupMethodPathEndpointReads(removalRequest);

            Association expectedLookupPair =
                CreateResolvedPairFrom(removalRequest, resolvedContentItem, readerUserId);

            var readersRow = new PersonalAssociationMatch
            {
                Id = Guid.NewGuid(),
                EntityBKeyId = GetNamedReactionId(removalRequest),
                IsDeleted = false,
            };

            var withdrawnRow = new Association
            {
                Id = readersRow.Id,
                IsDeleted = true,
            };

            this.associationServiceMock.Setup(service =>
                service.FindPersonalAssociationAsync(
                    It.Is(SameAssociationAs(expectedLookupPair)),
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(readersRow);

            this.associationServiceMock.Setup(service =>
                service.RemoveAssociationByIdAsync(
                    readersRow.Id,
                    null,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(withdrawnRow);

            var expectedResult = new AssociationRemovalResult
            {
                Status = AssociationRemovalStatus.Removed,
                AssociationId = readersRow.Id,
            };

            // when
            AssociationRemovalResult actualResult =
                await this.associationOrchestrationService.RemoveAssociationByPairAsync(
                    removalRequest,
                    TestContext.Current.CancellationToken);

            // then: the reader's row is withdrawn, with no deletion reason, and answered Removed
            // with its id and nothing else
            actualResult.Should().BeEquivalentTo(expectedResult);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(removalRequest),
                    Times.Once);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveContentItemByIdAsync(
                    resolvedContentItem.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.reactionServiceMock.Verify(service =>
                service.RetrieveReactionByIdAsync(
                    GetNamedReactionId(removalRequest),
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.associationServiceMock.Verify(service =>
                service.FindPersonalAssociationAsync(
                    It.Is(SameAssociationAs(expectedLookupPair)),
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.associationServiceMock.Verify(service =>
                service.RemoveAssociationByIdAsync(
                    readersRow.Id,
                    null,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.reactionServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldAnswerNothingToRemoveWhenTheReaderHoldsNoReactionAsync()
        {
            // given: the reader holds no reaction on the item, so the lookup finds no row
            string readerUserId = GetRandomString();
            this.ambientSecurityContext = CreateReaderSecurityContext(readerUserId);

            Association removalRequest =
                CreateRawUpsertRequestBetween(EntityType.ContentItem, EntityType.Reaction);

            Association expectedLookupPair = SetupReadersWithdrawal(removalRequest, readerUserId);

            this.associationServiceMock.Setup(service =>
                service.FindPersonalAssociationAsync(
                    It.Is(SameAssociationAs(expectedLookupPair)),
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync((PersonalAssociationMatch)null);

            var expectedResult = new AssociationRemovalResult
            {
                Status = AssociationRemovalStatus.NothingToRemove,
                AssociationId = null,
            };

            // when
            AssociationRemovalResult actualResult =
                await this.associationOrchestrationService.RemoveAssociationByPairAsync(
                    removalRequest,
                    TestContext.Current.CancellationToken);

            // then: nothing is removed, and no id is answered
            actualResult.Should().BeEquivalentTo(expectedResult);

            this.associationServiceMock.Verify(service =>
                service.FindPersonalAssociationAsync(
                    It.Is(SameAssociationAs(expectedLookupPair)),
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldLeaveAnotherHeldReactionAloneAsync()
        {
            // given: the reader holds Joy on the item and withdraws Love. The lookup finds their
            // one row on the item whichever reaction it holds, so the row it answers holds Joy.
            string readerUserId = GetRandomString();
            this.ambientSecurityContext = CreateReaderSecurityContext(readerUserId);

            Association removalRequest =
                CreateRawUpsertRequestBetween(EntityType.ContentItem, EntityType.Reaction);

            Association expectedLookupPair = SetupReadersWithdrawal(removalRequest, readerUserId);

            var readersJoyRow = new PersonalAssociationMatch
            {
                Id = Guid.NewGuid(),
                EntityBKeyId = Guid.NewGuid(),
                IsDeleted = false,
            };

            this.associationServiceMock.Setup(service =>
                service.FindPersonalAssociationAsync(
                    It.Is(SameAssociationAs(expectedLookupPair)),
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(readersJoyRow);

            var expectedResult = new AssociationRemovalResult
            {
                Status = AssociationRemovalStatus.NothingToRemove,
                AssociationId = null,
            };

            // when
            AssociationRemovalResult actualResult =
                await this.associationOrchestrationService.RemoveAssociationByPairAsync(
                    removalRequest,
                    TestContext.Current.CancellationToken);

            // then: the Joy row is left as it is, and no id is answered
            actualResult.Should().BeEquivalentTo(expectedResult);

            this.associationServiceMock.Verify(service =>
                service.FindPersonalAssociationAsync(
                    It.Is(SameAssociationAs(expectedLookupPair)),
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldAnswerNothingToRemoveForAnAlreadyWithdrawnRowAsync()
        {
            // given: the reader's row holding the reaction they name is already withdrawn — by
            // them, or taken down by a moderator, which the lookup does not tell apart. Withdrawing
            // again changes nothing: the member is idempotent.
            string readerUserId = GetRandomString();
            this.ambientSecurityContext = CreateReaderSecurityContext(readerUserId);

            Association removalRequest =
                CreateRawUpsertRequestBetween(EntityType.ContentItem, EntityType.Reaction);

            Association expectedLookupPair = SetupReadersWithdrawal(removalRequest, readerUserId);

            var withdrawnRow = new PersonalAssociationMatch
            {
                Id = Guid.NewGuid(),
                EntityBKeyId = removalRequest.EntityBKeyId,
                IsDeleted = true,
            };

            this.associationServiceMock.Setup(service =>
                service.FindPersonalAssociationAsync(
                    It.Is(SameAssociationAs(expectedLookupPair)),
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(withdrawnRow);

            var expectedResult = new AssociationRemovalResult
            {
                Status = AssociationRemovalStatus.NothingToRemove,
                AssociationId = null,
            };

            // when
            AssociationRemovalResult actualResult =
                await this.associationOrchestrationService.RemoveAssociationByPairAsync(
                    removalRequest,
                    TestContext.Current.CancellationToken);

            // then: nothing is removed, and no id is answered
            actualResult.Should().BeEquivalentTo(expectedResult);

            this.associationServiceMock.Verify(service =>
                service.FindPersonalAssociationAsync(
                    It.Is(SameAssociationAs(expectedLookupPair)),
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ReadOnlyRolesOverAReaction))]
        public async Task ShouldRemoveTheReadersReactionWhateverReadOnlyRoleTheyHoldAsync(
            string readOnlyRole)
        {
            // given: a reader under a read-only role withdraws their reaction. A reaction is not a
            // contribution, and the far end's type says the pair is personal before anything is
            // read, so this layer asks none of the read-only roles (§SEC14.7 posture A′ rules 1
            // and 4).
            string readerUserId = GetRandomString();
            this.ambientSecurityContext = CreateReaderSecurityContext(readerUserId, readOnlyRole);

            Association removalRequest =
                CreateRawUpsertRequestBetween(EntityType.ContentItem, EntityType.Reaction);

            Association expectedLookupPair = SetupReadersWithdrawal(removalRequest, readerUserId);

            var readersRow = new PersonalAssociationMatch
            {
                Id = Guid.NewGuid(),
                EntityBKeyId = removalRequest.EntityBKeyId,
                IsDeleted = false,
            };

            this.associationServiceMock.Setup(service =>
                service.FindPersonalAssociationAsync(
                    It.Is(SameAssociationAs(expectedLookupPair)),
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(readersRow);

            this.associationServiceMock.Setup(service =>
                service.RemoveAssociationByIdAsync(
                    readersRow.Id,
                    null,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(new Association { Id = readersRow.Id, IsDeleted = true });

            var expectedResult = new AssociationRemovalResult
            {
                Status = AssociationRemovalStatus.Removed,
                AssociationId = readersRow.Id,
            };

            // when
            AssociationRemovalResult actualResult =
                await this.associationOrchestrationService.RemoveAssociationByPairAsync(
                    removalRequest,
                    TestContext.Current.CancellationToken);

            // then
            actualResult.Should().BeEquivalentTo(expectedResult);

            this.associationServiceMock.Verify(service =>
                service.FindPersonalAssociationAsync(
                    It.Is(SameAssociationAs(expectedLookupPair)),
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.associationServiceMock.Verify(service =>
                service.RemoveAssociationByIdAsync(
                    readersRow.Id,
                    null,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldWithdrawOnlyTheCallersOwnReactionAsync()
        {
            // given: the request names another reader. UserId comes from the envelope and never
            // from the request, and the claim is overwritten without comment (§DOM4.10 rules 1
            // and 2), so the lookup is asked for the caller's own row and another reader's row is
            // never reached: a withdrawal is keyed on (content item, reaction, caller) (§ARC16.8).
            string readerUserId = GetRandomString();
            this.ambientSecurityContext = CreateReaderSecurityContext(readerUserId);

            Association removalRequest =
                CreateRawUpsertRequestBetween(EntityType.ContentItem, EntityType.Reaction);

            string anotherReaderUserId = $"another-reader-{Guid.NewGuid()}";
            removalRequest.UserId = anotherReaderUserId;
            Association expectedLookupPair = SetupReadersWithdrawal(removalRequest, readerUserId);

            var readersRow = new PersonalAssociationMatch
            {
                Id = Guid.NewGuid(),
                EntityBKeyId = removalRequest.EntityBKeyId,
                IsDeleted = false,
            };

            this.associationServiceMock.Setup(service =>
                service.FindPersonalAssociationAsync(
                    It.Is(SameAssociationAs(expectedLookupPair)),
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(readersRow);

            this.associationServiceMock.Setup(service =>
                service.RemoveAssociationByIdAsync(
                    readersRow.Id,
                    null,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(new Association { Id = readersRow.Id, IsDeleted = true });

            var expectedResult = new AssociationRemovalResult
            {
                Status = AssociationRemovalStatus.Removed,
                AssociationId = readersRow.Id,
            };

            // when
            AssociationRemovalResult actualResult =
                await this.associationOrchestrationService.RemoveAssociationByPairAsync(
                    removalRequest,
                    TestContext.Current.CancellationToken);

            // then: the caller's own row is withdrawn, and nothing is said about the claim
            actualResult.Should().BeEquivalentTo(expectedResult);

            this.associationServiceMock.Verify(service =>
                service.FindPersonalAssociationAsync(
                    It.Is(SameAssociationAs(expectedLookupPair)),
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.associationServiceMock.Verify(service =>
                service.RemoveAssociationByIdAsync(
                    readersRow.Id,
                    null,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldWithdrawWithoutAskingTheItemsSettingAsync()
        {
            // given: the item's setting no longer allows reactions. The facet gate judges what may
            // be given, and withdrawing is never gated (§ARC16.2.1), so a reader can always take
            // back a reaction they gave before the setting changed.
            string readerUserId = GetRandomString();
            this.ambientSecurityContext = CreateReaderSecurityContext(readerUserId);

            Association removalRequest =
                CreateRawUpsertRequestBetween(EntityType.ContentItem, EntityType.Reaction);

            SetupInboundEnvelopeFor(removalRequest);
            ContentItem resolvedContentItem = SetupMethodPathEndpointReads(removalRequest);

            Association expectedLookupPair =
                CreateResolvedPairFrom(removalRequest, resolvedContentItem, readerUserId);

            ContentItemSetting refusingSetting =
                CreateAllowingContentItemSetting(resolvedContentItem.ContentType);

            refusingSetting.ReactionsAllowed = false;

            this.accessBrokerMock.Setup(broker =>
                broker.RetrieveEffectiveContentItemSettingsAsync(
                    It.Is(SameSettingKeysAs(CreateSettingKeysFor(resolvedContentItem))),
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(new List<EffectiveContentItemSetting>
                        {
                            new EffectiveContentItemSetting
                            {
                                ContentItemId = resolvedContentItem.Id,
                                ContentItemSetting = refusingSetting,
                            },
                        });

            var readersRow = new PersonalAssociationMatch
            {
                Id = Guid.NewGuid(),
                EntityBKeyId = removalRequest.EntityBKeyId,
                IsDeleted = false,
            };

            this.associationServiceMock.Setup(service =>
                service.FindPersonalAssociationAsync(
                    It.Is(SameAssociationAs(expectedLookupPair)),
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(readersRow);

            this.associationServiceMock.Setup(service =>
                service.RemoveAssociationByIdAsync(
                    readersRow.Id,
                    null,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(new Association { Id = readersRow.Id, IsDeleted = true });

            var expectedResult = new AssociationRemovalResult
            {
                Status = AssociationRemovalStatus.Removed,
                AssociationId = readersRow.Id,
            };

            // when
            AssociationRemovalResult actualResult =
                await this.associationOrchestrationService.RemoveAssociationByPairAsync(
                    removalRequest,
                    TestContext.Current.CancellationToken);

            // then: withdrawn, and the setting is never asked
            actualResult.Should().BeEquivalentTo(expectedResult);

            this.associationServiceMock.Verify(service =>
                service.RemoveAssociationByIdAsync(
                    readersRow.Id,
                    null,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.accessBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // An anonymous caller in both shapes: no security context at all, and one that is not
        // signed in.
        public static TheoryData<SecurityContext> UnauthenticatedWithdrawals() =>
            new TheoryData<SecurityContext>
            {
                null,
                new SecurityContext { IsAuthenticated = false, Roles = Array.Empty<string>() },
            };

        [Theory]
        [MemberData(nameof(UnauthenticatedWithdrawals))]
        public async Task ShouldThrowValidationExceptionOnRemoveByPairIfUserIsNotAuthenticatedAndLogItAsync(
            SecurityContext unauthenticatedSecurityContext)
        {
            // given: an anonymous caller owns no reaction to withdraw, so it is refused before
            // anything is read
            this.ambientSecurityContext = unauthenticatedSecurityContext;

            Association removalRequest =
                CreateRawUpsertRequestBetween(EntityType.ContentItem, EntityType.Reaction);

            SetupInboundEnvelopeFor(removalRequest);

            var unauthorizedAssociationOrchestrationException =
                new UnauthorizedAssociationOrchestrationException(
                    message: "The current user is not authenticated.");

            var expectedValidationException =
                new AssociationOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: unauthorizedAssociationOrchestrationException);

            // when
            ValueTask<AssociationRemovalResult> removeTask =
                this.associationOrchestrationService.RemoveAssociationByPairAsync(
                    removalRequest,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(removeTask.AsTask);

            // then: refused before any endpoint or row is read
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(removalRequest),
                    Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.reactionServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // A signed-in reader's withdrawal: the envelope and both endpoint reads. Hands back the
        // pair the lookup is to be handed.
        private Association SetupReadersWithdrawal(Association removalRequest, string readerUserId)
        {
            SetupInboundEnvelopeFor(removalRequest);
            ContentItem resolvedContentItem = SetupMethodPathEndpointReads(removalRequest);

            return CreateResolvedPairFrom(removalRequest, resolvedContentItem, readerUserId);
        }

        // The reaction a withdrawal request names: the key of its Reaction endpoint, on whichever
        // side the request puts it.
        private static Guid GetNamedReactionId(Association removalRequest) =>
            removalRequest.EntityAType == EntityType.Reaction
                ? removalRequest.EntityAKeyId
                : removalRequest.EntityBKeyId;

        // A reader's reaction on an item as the withdrawal hands it to the lookup: the raw request
        // in the order it was sent, with the item at its group under AllVersions with its content
        // type, the reaction at its own id under ThisVersionOnly with none, and the reader's own
        // user id. Unlike the upsert's, it carries no status: a withdrawal writes none.
        private static Association CreateResolvedPairFrom(
            Association removalRequest,
            ContentItem resolvedContentItem,
            string readerUserId)
        {
            Association resolvedPair = removalRequest.DeepClone();
            bool isTheItemOnA = removalRequest.EntityAType == EntityType.ContentItem;

            resolvedPair.EntityAGroupId =
                isTheItemOnA ? resolvedContentItem.GroupId : removalRequest.EntityAKeyId;

            resolvedPair.EntityAContentType = isTheItemOnA ? resolvedContentItem.ContentType : null;
            resolvedPair.EntityAScope = isTheItemOnA ? Scope.AllVersions : Scope.ThisVersionOnly;

            resolvedPair.EntityBGroupId =
                isTheItemOnA ? removalRequest.EntityBKeyId : resolvedContentItem.GroupId;

            resolvedPair.EntityBContentType = isTheItemOnA ? null : resolvedContentItem.ContentType;
            resolvedPair.EntityBScope = isTheItemOnA ? Scope.ThisVersionOnly : Scope.AllVersions;
            resolvedPair.UserId = readerUserId;

            return resolvedPair;
        }
    }
}
