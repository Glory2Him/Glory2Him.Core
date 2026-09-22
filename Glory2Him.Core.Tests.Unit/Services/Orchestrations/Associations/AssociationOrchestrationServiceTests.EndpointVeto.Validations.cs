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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.Associations.Exceptions;
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Orchestrations.Associations;
using Glory2Him.Core.Models.Orchestrations.Associations.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Associations
{
    public partial class AssociationOrchestrationServiceTests
    {
        // Four scoped names are in play on every write, two per endpoint: each end's
        // %EntityType%-ReadOnly and each ContentItem end's narrow ContentItem-%ContentType%-ReadOnly
        // (§SEC14.7 posture A′ rule 1). ANY ONE of them refuses, and the cases below exercise
        // both ends and both tiers — a block on one end alone, held alongside a review role on
        // the other, must still refuse.
        public static TheoryData<string, string[]> BlockedEndpointCases() =>
            new TheoryData<string, string[]>
            {
                // the worked case from the criterion: Tag-ReadOnly alongside
                // BibleReference-Reviewers cannot pair a tag with a scripture page. The role they
                // hold on one end does not buy them the end they are banned from.
                {
                    BibleReferenceTagPairing,
                    new[]
                    {
                        Roles.ReadOnlyFor(EntityType.Tag),
                        Roles.ReviewersFor(EntityType.BibleReference),
                    }
                },

                // and the same pairing the other way round — the OR runs in both directions
                {
                    BibleReferenceTagPairing,
                    new[]
                    {
                        Roles.ReadOnlyFor(EntityType.BibleReference),
                        Roles.ReviewersFor(EntityType.Tag),
                    }
                },

                // the NARROW tier, composed from the content type the orchestration derived off
                // the resolved endpoint — never off the caller's copy, which is why it may be
                // trusted here at all
                {
                    ContentItemTagPairing,
                    new[]
                    {
                        Roles.ReadOnlyFor(EntityType.ContentItem, ContentType.Story),
                        Roles.PublishersFor(EntityType.Tag),
                    }
                },

                // the coarse tier on the same pairing
                {
                    ContentItemTagPairing,
                    new[] { Roles.ReadOnlyFor(EntityType.ContentItem) }
                },
            };

        [Theory]
        [MemberData(nameof(BlockedEndpointCases))]
        public async Task ShouldThrowValidationExceptionOnAddIfEitherEndpointIsBlockedForTheCallerAsync(
            string pairing,
            string[] callerRoles)
        {
            // given: the add is the one write that resolves BOTH endpoints from storage as its
            // own first act, so the endpoint half of the veto is decidable here and is decided
            // here — the orchestration's OWN refusal, mapped to its own validation exception
            // (§SEC14.7 posture A′ rule 4, "the add is the exception that proves the rule").
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(callerRoles);

            Association rawRequest = SetupPairingEndpointReads(pairing);

            var unauthorizedAssociationOrchestrationException =
                new UnauthorizedAssociationOrchestrationException(
                    message: "The current user is blocked from contributing content item associations.");

            var expectedValidationException =
                new AssociationOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: unauthorizedAssociationOrchestrationException);

            // when
            ValueTask<AssociationSuggestionResult> addTask =
                this.associationOrchestrationService.AddAssociationAsync(
                    rawRequest,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(
                    addTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            // refused before the pair probe, so a blocked caller cannot use the add to learn
            // which pairings already exist
            this.associationServiceMock.Verify(service =>
                service.FindAssociationByPairAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldAddWhenTheBlockIsOutsideBothEndpointsScopeAsync()
        {
            // given: the negative control the theory above needs. A scoped block is SILENT
            // outside its scope (§SEC18.6 rule 2) — Tag-ReadOnly says nothing about a
            // ContentItem ↔ BibleReference pairing — so a veto that refused everything would
            // pass every case above and fail here.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.ReadOnlyFor(EntityType.Tag));

            var rawRequest = new Association
            {
                EntityAType = EntityType.BibleReference,
                EntityAKeyId = Guid.NewGuid(),
                EntityBType = EntityType.ContentItem,
                EntityBKeyId = Guid.NewGuid(),
            };

            this.bibleReferenceServiceMock.Setup(service =>
                service.RetrieveBibleReferenceByIdAsync(
                    rawRequest.EntityAKeyId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new BibleReference { Id = rawRequest.EntityAKeyId });

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(
                    rawRequest.EntityBKeyId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new ContentItem
                        {
                            Id = rawRequest.EntityBKeyId,
                            GroupId = Guid.NewGuid(),
                            ContentType = ContentType.Story,
                        });

            this.associationServiceMock.Setup(service =>
                service.FindAssociationByPairAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((AssociationPairMatch?)null);

            this.associationServiceMock.Setup(service =>
                service.FindOverlappingAssociationAsync(
                    It.IsAny<Association>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((AssociationPairMatch?)null);

            var insertedId = Guid.NewGuid();

            this.associationServiceMock.Setup(service =>
                service.AddAssociationAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Association association, CancellationToken _) =>
                        {
                            association.Id = insertedId;

                            return association;
                        });

            // when
            AssociationSuggestionResult actualResult =
                await this.associationOrchestrationService.AddAssociationAsync(
                    rawRequest,
                    TestContext.Current.CancellationToken);

            // then
            actualResult.Status.Should().Be(AssociationSuggestionStatus.Created);
            actualResult.AssociationId.Should().Be(insertedId);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(ModifyMember)]
        [InlineData(RemoveMember)]
        [InlineData(HardRemoveMember)]
        public async Task ShouldThrowDependencyValidationExceptionOnWriteIfEitherEndpointIsBlockedForTheCallerAsync(
            string writeMember)
        {
            // given: on modify, remove and hard remove the caller hands over an id or an
            // untrusted row, so there is no resolved endpoint for this layer to compose from —
            // the veto runs in the foundation against the STORED endpoints and arrives here as a
            // DEPENDENCY VALIDATION failure. Same rule, different layer, different family; both
            // 4xx and neither a 424.
            //
            // Hard removal additionally needs Administrators just to reach the foundation at all,
            // which is criterion 7's half of the split. That is the point of running it in this
            // theory: an administrator who is blocked on ONE endpoint is still refused.
            string[] callerRoles = writeMember == HardRemoveMember
                ? new[]
                {
                    Roles.ReadOnlyFor(EntityType.Tag),
                    Roles.ReviewersFor(EntityType.BibleReference),
                    Roles.Administrators,
                }
                : new[]
                {
                    Roles.ReadOnlyFor(EntityType.Tag),
                    Roles.ReviewersFor(EntityType.BibleReference),
                };

            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(callerRoles);

            var storedAssociationId = Guid.NewGuid();

            var callerSuppliedAssociation = new Association
            {
                Id = storedAssociationId,
                EntityAType = EntityType.BibleReference,
                EntityAKeyId = Guid.NewGuid(),
                EntityBType = EntityType.Tag,
                EntityBKeyId = Guid.NewGuid(),
            };

            var unauthorizedAssociationException =
                new UnauthorizedAssociationException(
                    message: "The current user is blocked from contributing content item associations.");

            var associationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: unauthorizedAssociationException);

            var expectedDependencyValidationException =
                new AssociationOrchestrationDependencyValidationException(
                    message: "Content item association orchestration dependency validation error " +
                        "occurred, fix the errors and try again.",
                    innerException: unauthorizedAssociationException);

            this.associationServiceMock.Setup(service =>
                service.ModifyAssociationAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(associationValidationException);

            this.associationServiceMock.Setup(service =>
                service.RemoveAssociationByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(associationValidationException);

            this.associationServiceMock.Setup(service =>
                service.HardRemoveAssociationByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(associationValidationException);

            // when
            ValueTask<Association> writeTask = writeMember switch
            {
                ModifyMember =>
                    this.associationOrchestrationService.ModifyAssociationAsync(
                        callerSuppliedAssociation,
                        TestContext.Current.CancellationToken),

                RemoveMember =>
                    this.associationOrchestrationService.RemoveAssociationByIdAsync(
                        storedAssociationId,
                        deletionReason: null,
                        cancellationToken: TestContext.Current.CancellationToken),

                _ =>
                    this.associationOrchestrationService.HardRemoveAssociationByIdAsync(
                        storedAssociationId,
                        TestContext.Current.CancellationToken),
            };

            AssociationOrchestrationDependencyValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationDependencyValidationException>(
                    writeTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyValidationException))),
                Times.Once);

            // the orchestration resolved no endpoint of its own on any of the three
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.tagServiceMock.VerifyNoOtherCalls();
            this.bibleReferenceServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        private const string BibleReferenceTagPairing = "BibleReference-Tag";
        private const string ContentItemTagPairing = "ContentItem-Tag";
        private const string ModifyMember = "Modify";
        private const string RemoveMember = "Remove";
        private const string HardRemoveMember = "HardRemove";

        // Builds a raw add request for the named pairing and stubs both endpoints' by-id reads,
        // so resolution succeeds and the veto is the only thing left that can refuse.
        private Association SetupPairingEndpointReads(string pairing)
        {
            if (pairing == BibleReferenceTagPairing)
            {
                var bibleReferenceToTag = new Association
                {
                    EntityAType = EntityType.BibleReference,
                    EntityAKeyId = Guid.NewGuid(),
                    EntityBType = EntityType.Tag,
                    EntityBKeyId = Guid.NewGuid(),
                };

                this.bibleReferenceServiceMock.Setup(service =>
                    service.RetrieveBibleReferenceByIdAsync(
                        bibleReferenceToTag.EntityAKeyId,
                        It.IsAny<CancellationToken>()))
                            .ReturnsAsync(new BibleReference
                            {
                                Id = bibleReferenceToTag.EntityAKeyId,
                            });

                this.tagServiceMock.Setup(service =>
                    service.RetrieveTagByIdAsync(
                        bibleReferenceToTag.EntityBKeyId,
                        It.IsAny<CancellationToken>()))
                            .ReturnsAsync(new Tag { Id = bibleReferenceToTag.EntityBKeyId });

                return bibleReferenceToTag;
            }

            var contentItemToTag = new Association
            {
                EntityAType = EntityType.ContentItem,
                EntityAKeyId = Guid.NewGuid(),
                EntityBType = EntityType.Tag,
                EntityBKeyId = Guid.NewGuid(),
            };

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(
                    contentItemToTag.EntityAKeyId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new ContentItem
                        {
                            Id = contentItemToTag.EntityAKeyId,
                            GroupId = Guid.NewGuid(),

                            // set explicitly — the narrow tier is composed from this value, and a
                            // fixture that let it default would prove nothing about it
                            ContentType = ContentType.Story,
                        });

            this.tagServiceMock.Setup(service =>
                service.RetrieveTagByIdAsync(
                    contentItemToTag.EntityBKeyId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new Tag { Id = contentItemToTag.EntityBKeyId });

            return contentItemToTag;
        }
    }
}
