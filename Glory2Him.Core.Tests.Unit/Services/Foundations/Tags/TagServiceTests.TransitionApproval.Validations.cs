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
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Foundations.Tags.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Tags
{
    public partial class TagServiceTests
    {
        public static TheoryData<string[]> NonPublisherRoleSets() =>
            new TheoryData<string[]>
            {
                new string[0],

                // a Reviewer holds the review tier and MUST still never set an approval status
                // (§8.6 HR-3) — the publisher tier deliberately excludes it
                new[] { Roles.Reviewer },
                new[] { Roles.TagReviewer },
            };

        [Fact]
        public async Task ShouldThrowValidationExceptionOnApproveIfTagIsNullAsync()
        {
            // given
            Tag nullTag = null;

            var nullTagException =
                new NullTagException(message: "Tag is null.");

            var expectedTagValidationException =
                new TagValidationException(
                    message: "Tag validation error occurred, fix the errors and try again.",
                    innerException: nullTagException);

            // when
            ValueTask<Tag> approveTask =
                this.tagService.TransitionTagApprovalAsync(
                    nullTag,
                    TestContext.Current.CancellationToken);

            TagValidationException actualException =
                await Assert.ThrowsAsync<TagValidationException>(approveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedTagValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectTagByIdAsync(
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

            Tag inputTag = CreateApprovalDecision(Guid.NewGuid());
            inputTag.ApprovalStatus = notATransitionTarget;
            inputTag.IsPublished = false;
            inputTag.PublishDate = null;

            // when
            ValueTask<Tag> approveTask =
                this.tagService.TransitionTagApprovalAsync(
                    inputTag,
                    TestContext.Current.CancellationToken);

            await Assert.ThrowsAsync<TagValidationException>(approveTask.AsTask);

            // then: the status never reached storage — the row was never even read
            this.storageBrokerMock.Verify(broker =>
                    broker.SelectTagByIdAsync(
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

            Tag inputTag = CreateRejectionDecision(Guid.NewGuid());
            inputTag.IsPublished = true;

            var invalidTagException =
                new InvalidTagException(
                    message: "Tag is invalid, fix the errors and try again.");

            invalidTagException.UpsertDataList(
                key: nameof(Tag.IsPublished),
                value: "Is published requires an approved tag.");

            var expectedTagValidationException =
                new TagValidationException(
                    message: "Tag validation error occurred, fix the errors and try again.",
                    innerException: invalidTagException);

            // when
            ValueTask<Tag> approveTask =
                this.tagService.TransitionTagApprovalAsync(
                    inputTag,
                    TestContext.Current.CancellationToken);

            TagValidationException actualException =
                await Assert.ThrowsAsync<TagValidationException>(approveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedTagValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectTagByIdAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishTagAsync(
                        It.IsAny<EventEnvelope<Tag>>(),
                        It.IsAny<TagEventOperation>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnApproveIfPublishDateWithoutPublicationAsync()
        {
            // given: a publish date without publication is a date nothing reads. DoApprove copies
            // PublishDate straight from the caller, so this rule is the only guard against a
            // phantom publish date on an unpublished row.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher);

            Tag inputTag = CreateRejectionDecision(Guid.NewGuid());
            inputTag.IsPublished = false;
            inputTag.PublishDate = GetRandomDateTimeOffset();

            var invalidTagException =
                new InvalidTagException(
                    message: "Tag is invalid, fix the errors and try again.");

            invalidTagException.UpsertDataList(
                key: nameof(Tag.PublishDate),
                value: "Publish date requires a published tag.");

            var expectedTagValidationException =
                new TagValidationException(
                    message: "Tag validation error occurred, fix the errors and try again.",
                    innerException: invalidTagException);

            // when
            ValueTask<Tag> approveTask =
                this.tagService.TransitionTagApprovalAsync(
                    inputTag,
                    TestContext.Current.CancellationToken);

            TagValidationException actualException =
                await Assert.ThrowsAsync<TagValidationException>(approveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedTagValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectTagByIdAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowNotFoundOnApproveIfTheRowIsMissingAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher);

            Tag inputTag = CreateApprovalDecision(Guid.NewGuid());

            this.storageBrokerMock.Setup(broker =>
                broker.SelectTagByIdAsync(
                    inputTag.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Tag)null);

            // when
            ValueTask<Tag> approveTask =
                this.tagService.TransitionTagApprovalAsync(
                    inputTag,
                    TestContext.Current.CancellationToken);

            await Assert.ThrowsAsync<TagValidationException>(approveTask.AsTask);

            // then: a missing row is decided against nothing
            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateTagAsync(
                        It.IsAny<Tag>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowNotFoundOnApproveIfTheRowIsSoftDeletedAsync()
        {
            // given: a soft-removed row is a takedown reported as not-found.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher);

            Tag storageTag = CreateApprovableStorageTag();
            storageTag.IsDeleted = true;

            Tag inputTag = CreateApprovalDecision(storageTag.Id);

            SetupTagStorageRead(storageTag);

            var notFoundTagException =
                new NotFoundTagException(
                    message: $"Tag not found with id: {storageTag.Id}.");

            var expectedTagValidationException =
                new TagValidationException(
                    message: "Tag validation error occurred, fix the errors and try again.",
                    innerException: notFoundTagException);

            // when
            ValueTask<Tag> approveTask =
                this.tagService.TransitionTagApprovalAsync(
                    inputTag,
                    TestContext.Current.CancellationToken);

            TagValidationException actualException =
                await Assert.ThrowsAsync<TagValidationException>(approveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedTagValidationException);

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

            Tag storageTag = CreateApprovableStorageTag();
            storageTag.ApprovalStatus = storageStatus;

            Tag inputTag = CreateApprovalDecision(storageTag.Id);

            SetupTagStorageRead(storageTag);
            SetupAccessBrokerToPermit();

            var invalidTagException =
                new InvalidTagException(
                    message: "Tag cannot be approved from status " +
                        $"{storageStatus}.");

            var expectedTagValidationException =
                new TagValidationException(
                    message: "Tag validation error occurred, fix the errors and try again.",
                    innerException: invalidTagException);

            // when
            ValueTask<Tag> approveTask =
                this.tagService.TransitionTagApprovalAsync(
                    inputTag,
                    TestContext.Current.CancellationToken);

            TagValidationException actualException =
                await Assert.ThrowsAsync<TagValidationException>(approveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedTagValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateTagAsync(
                        It.IsAny<Tag>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishTagAsync(
                        It.IsAny<EventEnvelope<Tag>>(),
                        It.IsAny<TagEventOperation>()),
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

            Tag storageTag = CreateApprovableStorageTag();
            Tag inputTag = CreateApprovalDecision(storageTag.Id);

            SetupTagStorageRead(storageTag);

            var unauthorizedTagException =
                new UnauthorizedTagException(
                    message: "The current user is not allowed to approve this tag.");

            var expectedTagValidationException =
                new TagValidationException(
                    message: "Tag validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedTagException);

            // when
            ValueTask<Tag> approveTask =
                this.tagService.TransitionTagApprovalAsync(
                    inputTag,
                    TestContext.Current.CancellationToken);

            TagValidationException actualException =
                await Assert.ThrowsAsync<TagValidationException>(approveTask.AsTask);

            // then: refused before the cross-entity decision is asked
            actualException.Should().BeEquivalentTo(expectedTagValidationException);

            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateTagAsync(
                        It.IsAny<Tag>(),
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

            Tag storageTag = CreateApprovableStorageTag();
            Tag inputTag = CreateApprovalDecision(storageTag.Id);

            SetupTagStorageRead(storageTag);
            SetupAccessBrokerToRefuse(AccessDenialReason.SelfApprovalNotPermitted);

            var unauthorizedTagException =
                new UnauthorizedTagException(
                    message: "The current user is not allowed to approve this tag.");

            var expectedTagValidationException =
                new TagValidationException(
                    message: "Tag validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedTagException);

            // when
            ValueTask<Tag> approveTask =
                this.tagService.TransitionTagApprovalAsync(
                    inputTag,
                    TestContext.Current.CancellationToken);

            TagValidationException actualException =
                await Assert.ThrowsAsync<TagValidationException>(approveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedTagValidationException);

            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateTagAsync(
                        It.IsAny<Tag>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishTagAsync(
                        It.IsAny<EventEnvelope<Tag>>(),
                        It.IsAny<TagEventOperation>()),
                Times.Never);

            this.loggingBrokerMock.Verify(broker =>
                    broker.LogWarningAsync(It.IsAny<string>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                    broker.LogErrorAsync(It.Is(
                        SameExceptionAs(expectedTagValidationException))),
                Times.Once);
        }

        [Fact]
        public async Task ShouldNotLeakTheAccessExplanationToTheCallerOnApproveDenialAsync()
        {
            // given: the verdict's Explanation and the denial reason name resolved policy;
            // exception messages and their Data surface outward through a public event address
            // (§14.5 rule 2), so neither may appear in anything thrown.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher);

            Tag storageTag = CreateApprovableStorageTag();
            Tag inputTag = CreateApprovalDecision(storageTag.Id);

            SetupTagStorageRead(storageTag);
            SetupAccessBrokerToRefuse(AccessDenialReason.ApprovalThresholdNotMet);

            // when
            ValueTask<Tag> approveTask =
                this.tagService.TransitionTagApprovalAsync(
                    inputTag,
                    TestContext.Current.CancellationToken);

            TagValidationException actualException =
                await Assert.ThrowsAsync<TagValidationException>(approveTask.AsTask);

            // then: the service's own wording, naming no policy
            actualException.InnerException.Message.Should().Be(
                "The current user is not allowed to approve this tag.");

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

            Tag storageTag = CreateApprovableStorageTag();
            Tag inputTag = CreateApprovalDecision(storageTag.Id);

            SetupTagStorageRead(storageTag);
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
            ValueTask<Tag> approveTask =
                this.tagService.TransitionTagApprovalAsync(
                    inputTag,
                    TestContext.Current.CancellationToken);

            await Assert.ThrowsAsync<TagValidationException>(approveTask.AsTask);

            // then: the warning lands first, and the error the throw produces second
            logCallOrder.Should().HaveCount(2);
            logCallOrder[0].Should().StartWith("warning:");
            logCallOrder[1].Should().Be("error");

            logCallOrder[0].Should().Contain(storageTag.Id.ToString());
            logCallOrder[0].Should().Contain(nameof(AccessDenialReason.ApprovalThresholdNotMet));
            logCallOrder[0].Should().Contain("refused");
        }

        [Fact]
        public async Task ShouldAskTheAccessBrokerAboutTheStoredTagOnApproveAsync()
        {
            // given: the caller's copy names a DIFFERENT author from the stored row. If the
            // query were built from the caller's copy, a contributor could name somebody else as
            // author and walk past the self-approval bar.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher);

            Tag storageTag = CreateApprovableStorageTag();
            storageTag.CreatedBy = $"stored-{Guid.NewGuid()}";

            Tag inputTag = CreateApprovalDecision(storageTag.Id);
            inputTag.CreatedBy = $"caller-{Guid.NewGuid()}";

            Guid expectedEntityId = storageTag.Id;
            string expectedCreatedBy = storageTag.CreatedBy;

            // when
            ApprovalDecisionQuery actualQuery =
                await CaptureApprovalDecisionQueryAsync(storageTag, inputTag);

            // then
            actualQuery.Should().NotBeNull();

            actualQuery.EntityType.Should().Be(EntityType.Tag);
            actualQuery.EntityId.Should().Be(expectedEntityId);

            // a tag carries no content type, so its policy tier is (Tag, null)
            actualQuery.ContentType.Should().BeNull();

            actualQuery.EntityCreatedBy.Should().Be(expectedCreatedBy);
            actualQuery.EntityCreatedBy.Should().NotBe(inputTag.CreatedBy);

            // a tag has no confidence score — that is an association's input
            actualQuery.ConfidenceScore.Should().BeNull();

            // one subject: the tag authorises from itself, keyed by its own type with no
            // content type
            actualQuery.RoleSubjects.Should().HaveCount(1);
            actualQuery.RoleSubjects[0].EntityType.Should().Be(nameof(EntityType.Tag));
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

            Tag storageTag = CreateApprovableStorageTag();

            Tag inputTag = callerStatus == ApprovalStatus.Rejected
                ? CreateRejectionDecision(storageTag.Id)
                : CreateApprovalDecision(storageTag.Id);

            // when
            ApprovalDecisionQuery actualQuery =
                await CaptureApprovalDecisionQueryAsync(storageTag, inputTag);

            // then
            actualQuery.Should().NotBeNull();
            actualQuery.Decision.Should().Be(expectedDecision);
        }
    }
}
