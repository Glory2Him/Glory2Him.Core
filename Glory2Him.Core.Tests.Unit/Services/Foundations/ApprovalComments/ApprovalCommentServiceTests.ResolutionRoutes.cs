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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.ApprovalComments;
using Glory2Him.Core.Models.Foundations.ApprovalComments.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalComments
{
    public partial class ApprovalCommentServiceTests
    {
        // ── IsResolved is written on three legitimate routes, and that is the design ──
        //
        // The field records whether a comment is SETTLED — whether it still requires something
        // before the approval can proceed. Not every comment asks for anything: an observation,
        // or a reviewer recording rationale so others can see the thinking behind a verdict, is
        // informational and never blocks (§7.8). So "resolved" does not mean "a question was
        // answered", and both birth values are legitimate.
        //
        //   Add     — the author, choosing whether their new comment is outstanding
        //   Modify  — the owner, on their own row
        //   Resolve — the owner OR an administrator
        //
        // What Resolve adds is the ADMIN route, not exclusivity over the field. Pinning
        // IsResolved against storage on modify would leave the owner unable to change something
        // that is theirs; pinning it false on add would make it impossible to leave a remark
        // without blocking the approval.
        //
        // The routes publish different facts, which costs nothing because the approval workflow
        // subscribes to every ApprovalComment address and re-tests the §8.5 conditions on each
        // (§10.17 inbound item (a)). These tests exist so that contract cannot be narrowed by
        // accident: pinning the field fails them, and collapsing the addresses into one fails
        // the last.

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShouldLetTheOwnerChangeIsResolvedThroughModifyAsync(bool newResolution)
        {
            // given: the owner edits their comment and settles (or re-opens) it in the same
            // write. Modify carries IsResolved through unpinned, in both directions.
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ApprovalComment inputApprovalComment =
                CreateRandomModifyApprovalComment(randomDateTimeOffset, randomUserId);

            inputApprovalComment.IsResolved = newResolution;

            ApprovalComment auditAppliedApprovalComment = inputApprovalComment.DeepClone();

            ApprovalComment storageApprovalComment = auditAppliedApprovalComment.DeepClone();

            storageApprovalComment.UpdatedWhen =
                storageApprovalComment.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            // the stored row holds the OPPOSITE value — this is the change under test
            storageApprovalComment.IsResolved = newResolution is false;

            ApprovalComment auditPreservedApprovalComment = auditAppliedApprovalComment.DeepClone();
            ApprovalComment updatedApprovalComment = auditPreservedApprovalComment.DeepClone();

            ApprovalComment savedApprovalComment = null;

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    inputApprovalComment,
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync(auditAppliedApprovalComment);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    auditAppliedApprovalComment.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    auditAppliedApprovalComment,
                    storageApprovalComment))
                        .ReturnsAsync(auditPreservedApprovalComment);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalCommentAsync(
                    It.IsAny<ApprovalComment>(),
                    It.IsAny<CancellationToken>()))
                        .Callback<ApprovalComment, CancellationToken>(
                            (entity, _) => savedApprovalComment = entity.DeepClone())
                        .ReturnsAsync(updatedApprovalComment);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalCommentAsync(
                    It.IsAny<EventEnvelope<ApprovalComment>>(),
                    It.IsAny<ApprovalCommentEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<ApprovalComment>>(
                            new EventPublishResult<ApprovalComment>()));

            // when
            ApprovalComment actualApprovalComment =
                await this.approvalCommentService.ModifyApprovalCommentAsync(
                    inputApprovalComment,
                    TestContext.Current.CancellationToken);

            // then: the caller's value reached storage rather than being pinned to the stored one
            savedApprovalComment.Should().NotBeNull();
            savedApprovalComment.IsResolved.Should().Be(newResolution);
            actualApprovalComment.IsResolved.Should().Be(newResolution);

            // and it is announced as a modification, which the approval workflow also subscribes
            // to — the gate move is never silent whichever route carried it
            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalCommentAsync(
                    It.IsAny<EventEnvelope<ApprovalComment>>(),
                    ApprovalCommentEventOperation.Modified),
                Times.Once);
        }

        [Fact]
        public async Task ShouldRefuseANonOwnerTheModifyRouteToIsResolvedAsync()
        {
            // given: the Administrators route to IsResolved is Resolve, and only Resolve. Modify stays
            // owner-only for every field — an administrator who could reach it here would have the
            // author's words as well, which is exactly what §14.7 rule 5 withdraws.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();

            ApprovalComment inputApprovalComment =
                CreateRandomModifyApprovalComment(randomDateTimeOffset, randomUserId);

            inputApprovalComment.IsResolved = true;

            ApprovalComment storageApprovalComment = inputApprovalComment.DeepClone();

            storageApprovalComment.UpdatedWhen =
                storageApprovalComment.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            storageApprovalComment.IsResolved = false;

            // the acting Administrators is NOT the comment's author
            storageApprovalComment.CreatedBy = GetRandomString();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    inputApprovalComment,
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync(inputApprovalComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    inputApprovalComment.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalComment);

            var unauthorizedApprovalCommentException = new UnauthorizedApprovalCommentException(
                message: "The current user is not allowed to modify this approval comment.");

            var expectedApprovalCommentValidationException = new ApprovalCommentValidationException(
                message: "Approval comment validation error occurred, fix the errors and try again.",
                innerException: unauthorizedApprovalCommentException);

            // when
            ValueTask<ApprovalComment> modifyTask =
                this.approvalCommentService.ModifyApprovalCommentAsync(
                    inputApprovalComment,
                    TestContext.Current.CancellationToken);

            ApprovalCommentValidationException actualException =
                await Assert.ThrowsAsync<ApprovalCommentValidationException>(modifyTask.AsTask);

            // then: refused by the OWNERSHIP gate specifically, not by some other validation
            actualException.Should().BeEquivalentTo(expectedApprovalCommentValidationException);

            // nothing written
            this.storageBrokerMock.Verify(broker =>
                broker.UpdateApprovalCommentAsync(
                    It.IsAny<ApprovalComment>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShouldCarryTheCallersIsResolvedThroughAddAsync(bool bornResolved)
        {
            // given: a comment born SETTLED is an intended capability, not a missing validation.
            // Not every comment asks for anything — an observation, or a reviewer recording
            // rationale so others can see the thinking behind a verdict, requires no response and
            // must not hold the approval shut (§7.8). Add therefore applies no rule to
            // IsResolved, and both birth values are correct.
            //
            // DO NOT "fix" this by pinning IsResolved false at creation the way IsDeleted is.
            // The analogy is false: IsDeleted has exactly one legitimate birth value and this
            // field has two. Pinning it would make it impossible to leave a remark without
            // blocking the approval, which is the whole case the informational comment serves.
            //
            // The column still defaults to false, so a caller who says nothing gets the
            // fail-closed answer: silence means outstanding.
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ApprovalComment inputApprovalComment =
                CreateApprovalCommentFiller(randomDateTimeOffset, randomUserId).Create();

            inputApprovalComment.IsResolved = bornResolved;

            ApprovalComment savedApprovalComment = null;

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(
                    It.IsAny<ApprovalComment>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync(inputApprovalComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(inputApprovalComment.CreatedBy);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.InsertApprovalCommentAsync(
                    It.IsAny<ApprovalComment>(),
                    It.IsAny<CancellationToken>()))
                        .Callback<ApprovalComment, CancellationToken>(
                            (entity, _) => savedApprovalComment = entity.DeepClone())
                        .ReturnsAsync((ApprovalComment entity, CancellationToken _) => entity);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalCommentAsync(
                    It.IsAny<EventEnvelope<ApprovalComment>>(),
                    It.IsAny<ApprovalCommentEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<ApprovalComment>>(
                            new EventPublishResult<ApprovalComment>()));

            // when
            await this.approvalCommentService.AddApprovalCommentAsync(
                inputApprovalComment,
                TestContext.Current.CancellationToken);

            // then
            savedApprovalComment.Should().NotBeNull();
            savedApprovalComment.IsResolved.Should().Be(bornResolved);

            // announced as an addition — the approval workflow subscribes to this address too,
            // because a comment born UNRESOLVED newly blocks its approval (§10.17 inbound (a))
            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalCommentAsync(
                    It.IsAny<EventEnvelope<ApprovalComment>>(),
                    ApprovalCommentEventOperation.Added),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalCommentAsync(
                    It.IsAny<EventEnvelope<ApprovalComment>>(),
                    ApprovalCommentEventOperation.Resolved),
                Times.Never);
        }

        [Fact]
        public async Task ShouldAnnounceResolutionOnDistinctAddressesPerRouteAsync()
        {
            // given: the same field change, made through the transition instead, publishes
            // -Resolved rather than -Modified. Both addresses are live and the approval workflow
            // must subscribe to both (§10.17 inbound item (a)); a consumer that watched only one
            // would miss half the gate moves.
            string randomUserId = GetRandomString();

            ApprovalComment storageApprovalComment = CreateRandomApprovalComment();
            storageApprovalComment.CreatedBy = randomUserId;
            storageApprovalComment.IsResolved = false;

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    storageApprovalComment.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<ApprovalComment>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync((ApprovalComment entity, SecurityContext _) => entity);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalCommentAsync(
                    It.IsAny<ApprovalComment>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((ApprovalComment entity, CancellationToken _) => entity);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalCommentAsync(
                    It.IsAny<EventEnvelope<ApprovalComment>>(),
                    It.IsAny<ApprovalCommentEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<ApprovalComment>>(
                            new EventPublishResult<ApprovalComment>()));

            // when
            await this.approvalCommentService.ResolveApprovalCommentAsync(
                storageApprovalComment.Id,
                isResolved: true,
                TestContext.Current.CancellationToken);

            // then
            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalCommentAsync(
                    It.IsAny<EventEnvelope<ApprovalComment>>(),
                    ApprovalCommentEventOperation.Resolved),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalCommentAsync(
                    It.IsAny<EventEnvelope<ApprovalComment>>(),
                    ApprovalCommentEventOperation.Modified),
                Times.Never);
        }
    }
}
