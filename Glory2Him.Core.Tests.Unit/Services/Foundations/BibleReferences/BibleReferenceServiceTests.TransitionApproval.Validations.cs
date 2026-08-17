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
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Glory2Him.Core.Models.Foundations.BibleReferences.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.BibleReferences
{
    public partial class BibleReferenceServiceTests
    {
        public static TheoryData<string[]> NonPublisherRoleSets() =>
            new TheoryData<string[]>
            {
                new string[0],

                // a Reviewer holds the review tier and MUST still never set an approval status
                // (§8.6 HR-3) — the publisher tier deliberately excludes it
                new[] { Roles.Reviewer },
                new[] { Roles.BibleReferenceReviewer },
            };

        [Fact]
        public async Task ShouldThrowValidationExceptionOnApproveIfBibleReferenceIsNullAsync()
        {
            // given
            BibleReference nullBibleReference = null;

            var nullBibleReferenceException =
                new NullBibleReferenceException(message: "Bible reference is null.");

            var expectedBibleReferenceValidationException =
                new BibleReferenceValidationException(
                    message: "Bible reference validation error occurred, fix the errors and try again.",
                    innerException: nullBibleReferenceException);

            // when
            ValueTask<BibleReference> approveTask =
                this.bibleReferenceService.TransitionBibleReferenceApprovalAsync(
                    nullBibleReference,
                    TestContext.Current.CancellationToken);

            BibleReferenceValidationException actualException =
                await Assert.ThrowsAsync<BibleReferenceValidationException>(approveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedBibleReferenceValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectBibleReferenceByIdAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [InlineData(ApprovalStatus.Draft)]
        [InlineData(ApprovalStatus.Dismissed)]
        public async Task ShouldThrowValidationExceptionOnTransitionIfStatusIsNotATransitionTargetAsync(
            ApprovalStatus notATransitionTarget)
        {
            // given: this operation owns IApproval, so it is the one allowed to carry a status —
            // but only to a state the workflow can hold a row in. Draft is reached once, at
            // creation, and submitting is its own verb; Dismissed belongs to a withdrawal step.
            // Submitted is NOT here: it is what an override re-opens a terminal row to.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher);

            BibleReference inputBibleReference = CreateApprovalDecision(Guid.NewGuid());
            inputBibleReference.ApprovalStatus = notATransitionTarget;
            inputBibleReference.IsPublished = false;
            inputBibleReference.PublishDate = null;

            // when
            ValueTask<BibleReference> approveTask =
                this.bibleReferenceService.TransitionBibleReferenceApprovalAsync(
                    inputBibleReference,
                    TestContext.Current.CancellationToken);

            await Assert.ThrowsAsync<BibleReferenceValidationException>(approveTask.AsTask);

            // then: the status never reached storage — the row was never even read
            this.storageBrokerMock.Verify(broker =>
                    broker.SelectBibleReferenceByIdAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnApproveIfPublishedWithoutApprovalAsync()
        {
            // given: publication is a consequence of approval — a row cannot be published while
            // being rejected. The rule is the ONLY guard on this pair (DoApprove copies
            // IsPublished straight from the caller), and it fires before the row is read.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher);

            BibleReference inputBibleReference = CreateRejectionDecision(Guid.NewGuid());
            inputBibleReference.IsPublished = true;

            var invalidBibleReferenceException =
                new InvalidBibleReferenceException(
                    message: "Bible reference is invalid, fix the errors and try again.");

            invalidBibleReferenceException.UpsertDataList(
                key: nameof(BibleReference.IsPublished),
                value: "Is published requires an approved bible reference.");

            var expectedBibleReferenceValidationException =
                new BibleReferenceValidationException(
                    message: "Bible reference validation error occurred, fix the errors and try again.",
                    innerException: invalidBibleReferenceException);

            // when
            ValueTask<BibleReference> approveTask =
                this.bibleReferenceService.TransitionBibleReferenceApprovalAsync(
                    inputBibleReference,
                    TestContext.Current.CancellationToken);

            BibleReferenceValidationException actualException =
                await Assert.ThrowsAsync<BibleReferenceValidationException>(approveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedBibleReferenceValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectBibleReferenceByIdAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishBibleReferenceAsync(
                        It.IsAny<EventEnvelope<BibleReference>>(),
                        It.IsAny<BibleReferenceEventOperation>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnApproveIfPublishDateWithoutPublicationAsync()
        {
            // given: a publish date without publication is a date nothing reads. DoApprove copies
            // PublishDate straight from the caller, so this rule is the only guard against a
            // phantom publish date on an unpublished row.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher);

            BibleReference inputBibleReference = CreateRejectionDecision(Guid.NewGuid());
            inputBibleReference.IsPublished = false;
            inputBibleReference.PublishDate = GetRandomDateTimeOffset();

            var invalidBibleReferenceException =
                new InvalidBibleReferenceException(
                    message: "Bible reference is invalid, fix the errors and try again.");

            invalidBibleReferenceException.UpsertDataList(
                key: nameof(BibleReference.PublishDate),
                value: "Publish date requires a published bible reference.");

            var expectedBibleReferenceValidationException =
                new BibleReferenceValidationException(
                    message: "Bible reference validation error occurred, fix the errors and try again.",
                    innerException: invalidBibleReferenceException);

            // when
            ValueTask<BibleReference> approveTask =
                this.bibleReferenceService.TransitionBibleReferenceApprovalAsync(
                    inputBibleReference,
                    TestContext.Current.CancellationToken);

            BibleReferenceValidationException actualException =
                await Assert.ThrowsAsync<BibleReferenceValidationException>(approveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedBibleReferenceValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectBibleReferenceByIdAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowNotFoundOnApproveIfTheRowIsMissingAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher);

            BibleReference inputBibleReference = CreateApprovalDecision(Guid.NewGuid());

            this.storageBrokerMock.Setup(broker =>
                broker.SelectBibleReferenceByIdAsync(
                    inputBibleReference.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((BibleReference)null);

            // when
            ValueTask<BibleReference> approveTask =
                this.bibleReferenceService.TransitionBibleReferenceApprovalAsync(
                    inputBibleReference,
                    TestContext.Current.CancellationToken);

            await Assert.ThrowsAsync<BibleReferenceValidationException>(approveTask.AsTask);

            // then: a missing row is decided against nothing
            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateBibleReferenceAsync(
                        It.IsAny<BibleReference>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowNotFoundOnApproveIfTheRowIsSoftDeletedAsync()
        {
            // given: a soft-removed row is a takedown reported as not-found.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher);

            BibleReference storageBibleReference = CreateApprovableStorageBibleReference();
            storageBibleReference.IsDeleted = true;

            BibleReference inputBibleReference = CreateApprovalDecision(storageBibleReference.Id);

            SetupBibleReferenceStorageRead(storageBibleReference);

            var notFoundBibleReferenceException =
                new NotFoundBibleReferenceException(
                    message: $"Bible reference not found with id: {storageBibleReference.Id}.");

            var expectedBibleReferenceValidationException =
                new BibleReferenceValidationException(
                    message: "Bible reference validation error occurred, fix the errors and try again.",
                    innerException: notFoundBibleReferenceException);

            // when
            ValueTask<BibleReference> approveTask =
                this.bibleReferenceService.TransitionBibleReferenceApprovalAsync(
                    inputBibleReference,
                    TestContext.Current.CancellationToken);

            BibleReferenceValidationException actualException =
                await Assert.ThrowsAsync<BibleReferenceValidationException>(approveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedBibleReferenceValidationException);

            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [InlineData(ApprovalStatus.Draft)]
        [InlineData(ApprovalStatus.Dismissed)]
        public async Task ShouldThrowValidationExceptionOnTransitionIfTheStoredRowIsNotTransitionableAsync(
            ApprovalStatus storageStatus)
        {
            // given: a Draft has not been submitted and a Dismissed row is not in a round at all,
            // so neither can be decided. Approved and Rejected are absent because they ARE
            // transitionable — by an Admin, through the override — and are covered there.
            //
            // The tier and the access decision pass first (global Publisher, permissive fixture),
            // so this proves the state gate stands on its own.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher);

            BibleReference storageBibleReference = CreateApprovableStorageBibleReference();
            storageBibleReference.ApprovalStatus = storageStatus;

            BibleReference inputBibleReference = CreateApprovalDecision(storageBibleReference.Id);

            SetupBibleReferenceStorageRead(storageBibleReference);
            SetupAccessBrokerToPermit();

            var invalidBibleReferenceException =
                new InvalidBibleReferenceException(
                    message: "Bible reference cannot be approved from status " +
                        $"{storageStatus}.");

            var expectedBibleReferenceValidationException =
                new BibleReferenceValidationException(
                    message: "Bible reference validation error occurred, fix the errors and try again.",
                    innerException: invalidBibleReferenceException);

            // when
            ValueTask<BibleReference> approveTask =
                this.bibleReferenceService.TransitionBibleReferenceApprovalAsync(
                    inputBibleReference,
                    TestContext.Current.CancellationToken);

            BibleReferenceValidationException actualException =
                await Assert.ThrowsAsync<BibleReferenceValidationException>(approveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedBibleReferenceValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateBibleReferenceAsync(
                        It.IsAny<BibleReference>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishBibleReferenceAsync(
                        It.IsAny<EventEnvelope<BibleReference>>(),
                        It.IsAny<BibleReferenceEventOperation>()),
                Times.Never);
        }

        [Theory]
        [MemberData(nameof(NonPublisherRoleSets))]
        public async Task ShouldThrowUnauthorizedOnApproveIfCallerLacksThePublisherTierAsync(
            string[] roles)
        {
            // given: the row-local publisher-tier check is where HR-3 lands — a Reviewer is
            // refused before the access decision is ever asked.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(roles);

            BibleReference storageBibleReference = CreateApprovableStorageBibleReference();
            BibleReference inputBibleReference = CreateApprovalDecision(storageBibleReference.Id);

            SetupBibleReferenceStorageRead(storageBibleReference);

            var unauthorizedBibleReferenceException =
                new UnauthorizedBibleReferenceException(
                    message: "The current user is not allowed to approve this bible reference.");

            var expectedBibleReferenceValidationException =
                new BibleReferenceValidationException(
                    message: "Bible reference validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedBibleReferenceException);

            // when
            ValueTask<BibleReference> approveTask =
                this.bibleReferenceService.TransitionBibleReferenceApprovalAsync(
                    inputBibleReference,
                    TestContext.Current.CancellationToken);

            BibleReferenceValidationException actualException =
                await Assert.ThrowsAsync<BibleReferenceValidationException>(approveTask.AsTask);

            // then: refused before the cross-entity decision is asked
            actualException.Should().BeEquivalentTo(expectedBibleReferenceValidationException);

            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateBibleReferenceAsync(
                        It.IsAny<BibleReference>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnApproveIfTheAccessBrokerRefusesAsync()
        {
            // given: the caller holds the global Publisher role, so the row-local tier check
            // passes and the cross-entity decision is the ONLY thing left that can refuse the
            // approve (HR-2 self-approval lives behind the access broker).
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher);

            BibleReference storageBibleReference = CreateApprovableStorageBibleReference();
            BibleReference inputBibleReference = CreateApprovalDecision(storageBibleReference.Id);

            SetupBibleReferenceStorageRead(storageBibleReference);
            SetupAccessBrokerToRefuse(AccessDenialReason.SelfApprovalNotPermitted);

            var unauthorizedBibleReferenceException =
                new UnauthorizedBibleReferenceException(
                    message: "The current user is not allowed to approve this bible reference.");

            var expectedBibleReferenceValidationException =
                new BibleReferenceValidationException(
                    message: "Bible reference validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedBibleReferenceException);

            // when
            ValueTask<BibleReference> approveTask =
                this.bibleReferenceService.TransitionBibleReferenceApprovalAsync(
                    inputBibleReference,
                    TestContext.Current.CancellationToken);

            BibleReferenceValidationException actualException =
                await Assert.ThrowsAsync<BibleReferenceValidationException>(approveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedBibleReferenceValidationException);

            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateBibleReferenceAsync(
                        It.IsAny<BibleReference>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishBibleReferenceAsync(
                        It.IsAny<EventEnvelope<BibleReference>>(),
                        It.IsAny<BibleReferenceEventOperation>()),
                Times.Never);

            this.loggingBrokerMock.Verify(broker =>
                    broker.LogWarningAsync(It.IsAny<string>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                    broker.LogErrorAsync(It.Is(
                        SameExceptionAs(expectedBibleReferenceValidationException))),
                Times.Once);
        }

        [Fact]
        public async Task ShouldNotLeakTheAccessExplanationToTheCallerOnApproveDenialAsync()
        {
            // given: the verdict's Explanation and the denial reason name resolved policy;
            // exception messages and their Data surface outward through a public event address
            // (§14.5 rule 2), so neither may appear in anything thrown.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher);

            BibleReference storageBibleReference = CreateApprovableStorageBibleReference();
            BibleReference inputBibleReference = CreateApprovalDecision(storageBibleReference.Id);

            SetupBibleReferenceStorageRead(storageBibleReference);
            SetupAccessBrokerToRefuse(AccessDenialReason.ApprovalThresholdNotMet);

            // when
            ValueTask<BibleReference> approveTask =
                this.bibleReferenceService.TransitionBibleReferenceApprovalAsync(
                    inputBibleReference,
                    TestContext.Current.CancellationToken);

            BibleReferenceValidationException actualException =
                await Assert.ThrowsAsync<BibleReferenceValidationException>(approveTask.AsTask);

            // then: the service's own wording, naming no policy
            actualException.InnerException.Message.Should().Be(
                "The current user is not allowed to approve this bible reference.");

            string thrownText = FlattenExceptionText(actualException);

            thrownText.Should().NotContain("refused");
            thrownText.Should().NotContain(nameof(AccessDenialReason.ApprovalThresholdNotMet));

            actualException.Data.Count.Should().Be(0);
            actualException.InnerException.Data.Count.Should().Be(0);
        }

        [Fact]
        public async Task ShouldLogTheDenialAsAWarningBeforeThrowingOnApproveAsync()
        {
            // given: §14.5 — the true reason is recorded server-side BEFORE the throw, because
            // the throw is what discards the verdict.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher);

            BibleReference storageBibleReference = CreateApprovableStorageBibleReference();
            BibleReference inputBibleReference = CreateApprovalDecision(storageBibleReference.Id);

            SetupBibleReferenceStorageRead(storageBibleReference);
            SetupAccessBrokerToRefuse(AccessDenialReason.ApprovalThresholdNotMet);

            var logCallOrder = new List<string>();

            this.loggingBrokerMock.Setup(broker =>
                broker.LogWarningAsync(It.IsAny<string>()))
                    .Callback<string>(message => logCallOrder.Add($"warning:{message}"))
                    .Returns(ValueTask.CompletedTask);

            this.loggingBrokerMock.Setup(broker =>
                broker.LogErrorAsync(It.IsAny<Exception>()))
                    .Callback<Exception>(_ => logCallOrder.Add("error"))
                    .Returns(ValueTask.CompletedTask);

            // when
            ValueTask<BibleReference> approveTask =
                this.bibleReferenceService.TransitionBibleReferenceApprovalAsync(
                    inputBibleReference,
                    TestContext.Current.CancellationToken);

            await Assert.ThrowsAsync<BibleReferenceValidationException>(approveTask.AsTask);

            // then: the warning lands first, and the error the throw produces second
            logCallOrder.Should().HaveCount(2);
            logCallOrder[0].Should().StartWith("warning:");
            logCallOrder[1].Should().Be("error");

            logCallOrder[0].Should().Contain(storageBibleReference.Id.ToString());
            logCallOrder[0].Should().Contain(nameof(AccessDenialReason.ApprovalThresholdNotMet));
            logCallOrder[0].Should().Contain("refused");
        }

        [Fact]
        public async Task ShouldAskTheAccessBrokerAboutTheStoredBibleReferenceOnApproveAsync()
        {
            // given: the caller's copy names a DIFFERENT author from the stored row. If the
            // query were built from the caller's copy, a contributor could name somebody else as
            // author and walk past the self-approval bar.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher);

            BibleReference storageBibleReference = CreateApprovableStorageBibleReference();
            storageBibleReference.CreatedBy = $"stored-{Guid.NewGuid()}";

            BibleReference inputBibleReference = CreateApprovalDecision(storageBibleReference.Id);
            inputBibleReference.CreatedBy = $"caller-{Guid.NewGuid()}";

            Guid expectedEntityId = storageBibleReference.Id;
            string expectedCreatedBy = storageBibleReference.CreatedBy;

            // when
            ApprovalDecisionQuery actualQuery =
                await CaptureApprovalDecisionQueryAsync(storageBibleReference, inputBibleReference);

            // then
            actualQuery.Should().NotBeNull();

            actualQuery.EntityType.Should().Be(EntityType.BibleReference);
            actualQuery.EntityId.Should().Be(expectedEntityId);

            // a bibleReference carries no content type, so its policy tier is (BibleReference, null)
            actualQuery.ContentType.Should().BeNull();

            actualQuery.EntityCreatedBy.Should().Be(expectedCreatedBy);
            actualQuery.EntityCreatedBy.Should().NotBe(inputBibleReference.CreatedBy);

            // a bibleReference has no confidence score — that is an association's input
            actualQuery.ConfidenceScore.Should().BeNull();

            // one subject: the bibleReference authorises from itself, keyed by its own type with no
            // content type
            actualQuery.RoleSubjects.Should().HaveCount(1);
            actualQuery.RoleSubjects[0].EntityType.Should().Be(nameof(EntityType.BibleReference));
            actualQuery.RoleSubjects[0].ContentType.Should().BeNull();

            actualQuery.IsBypassRequested.Should().BeFalse();
            actualQuery.BypassReason.Should().BeNull();
        }

        [Theory]
        [InlineData(ApprovalStatus.Approved, ApprovalDecision.Approve)]
        [InlineData(ApprovalStatus.Rejected, ApprovalDecision.Reject)]
        public async Task ShouldTellTheAccessBrokerWhichWayTheApprovalIsMovingOnApproveAsync(
            ApprovalStatus callerStatus,
            ApprovalDecision expectedDecision)
        {
            // given: rejecting withholds approval rather than granting it, so it satisfies no
            // threshold and waives nothing. Asking one question for both would leave a publisher
            // unable to reject the very row the threshold was failing to approve.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher);

            BibleReference storageBibleReference = CreateApprovableStorageBibleReference();

            BibleReference inputBibleReference = callerStatus == ApprovalStatus.Rejected
                ? CreateRejectionDecision(storageBibleReference.Id)
                : CreateApprovalDecision(storageBibleReference.Id);

            // when
            ApprovalDecisionQuery actualQuery =
                await CaptureApprovalDecisionQueryAsync(storageBibleReference, inputBibleReference);

            // then
            actualQuery.Should().NotBeNull();
            actualQuery.Decision.Should().Be(expectedDecision);
        }
    }
}
