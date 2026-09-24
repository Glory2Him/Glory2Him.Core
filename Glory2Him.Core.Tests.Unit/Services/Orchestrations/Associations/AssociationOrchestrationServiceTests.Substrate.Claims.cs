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
using Force.DeepCloner;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Orchestrations.Associations.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Associations
{
    // THE REST OF THE REFUSAL SET (#631 criterion 3, Architecture.md "RULE — on the event path,
    // every value the write flow derives"). The foundation takes UserId and a versioned endpoint's
    // group id from its caller as handed, so on the event path a claim that differs from the
    // derivation is refused rather than edited out of signed content.
    public partial class AssociationOrchestrationServiceTests
    {
        public static TheoryData<string> ClaimedUserIds() =>
            new TheoryData<string>
            {
                "a-claimed-user-id",
                "",
                "   ",
            };

        // 3b. The derived UserId is null on every path until the personal-reaction derivation
        // exists, so every non-null claim differs — an empty or blank string included. A non-null
        // UserId makes the row personal, and that tier is seeded to auto-approve.
        [Theory]
        [MemberData(nameof(ClaimedUserIds))]
        public async Task ShouldRefuseAClaimedUserIdOnTheEventPathAsync(string claimedUserId)
        {
            // given
            Association addRequest = CreateHonestAddRequest();
            addRequest.UserId = claimedUserId;
            EventEnvelope<Association> inputEnvelope = CreateRequestEnvelope(addRequest);
            SetupEventPathEndpointReads(addRequest, inputEnvelope);

            var invalidAssociationOrchestrationException =
                new InvalidAssociationOrchestrationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationOrchestrationException.AddData(
                key: nameof(Association.UserId),
                values: "Value is derived and must not be supplied");

            var expectedValidationException =
                new AssociationOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidAssociationOrchestrationException);

            // when
            ValueTask<EventEnvelope<Association>> onAddingTask =
                this.associationOrchestrationService.OnAddingAssociationAsync(
                    inputEnvelope,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(
                    onAddingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.associationServiceMock.Verify(service =>
                service.OnAddingAssociationAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // Each case names the versioned endpoint type on B (A is always a ContentItem), which
        // side's claim is contradicted, and whether the claim is omitted rather than wrong.
        public static TheoryData<EntityType, string, bool> ContradictingVersionedGroupIdClaims() =>
            new TheoryData<EntityType, string, bool>
            {
                { EntityType.Tag, nameof(Association.EntityAGroupId), false },
                { EntityType.Tag, nameof(Association.EntityAGroupId), true },
                { EntityType.ContentItem, nameof(Association.EntityBGroupId), false },
                { EntityType.Link, nameof(Association.EntityBGroupId), false },
                { EntityType.Link, nameof(Association.EntityBGroupId), true },
            };

        // 3c. The foundation re-derives a non-versioned endpoint's group id but keeps a versioned
        // one's as handed, so a publisher could attach the row to a version group it never
        // resolved. A claim that differs from the resolved row's group is refused, and an omitted
        // (empty) one differs too.
        [Theory]
        [MemberData(nameof(ContradictingVersionedGroupIdClaims))]
        public async Task ShouldRefuseAContradictingVersionedGroupIdOnTheEventPathAsync(
            EntityType endpointBType,
            string contradictedParameter,
            bool isOmitted)
        {
            // given
            Association addRequest = CreateHonestAddRequest();
            addRequest.EntityBType = endpointBType;

            addRequest.EntityBContentType =
                endpointBType == EntityType.ContentItem ? ContentType.Story : null;

            addRequest.EntityBGroupId =
                endpointBType is EntityType.ContentItem or EntityType.Link
                    ? Guid.NewGuid()
                    : Guid.Empty;

            EventEnvelope<Association> inputEnvelope = CreateRequestEnvelope(addRequest);
            SetupEventPathEndpointReads(addRequest, inputEnvelope);

            SetupEventPathEndpointRead(
                endpointBType,
                addRequest.EntityBKeyId,
                addRequest.EntityBGroupId,
                inputEnvelope);

            // the reads above resolve the HONEST groups; only the claim changes now
            Guid claimedGroupId = isOmitted ? Guid.Empty : Guid.NewGuid();

            if (contradictedParameter == nameof(Association.EntityAGroupId))
            {
                addRequest.EntityAGroupId = claimedGroupId;
            }
            else
            {
                addRequest.EntityBGroupId = claimedGroupId;
            }

            var invalidAssociationOrchestrationException =
                new InvalidAssociationOrchestrationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationOrchestrationException.AddData(
                key: contradictedParameter,
                values: "Value must be the group its endpoint resolves to");

            var expectedValidationException =
                new AssociationOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidAssociationOrchestrationException);

            // when
            ValueTask<EventEnvelope<Association>> onAddingTask =
                this.associationOrchestrationService.OnAddingAssociationAsync(
                    inputEnvelope,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(
                    onAddingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.associationServiceMock.Verify(service =>
                service.OnAddingAssociationAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // 3d. What the foundation re-derives before anything reads it is not the event path's to
        // refuse: both scopes, and a non-versioned endpoint's group id (the Tag on B). Claims
        // that differ from the derivation on all three still reach the foundation, unchanged.
        [Fact]
        public async Task ShouldNotCompareScopeOrANonVersionedGroupIdOnTheEventPathAsync()
        {
            // given
            Association addRequest = CreateHonestAddRequest();
            addRequest.EntityAScope = Scope.ThisVersionOnly;
            addRequest.EntityBScope = Scope.AllVersions;
            addRequest.EntityBGroupId = Guid.NewGuid();
            EventEnvelope<Association> inputEnvelope = CreateRequestEnvelope(addRequest);
            Association expectedContent = addRequest.DeepClone();
            SetupEventPathEndpointReads(addRequest, inputEnvelope);

            this.associationServiceMock.Setup(service =>
                service.OnAddingAssociationAsync(
                    inputEnvelope,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(inputEnvelope);

            // when
            EventEnvelope<Association> actualReplyEnvelope =
                await this.associationOrchestrationService.OnAddingAssociationAsync(
                    inputEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().BeSameAs(inputEnvelope);
            inputEnvelope.Content.Should().BeEquivalentTo(expectedContent);

            this.associationServiceMock.Verify(service =>
                service.OnAddingAssociationAsync(
                    inputEnvelope,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
