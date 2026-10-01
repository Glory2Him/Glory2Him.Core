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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        // A READER'S REACTION HAS NO EVENT PATH (#723, AssociationOrchestrationService.md §2 rule
        // 1). It is given, changed and brought back through one write this door cannot reach —
        // Association-Upserting is not minted (§ARC16.2.2) — and the foundation's add behind this
        // door can neither revive nor repoint, so a reaction let through would be inserted beside
        // the reader's existing row (§DOM4.10 rule 6). Refused AFTER the verify and the duplicate
        // question, both of which are asked, and BEFORE either endpoint is read. Both reads are
        // stubbed, so a door without the refusal runs the whole flow through to the foundation.
        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddingEventIfThePairIsPersonalAndLogItAsync()
        {
            // given
            Association addRequest =
                CreateHonestAddRequestBetween(EntityType.ContentItem, EntityType.Reaction);

            EventEnvelope<Association> inputEnvelope = CreateRequestEnvelope(addRequest);
            SetupEventPathEndpointReadsBetween(addRequest, inputEnvelope);

            var invalidAssociationOrchestrationException =
                new InvalidAssociationOrchestrationException(
                    message: "A personal content item association cannot be added through an event.");

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

            this.associationServiceMock.Verify(service =>
                service.HasAlreadyAddedAssociationAsync(
                    inputEnvelope,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            // neither endpoint was read
            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.tagServiceMock.VerifyNoOtherCalls();
            this.reactionServiceMock.VerifyNoOtherCalls();
            this.bibleReferenceServiceMock.VerifyNoOtherCalls();
            this.commentServiceMock.VerifyNoOtherCalls();
            this.linkServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // AFTER THE DUPLICATE QUESTION'S ANSWER, not merely after it is asked (#723 criterion 2).
        // A re-delivered personal envelope the foundation has already applied settles as a replay,
        // as an editorial one does. Refused instead, a settled event would be recorded as a failed
        // delivery and retried, which is what #631's early duplicate question exists to prevent.
        [Fact]
        public async Task ShouldShortCircuitAnAlreadyAppliedPersonalPairBeforeRefusingItAsync()
        {
            // given
            Association addRequest =
                CreateHonestAddRequestBetween(EntityType.ContentItem, EntityType.Reaction);

            EventEnvelope<Association> inputEnvelope = CreateRequestEnvelope(addRequest);
            SetupEventPathEndpointReadsBetween(addRequest, inputEnvelope);

            this.associationServiceMock.Setup(service =>
                service.HasAlreadyAddedAssociationAsync(
                    inputEnvelope,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(true);

            // when
            EventEnvelope<Association> actualReplyEnvelope =
                await this.associationOrchestrationService.OnAddingAssociationAsync(
                    inputEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().BeNull();

            this.envelopeIntegrityBrokerMock.Verify(broker =>
                broker.VerifyAsync(
                    inputEnvelope,
                    "AssociationAdding",
                    EnvelopeDirection.Request),
                Times.Once);

            this.associationServiceMock.Verify(service =>
                service.HasAlreadyAddedAssociationAsync(
                    inputEnvelope,
                    TestContext.Current.CancellationToken),
                Times.Once);

            // nothing refused and logged, nothing read, nothing delegated
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

        // A REFUSAL WRITES NOTHING (#723, AssociationOrchestrationService.md §2 rule 2). The row,
        // the Association-Added fact and the ProcessedEvents record are all the foundation
        // handler's, so a refused personal pair has done none of them exactly when that handler
        // was never reached. The duplicate question is all the foundation is asked, and it
        // records nothing; nothing is minted here either.
        [Fact]
        public async Task ShouldNeverDelegateAPersonalPairToTheFoundationAsync()
        {
            // given
            Association addRequest =
                CreateHonestAddRequestBetween(EntityType.ContentItem, EntityType.Reaction);

            EventEnvelope<Association> inputEnvelope = CreateRequestEnvelope(addRequest);
            SetupEventPathEndpointReadsBetween(addRequest, inputEnvelope);

            this.associationServiceMock.Setup(service =>
                service.OnAddingAssociationAsync(
                    inputEnvelope,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(inputEnvelope);

            // when
            ValueTask<EventEnvelope<Association>> onAddingTask =
                this.associationOrchestrationService.OnAddingAssociationAsync(
                    inputEnvelope,
                    TestContext.Current.CancellationToken);

            await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(
                onAddingTask.AsTask);

            // then
            this.associationServiceMock.Verify(service =>
                service.OnAddingAssociationAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.associationServiceMock.Verify(service =>
                service.HasAlreadyAddedAssociationAsync(
                    inputEnvelope,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.associationServiceMock.VerifyNoOtherCalls();
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
        }

        // ONE ANSWER FOR EVERY PERSONAL PAIR (#723, AssociationOrchestrationService.md §2 rule 2),
        // after #631's single occupancy message. Two requests that share no value — not the side
        // the reaction is named on, not an id, not a reader, not a status, and not the signed
        // caller — are both refused, and the two refusals are compared with each other, message
        // and data. Neither may name a row, a reader or an approval state, so a publisher learns
        // that the pair is personal and nothing about it.
        [Fact]
        public async Task ShouldRefuseEveryPersonalPairWithOneMessageAsync()
        {
            // given
            var firstRequest = new Association
            {
                Id = Guid.NewGuid(),
                EntityAType = EntityType.ContentItem,
                EntityAKeyId = Guid.NewGuid(),
                EntityAGroupId = Guid.NewGuid(),
                EntityAScope = Scope.AllVersions,
                EntityAContentType = ContentType.Story,
                EntityBType = EntityType.Reaction,
                EntityBKeyId = Guid.NewGuid(),
                EntityBGroupId = Guid.NewGuid(),
                EntityBScope = Scope.ThisVersionOnly,
                EntityBContentType = null,
                UserId = $"reader-{Guid.NewGuid()}",
                SortOrder = 1,
                ConfidenceScore = 0.25m,
                ConfidenceReason = $"reason-{Guid.NewGuid()}",
                SourceBatchId = Guid.NewGuid(),
                ModelVersion = $"model-{Guid.NewGuid()}",
                CreatedBy = $"creator-{Guid.NewGuid()}",
                CreatedWhen = DateTimeOffset.UnixEpoch,
                UpdatedBy = $"updater-{Guid.NewGuid()}",
                UpdatedWhen = DateTimeOffset.UnixEpoch,
                DeletedBy = null,
                DeletedWhen = null,
                IsDeleted = false,
                DeletionReason = null,
                PublishDate = null,
                IsPublished = false,
                ApprovalStatus = ApprovalStatus.Submitted,
                IsApprovedByBypass = false,
                ApprovedByBypassReason = null,
            };

            var secondRequest = new Association
            {
                Id = Guid.NewGuid(),
                EntityAType = EntityType.Reaction,
                EntityAKeyId = Guid.NewGuid(),
                EntityAGroupId = Guid.NewGuid(),
                EntityAScope = Scope.ThisVersionOnly,
                EntityAContentType = null,
                EntityBType = EntityType.Tag,
                EntityBKeyId = Guid.NewGuid(),
                EntityBGroupId = Guid.NewGuid(),
                EntityBScope = Scope.AllVersions,
                EntityBContentType = ContentType.Testimony,
                UserId = $"reader-{Guid.NewGuid()}",
                SortOrder = 2,
                ConfidenceScore = 0.75m,
                ConfidenceReason = $"reason-{Guid.NewGuid()}",
                SourceBatchId = Guid.NewGuid(),
                ModelVersion = $"model-{Guid.NewGuid()}",
                CreatedBy = $"creator-{Guid.NewGuid()}",
                CreatedWhen = DateTimeOffset.UnixEpoch.AddDays(1),
                UpdatedBy = $"updater-{Guid.NewGuid()}",
                UpdatedWhen = DateTimeOffset.UnixEpoch.AddDays(1),
                DeletedBy = $"deleter-{Guid.NewGuid()}",
                DeletedWhen = DateTimeOffset.UnixEpoch.AddDays(2),
                IsDeleted = true,
                DeletionReason = $"deletion-{Guid.NewGuid()}",
                PublishDate = DateTimeOffset.UnixEpoch.AddDays(1),
                IsPublished = true,
                ApprovalStatus = ApprovalStatus.Approved,
                IsApprovedByBypass = true,
                ApprovedByBypassReason = $"bypass-{Guid.NewGuid()}",
            };

            SecurityContext firstCaller = CreateSignedReader();
            SecurityContext secondCaller = CreateSignedReader();
            var refusals = new List<(string Message, IDictionary Data)>();

            var personalRequests = new[]
            {
                (Request: firstRequest, Caller: firstCaller),
                (Request: secondRequest, Caller: secondCaller),
            };

            foreach ((Association request, SecurityContext caller) in personalRequests)
            {
                EventEnvelope<Association> inputEnvelope = CreateRequestEnvelope(request, caller);

                // when
                ValueTask<EventEnvelope<Association>> onAddingTask =
                    this.associationOrchestrationService.OnAddingAssociationAsync(
                        inputEnvelope,
                        TestContext.Current.CancellationToken);

                AssociationOrchestrationValidationException actualException =
                    await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(
                        onAddingTask.AsTask);

                refusals.Add((actualException.InnerException.Message, actualException.InnerException.Data));
            }

            // then
            refusals[1].Message.Should().Be(refusals[0].Message);
            refusals[1].Data.Should().BeEquivalentTo(refusals[0].Data);

            string message = refusals[0].Message;

            personalRequests
                .SelectMany(personalRequest => new[]
                {
                    personalRequest.Request.Id,
                    personalRequest.Request.EntityAKeyId,
                    personalRequest.Request.EntityAGroupId,
                    personalRequest.Request.EntityBKeyId,
                    personalRequest.Request.EntityBGroupId,
                })
                .Should().AllSatisfy(rowId =>
                    message.Should().NotContainEquivalentOf(rowId.ToString()));

            personalRequests
                .SelectMany(personalRequest => new[]
                {
                    personalRequest.Request.UserId,
                    personalRequest.Caller.SubjectId,
                    personalRequest.Caller.Username,
                })
                .Should().AllSatisfy(reader =>
                    message.Should().NotContainEquivalentOf(reader));

            Enum.GetNames<ApprovalStatus>().Should().AllSatisfy(status =>
                message.Should().NotContainEquivalentOf(status));
        }

        // A Reaction on each side in turn. Canonical order is the foundation's to compute, so a
        // publisher may name the reaction on either endpoint.
        public static TheoryData<EntityType, EntityType> PersonalPairs() =>
            new TheoryData<EntityType, EntityType>
            {
                { EntityType.Reaction, EntityType.ContentItem },
                { EntityType.ContentItem, EntityType.Reaction },
            };

        // PERSONALITY IS THE LOOKUP'S ANSWER FOR EITHER ENDPOINT (#723; §DOM4.2, §DOM4.10 rule 4):
        // a pair is personal where either endpoint's type is. A refusal that asked one side only
        // would let a reaction through whenever the publisher named it on the other. Both reads
        // are stubbed, so a side left unasked runs through to the foundation.
        [Theory]
        [MemberData(nameof(PersonalPairs))]
        public async Task ShouldRefuseAPersonalPairWhicheverEndpointIsPersonalAsync(
            EntityType entityAType,
            EntityType entityBType)
        {
            // given
            Association addRequest = CreateHonestAddRequestBetween(entityAType, entityBType);
            EventEnvelope<Association> inputEnvelope = CreateRequestEnvelope(addRequest);
            SetupEventPathEndpointReadsBetween(addRequest, inputEnvelope);

            var invalidAssociationOrchestrationException =
                new InvalidAssociationOrchestrationException(
                    message: "A personal content item association cannot be added through an event.");

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
            actualException.Should().BeEquivalentTo(
                expectedValidationException,
                because: $"a {EntityType.Reaction} on either endpoint makes the pair personal");

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.associationServiceMock.Verify(service =>
                service.OnAddingAssociationAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.reactionServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        public static TheoryData<string> UndefinedEndpointTypeParameters() =>
            new TheoryData<string>
            {
                nameof(Association.EntityAType),
                nameof(Association.EntityBType),
            };

        // THE REFUSAL SET STANDS (#723, Out of scope). An endpoint type outside the enum is
        // malformed input, and the shared flow's structural validation refuses it as invalid. The
        // personal refusal runs ahead of that flow, so it must leave such a value for the flow to
        // refuse, rather than fail on it as a service error.
        [Theory]
        [MemberData(nameof(UndefinedEndpointTypeParameters))]
        public async Task ShouldThrowValidationExceptionOnAddingEventIfAnEndpointTypeIsUndefinedAndLogItAsync(
            string undefinedParameter)
        {
            // given
            var undefinedEntityType = (EntityType)int.MaxValue;
            Association addRequest = CreateHonestAddRequest();

            if (undefinedParameter == nameof(Association.EntityAType))
            {
                addRequest.EntityAType = undefinedEntityType;
            }
            else
            {
                addRequest.EntityBType = undefinedEntityType;
            }

            EventEnvelope<Association> inputEnvelope = CreateRequestEnvelope(addRequest);

            var invalidAssociationOrchestrationException =
                new InvalidAssociationOrchestrationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationOrchestrationException.AddData(
                key: undefinedParameter,
                values: "Value is not a recognized entity type");

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

        private static SecurityContext CreateSignedReader() =>
            new SecurityContext
            {
                IsAuthenticated = true,
                SubjectId = $"subject-{Guid.NewGuid()}",
                Username = $"reader-{Guid.NewGuid()}",
            };

        // Stubs both endpoint reads as the signed caller would be answered, whatever the two types.
        private void SetupEventPathEndpointReadsBetween(
            Association addRequest,
            EventEnvelope<Association> inboundEnvelope)
        {
            SetupEventPathEndpointRead(
                addRequest.EntityAType,
                addRequest.EntityAKeyId,
                addRequest.EntityAGroupId,
                inboundEnvelope);

            SetupEventPathEndpointRead(
                addRequest.EntityBType,
                addRequest.EntityBKeyId,
                addRequest.EntityBGroupId,
                inboundEnvelope);
        }
    }
}
