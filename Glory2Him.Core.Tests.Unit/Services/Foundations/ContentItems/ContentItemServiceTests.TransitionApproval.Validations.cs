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
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItems
{
    public partial class ContentItemServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnApproveIfContentItemIsNullAsync()
        {
            // given
            ContentItem nullContentItem = null;

            var nullContentItemException =
                new NullContentItemException(message: "Content item is null.");

            var expectedContentItemValidationException =
                new ContentItemValidationException(
                    message: "Content item validation error occurred, fix the errors and try again.",
                    innerException: nullContentItemException);

            // when
            ValueTask<ContentItem> approveTask =
                this.contentItemService.TransitionContentItemApprovalAsync(
                    nullContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualException =
                await Assert.ThrowsAsync<ContentItemValidationException>(approveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedContentItemValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectContentItemByIdAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
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
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);

            ContentItem inputContentItem = CreateApprovalDecision(Guid.NewGuid());
            inputContentItem.ApprovalStatus = notATransitionTarget;
            inputContentItem.IsPublished = false;
            inputContentItem.PublishDate = null;

            // when
            ValueTask<ContentItem> approveTask =
                this.contentItemService.TransitionContentItemApprovalAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualException =
                await Assert.ThrowsAsync<ContentItemValidationException>(approveTask.AsTask);

            // then: the status never reached storage — the row was never even read
            this.storageBrokerMock.Verify(broker =>
                    broker.SelectContentItemByIdAsync(
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
            // IsPublished straight from the caller), and it must fire before the row is read, so
            // a rejected-but-published payload never touches storage.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);

            ContentItem inputContentItem = CreateRejectionDecision(Guid.NewGuid());
            inputContentItem.IsPublished = true;

            var invalidContentItemException =
                new InvalidContentItemException(
                    message: "Content item is invalid, fix the errors and try again.");

            invalidContentItemException.UpsertDataList(
                key: nameof(ContentItem.IsPublished),
                value: "Is published requires an approved content item.");

            var expectedContentItemValidationException =
                new ContentItemValidationException(
                    message: "Content item validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemException);

            // when
            ValueTask<ContentItem> approveTask =
                this.contentItemService.TransitionContentItemApprovalAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualException =
                await Assert.ThrowsAsync<ContentItemValidationException>(approveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedContentItemValidationException);

            // rejected-but-published never reached storage and never announced anything
            this.storageBrokerMock.Verify(broker =>
                    broker.SelectContentItemByIdAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateContentItemAsync(
                        It.IsAny<ContentItem>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishContentItemAsync(
                        It.IsAny<EventEnvelope<ContentItem>>(),
                        It.IsAny<ContentItemEventOperation>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnApproveIfPublishDateWithoutPublicationAsync()
        {
            // given: a publish date without publication is a date nothing reads. DoApprove copies
            // PublishDate straight from the caller, so this rule is the only thing stopping a
            // phantom publish date landing on an unpublished row.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);

            ContentItem inputContentItem = CreateRejectionDecision(Guid.NewGuid());
            inputContentItem.IsPublished = false;
            inputContentItem.PublishDate = GetRandomDateTimeOffset();

            var invalidContentItemException =
                new InvalidContentItemException(
                    message: "Content item is invalid, fix the errors and try again.");

            invalidContentItemException.UpsertDataList(
                key: nameof(ContentItem.PublishDate),
                value: "Publish date requires a published content item.");

            var expectedContentItemValidationException =
                new ContentItemValidationException(
                    message: "Content item validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemException);

            // when
            ValueTask<ContentItem> approveTask =
                this.contentItemService.TransitionContentItemApprovalAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualException =
                await Assert.ThrowsAsync<ContentItemValidationException>(approveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedContentItemValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectContentItemByIdAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateContentItemAsync(
                        It.IsAny<ContentItem>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowNotFoundOnApproveIfTheRowIsMissingAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);

            ContentItem inputContentItem = CreateApprovalDecision(Guid.NewGuid());

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemByIdAsync(
                    inputContentItem.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((ContentItem)null);

            // when
            ValueTask<ContentItem> approveTask =
                this.contentItemService.TransitionContentItemApprovalAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            await Assert.ThrowsAsync<ContentItemValidationException>(approveTask.AsTask);

            // then: a missing row is decided against nothing
            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateContentItemAsync(
                        It.IsAny<ContentItem>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowNotFoundOnApproveIfTheRowIsSoftDeletedAsync()
        {
            // given: a soft-removed row is a takedown reported as not-found, so a removed id is
            // indistinguishable from one that never existed (matches the read posture).
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);

            ContentItem storageContentItem = CreateApprovableStorageContentItem();
            storageContentItem.IsDeleted = true;

            ContentItem inputContentItem = CreateApprovalDecision(storageContentItem.Id);

            SetupContentItemStorageRead(storageContentItem);

            var notFoundContentItemException =
                new NotFoundContentItemException(
                    message: $"Content item not found with id: {storageContentItem.Id}.");

            var expectedContentItemValidationException =
                new ContentItemValidationException(
                    message: "Content item validation error occurred, fix the errors and try again.",
                    innerException: notFoundContentItemException);

            // when
            ValueTask<ContentItem> approveTask =
                this.contentItemService.TransitionContentItemApprovalAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualException =
                await Assert.ThrowsAsync<ContentItemValidationException>(approveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedContentItemValidationException);

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
            // transitionable — by an administrator, through the override — and are covered there.
            //
            // The tier and the access decision pass first (global Publishers, permissive fixture),
            // so this proves the state gate stands on its own.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);

            ContentItem storageContentItem = CreateApprovableStorageContentItem();
            storageContentItem.ApprovalStatus = storageStatus;

            ContentItem inputContentItem = CreateApprovalDecision(storageContentItem.Id);

            SetupContentItemStorageRead(storageContentItem);
            SetupAccessBrokerToPermit();

            var invalidContentItemException =
                new InvalidContentItemException(
                    message: "Content item cannot be approved from status " +
                        $"{storageStatus}.");

            var expectedContentItemValidationException =
                new ContentItemValidationException(
                    message: "Content item validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemException);

            // when
            ValueTask<ContentItem> approveTask =
                this.contentItemService.TransitionContentItemApprovalAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualException =
                await Assert.ThrowsAsync<ContentItemValidationException>(approveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedContentItemValidationException);

            // nothing was written and nothing was announced
            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateContentItemAsync(
                        It.IsAny<ContentItem>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishContentItemAsync(
                        It.IsAny<EventEnvelope<ContentItem>>(),
                        It.IsAny<ContentItemEventOperation>()),
                Times.Never);
        }

        public static TheoryData<string[]> NonPublisherRoleSets() =>
            new TheoryData<string[]>
            {
                new string[0],

                // a reviewer holds the review tier and MUST still never set an approval status
                // (§8.6 HR-3) — the publisher tier deliberately excludes it
                new[] { Roles.Reviewers },
                new[] { Roles.ContentItemReviewers },
            };

        [Theory]
        [MemberData(nameof(NonPublisherRoleSets))]
        public async Task ShouldThrowUnauthorizedOnApproveIfCallerLacksThePublisherTierAsync(
            string[] roles)
        {
            // given: the row-local publisher-tier check is what makes an unauthorised caller
            // cost one role comparison instead of a table read, and it is where HR-3 lands — a
            // Reviewers is refused before the access decision is ever asked.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(roles);

            ContentItem storageContentItem = CreateApprovableStorageContentItem();
            ContentItem inputContentItem = CreateApprovalDecision(storageContentItem.Id);

            SetupContentItemStorageRead(storageContentItem);

            var unauthorizedContentItemException =
                new UnauthorizedContentItemException(
                    message: "The current user is not allowed to approve this content item.");

            var expectedContentItemValidationException =
                new ContentItemValidationException(
                    message: "Content item validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedContentItemException);

            // when
            ValueTask<ContentItem> approveTask =
                this.contentItemService.TransitionContentItemApprovalAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualException =
                await Assert.ThrowsAsync<ContentItemValidationException>(approveTask.AsTask);

            // then: refused before the cross-entity decision is asked
            actualException.Should().BeEquivalentTo(expectedContentItemValidationException);

            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateContentItemAsync(
                        It.IsAny<ContentItem>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnApproveIfTheAccessBrokerRefusesAsync()
        {
            // given: the caller holds the global Publishers role, so the row-local tier check
            // passes and the cross-entity decision is the ONLY thing left that can refuse the
            // approve (HR-2 self-approval lives behind the access broker).
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);

            ContentItem storageContentItem = CreateApprovableStorageContentItem();
            ContentItem inputContentItem = CreateApprovalDecision(storageContentItem.Id);

            SetupContentItemStorageRead(storageContentItem);
            SetupAccessBrokerToRefuse(AccessDenialReason.SelfApprovalNotPermitted);

            var unauthorizedContentItemException =
                new UnauthorizedContentItemException(
                    message: "The current user is not allowed to approve this content item.");

            var expectedContentItemValidationException =
                new ContentItemValidationException(
                    message: "Content item validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedContentItemException);

            // when
            ValueTask<ContentItem> approveTask =
                this.contentItemService.TransitionContentItemApprovalAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualException =
                await Assert.ThrowsAsync<ContentItemValidationException>(approveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedContentItemValidationException);

            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
                Times.Once);

            // nothing written, nothing announced
            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateContentItemAsync(
                        It.IsAny<ContentItem>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishContentItemAsync(
                        It.IsAny<EventEnvelope<ContentItem>>(),
                        It.IsAny<ContentItemEventOperation>()),
                Times.Never);

            this.loggingBrokerMock.Verify(broker =>
                    broker.LogWarningAsync(It.IsAny<string>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                    broker.LogErrorAsync(It.Is(
                        SameExceptionAs(expectedContentItemValidationException))),
                Times.Once);
        }

        [Fact]
        public async Task ShouldNotLeakTheAccessExplanationToTheCallerOnApproveDenialAsync()
        {
            // given: the verdict's Explanation is composed from resolved policy values and the
            // denial reason names the rule. Exception messages and their Data surface outward
            // through a public event address (§14.5 rule 2), so neither may appear in anything
            // thrown.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);

            ContentItem storageContentItem = CreateApprovableStorageContentItem();
            ContentItem inputContentItem = CreateApprovalDecision(storageContentItem.Id);

            SetupContentItemStorageRead(storageContentItem);
            SetupAccessBrokerToRefuse(AccessDenialReason.ApprovalThresholdNotMet);

            // when
            ValueTask<ContentItem> approveTask =
                this.contentItemService.TransitionContentItemApprovalAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualException =
                await Assert.ThrowsAsync<ContentItemValidationException>(approveTask.AsTask);

            // then: the service's own wording, naming no policy
            actualException.InnerException.Message.Should().Be(
                "The current user is not allowed to approve this content item.");

            string thrownText = FlattenExceptionText(actualException);

            // the explanation the refusing verdict carried
            thrownText.Should().NotContain("refused");

            // and the name of the rule that fired
            thrownText.Should().NotContain(nameof(AccessDenialReason.ApprovalThresholdNotMet));

            actualException.Data.Count.Should().Be(0);
            actualException.InnerException.Data.Count.Should().Be(0);
        }

        [Fact]
        public async Task ShouldLogTheDenialAsAWarningBeforeThrowingOnApproveAsync()
        {
            // given: §14.5 — the true reason is recorded server-side and the caller is told
            // nothing about the policy. It has to be recorded BEFORE the throw, because the
            // throw is what discards the verdict.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);

            ContentItem storageContentItem = CreateApprovableStorageContentItem();
            ContentItem inputContentItem = CreateApprovalDecision(storageContentItem.Id);

            SetupContentItemStorageRead(storageContentItem);
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
            ValueTask<ContentItem> approveTask =
                this.contentItemService.TransitionContentItemApprovalAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            await Assert.ThrowsAsync<ContentItemValidationException>(approveTask.AsTask);

            // then: the warning lands first, and the error the throw produces second
            logCallOrder.Should().HaveCount(2);
            logCallOrder[0].Should().StartWith("warning:");
            logCallOrder[1].Should().Be("error");

            // the log is the one place the id and the reason belong
            logCallOrder[0].Should().Contain(storageContentItem.Id.ToString());
            logCallOrder[0].Should().Contain(nameof(AccessDenialReason.ApprovalThresholdNotMet));
            logCallOrder[0].Should().Contain("refused");
        }

        [Fact]
        public async Task ShouldAskTheAccessBrokerAboutTheStoredContentItemOnApproveAsync()
        {
            // given: the caller's copy names a DIFFERENT author and content type from the stored
            // row. That difference is what gives the assertion its meaning — if the query were
            // built from the caller's copy, a contributor could name somebody else as author, or
            // claim a content type they hold a publisher role for, and walk past the bar.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);

            ContentItem storageContentItem = CreateApprovableStorageContentItem();
            storageContentItem.ContentType = ContentType.Testimony;
            storageContentItem.CreatedBy = $"stored-{Guid.NewGuid()}";

            ContentItem inputContentItem = CreateApprovalDecision(storageContentItem.Id);
            inputContentItem.ContentType = ContentType.Story;
            inputContentItem.CreatedBy = $"caller-{Guid.NewGuid()}";

            Guid expectedEntityId = storageContentItem.Id;
            string expectedCreatedBy = storageContentItem.CreatedBy;

            // when
            ApprovalDecisionQuery actualQuery =
                await CaptureApprovalDecisionQueryAsync(storageContentItem, inputContentItem);

            // then
            actualQuery.Should().NotBeNull();

            actualQuery.EntityType.Should().Be(EntityType.ContentItem);
            actualQuery.EntityId.Should().Be(expectedEntityId);

            // a content item's policy tier IS keyed by its content type (unlike an association's)
            actualQuery.ContentType.Should().Be(ContentType.Testimony);

            actualQuery.EntityCreatedBy.Should().Be(expectedCreatedBy);
            actualQuery.EntityCreatedBy.Should().NotBe(inputContentItem.CreatedBy);

            // a content item has no confidence score — that is an association's input
            actualQuery.ConfidenceScore.Should().BeNull();

            // one subject: the item authorises from itself, keyed by its own type and content
            // type
            actualQuery.RoleSubjects.Should().HaveCount(1);
            actualQuery.RoleSubjects[0].EntityType.Should().Be(nameof(EntityType.ContentItem));
            actualQuery.RoleSubjects[0].ContentType.Should().Be(nameof(ContentType.Testimony));

            // bypass is its own operation and this is not it
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
            // given: the two directions are gated differently — rejecting withholds approval
            // rather than granting it, so it satisfies no threshold and waives nothing. Asking
            // one question for both would leave a publisher unable to reject the very row the
            // threshold was failing to approve.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);

            ContentItem storageContentItem = CreateApprovableStorageContentItem();

            ContentItem inputContentItem = callerStatus == ApprovalStatus.Rejected
                ? CreateRejectionDecision(storageContentItem.Id)
                : CreateApprovalDecision(storageContentItem.Id);

            // when
            ApprovalDecisionQuery actualQuery =
                await CaptureApprovalDecisionQueryAsync(storageContentItem, inputContentItem);

            // then
            actualQuery.Should().NotBeNull();
            actualQuery.Decision.Should().Be(expectedDecision);
        }
    }
}
