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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Orchestrations.Associations.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Associations
{
    public partial class AssociationOrchestrationServiceTests
    {
        // VERIFICATION SITS IN THE RECEIVER (§SEC14.6 rule 4), and this handler is now the first
        // receiver. It reads both endpoint rows before the foundation is reached, so doing that on
        // the word of an envelope nothing has vouched for would be acting on an unattested payload.
        // The name verified is the one EventBroker signs for this address — "AssociationAdding",
        // exactly what the foundation verified while the address bound there.
        [Fact]
        public async Task ShouldRefuseAnUnverifiedEnvelopeOnTheAddingAddressAsync()
        {
            // given
            Association addRequest = CreateHonestAddRequest();
            EventEnvelope<Association> inputEnvelope = CreateRequestEnvelope(addRequest);
            SetupEventPathEndpointReads(addRequest, inputEnvelope);

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    inputEnvelope,
                    "AssociationAdding",
                    EnvelopeDirection.Request))
                        .ReturnsAsync(false);

            var invalidAssociationOrchestrationException =
                new InvalidAssociationOrchestrationException(
                    message: "Invalid content item association event. " +
                        "Integrity verification failed.");

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

            this.envelopeIntegrityBrokerMock.Verify(broker =>
                broker.VerifyAsync(
                    inputEnvelope,
                    "AssociationAdding",
                    EnvelopeDirection.Request),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            // nothing read, nothing derived, nothing delegated
            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();
            this.associationServiceMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.tagServiceMock.VerifyNoOtherCalls();
            this.reactionServiceMock.VerifyNoOtherCalls();
            this.bibleReferenceServiceMock.VerifyNoOtherCalls();
            this.commentServiceMock.VerifyNoOtherCalls();
            this.linkServiceMock.VerifyNoOtherCalls();
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // Nothing to verify, nothing to deduplicate on and nothing to derive from. Refused ahead
        // of the verify, so a shapeless delivery never reaches the integrity broker at all.
        public static TheoryData<EventEnvelope<Association>> IncompleteAddingEnvelopes() =>
            new TheoryData<EventEnvelope<Association>>
            {
                null,

                new EventEnvelope<Association>
                {
                    Content = null,
                    SecurityContext = CreateAuthenticatedSecurityContext(),
                    Metadata = new EventMetadata { EventId = Guid.NewGuid() },
                },

                new EventEnvelope<Association>
                {
                    Content = CreateHonestAddRequest(),
                    SecurityContext = CreateAuthenticatedSecurityContext(),
                    Metadata = null,
                },
            };

        [Theory]
        [MemberData(nameof(IncompleteAddingEnvelopes))]
        public async Task ShouldThrowValidationExceptionOnAddingIfTheEnvelopeIsIncompleteAndLogItAsync(
            EventEnvelope<Association> incompleteEnvelope)
        {
            // given
            var invalidAssociationOrchestrationException =
                new InvalidAssociationOrchestrationException(
                    message: "Invalid content item association event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedValidationException =
                new AssociationOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidAssociationOrchestrationException);

            // when
            ValueTask<EventEnvelope<Association>> onAddingTask =
                this.associationOrchestrationService.OnAddingAssociationAsync(
                    incompleteEnvelope,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(
                    onAddingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();
            this.associationServiceMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.tagServiceMock.VerifyNoOtherCalls();
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // Claims that contradict what the endpoints resolve to — a Story on A, a Tag (no content
        // type) on B. Omitting A's value is a contradiction too: the derived value governs, and an
        // omission handed down would reach the foundation's gate as "undecidable" rather than as
        // what the endpoint is.
        public static TheoryData<ContentType?, ContentType?, string> ContradictingContentTypeClaims() =>
            new TheoryData<ContentType?, ContentType?, string>
            {
                { ContentType.Testimony, null, nameof(Association.EntityAContentType) },
                { null, null, nameof(Association.EntityAContentType) },
                { ContentType.Story, ContentType.Story, nameof(Association.EntityBContentType) },
            };

        // REFUSED, NOT OVERWRITTEN (Architecture.md, "the derivation REFUSES a contradicting
        // claim"). The claim sits inside a signed envelope whose HMAC covers the content, and the
        // property that signature buys is that no receiver silently edits a part the rules read.
        // So the derived value still governs, and a request that contradicts it never reaches the
        // foundation. An honest publisher — anything that resolved the endpoints — is unaffected.
        [Theory]
        [MemberData(nameof(ContradictingContentTypeClaims))]
        public async Task ShouldRefuseAContradictingContentTypeOnTheEventPathAsync(
            ContentType? claimedEntityAContentType,
            ContentType? claimedEntityBContentType,
            string contradictedParameter)
        {
            // given
            Association addRequest = CreateHonestAddRequest();
            addRequest.EntityAContentType = claimedEntityAContentType;
            addRequest.EntityBContentType = claimedEntityBContentType;
            EventEnvelope<Association> inputEnvelope = CreateRequestEnvelope(addRequest);
            SetupEventPathEndpointReads(addRequest, inputEnvelope);

            var invalidAssociationOrchestrationException =
                new InvalidAssociationOrchestrationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationOrchestrationException.AddData(
                key: contradictedParameter,
                values: "Value must be the content type its endpoint resolves to");

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

            // the derivation ran — both reads, as the signed caller — and the write never did
            this.contentItemServiceMock.Verify(service =>
                service.RetrieveContentItemByIdAsync(
                    addRequest.EntityAKeyId,
                    inputEnvelope,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.tagServiceMock.Verify(service =>
                service.RetrieveTagByIdAsync(
                    addRequest.EntityBKeyId,
                    inputEnvelope,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.associationServiceMock.Verify(service =>
                service.HasAlreadyAddedAssociationAsync(
                    inputEnvelope,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.associationServiceMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.tagServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
