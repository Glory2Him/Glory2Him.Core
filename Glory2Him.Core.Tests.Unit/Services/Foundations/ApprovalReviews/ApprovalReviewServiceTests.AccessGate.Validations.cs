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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Force.DeepCloner;
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.ApprovalReviews.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalReviews
{
    // The cross-entity half of "may this caller record a verdict?" — HR-1, the §7.7 rule 2b
    // open-round rule and the one-active-review bar all arrive through IAccessBroker, because
    // none of them can be answered from the review row alone. These tests are about what the
    // service does when that broker refuses.
    public partial class ApprovalReviewServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfTheAccessBrokerRefusesAsync()
        {
            // given: the caller holds Reviewer, so the row-local review-role gate passes and
            // the cross-entity decision is the only thing left that can refuse the add
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ApprovalReview inputApprovalReview =
                CreateApprovalReviewFiller(randomDateTimeOffset).Create();

            ApprovalReview auditAppliedApprovalReview = inputApprovalReview.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(
                    inputApprovalReview,
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync(auditAppliedApprovalReview);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedApprovalReview.CreatedBy);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            SetupAccessBrokerToRefuse(AccessDenialReason.ActiveReviewAlreadyRecorded);

            var unauthorizedApprovalReviewException =
                new UnauthorizedApprovalReviewException(
                    message: "The current user is not allowed to review approvals.");

            var expectedApprovalReviewValidationException =
                new ApprovalReviewValidationException(
                    message: "Approval review validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: unauthorizedApprovalReviewException);

            // when
            ValueTask<ApprovalReview> addApprovalReviewTask =
                this.approvalReviewService.AddApprovalReviewAsync(
                    inputApprovalReview,
                    TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualApprovalReviewValidationException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    addApprovalReviewTask.AsTask);

            // then
            actualApprovalReviewValidationException.Should().BeEquivalentTo(
                expectedApprovalReviewValidationException);

            // filing a new review is not an amendment, so the broker must be told so — the
            // amendment flag is what stops a first review being refused for finding itself
            this.accessBrokerMock.Verify(broker =>
                    broker.MayRecordApprovalReviewAsync(
                        auditAppliedApprovalReview.ApprovalId,
                        false,
                        It.IsAny<SecurityContext>(),
                        It.IsAny<CancellationToken>()),
                Times.Once);

            // nothing was inserted
            this.storageBrokerMock.Verify(broker =>
                    broker.InsertApprovalReviewAsync(
                        It.IsAny<ApprovalReview>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            // and nothing was announced
            this.eventBrokerMock.Verify(broker =>
                    broker.PublishApprovalReviewAsync(
                        It.IsAny<EventEnvelope<ApprovalReview>>(),
                        It.IsAny<ApprovalReviewEventOperation>()),
                Times.Never);

            this.loggingBrokerMock.Verify(broker =>
                    broker.LogWarningAsync(It.IsAny<string>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                    broker.LogErrorAsync(It.Is(
                        SameExceptionAs(expectedApprovalReviewValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyAddAuditValuesAsync(
                        inputApprovalReview,
                        It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldNotLeakTheAccessExplanationToTheCallerOnAddDenialAsync()
        {
            // given: the verdict's Explanation is composed from resolved policy values and the
            // denial reason names the rule that fired. Both belong in the server-side log and
            // nowhere in what is thrown (§14.5 rule 2), because exception messages and their
            // Data surface outward through a public event address.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ApprovalReview inputApprovalReview =
                CreateApprovalReviewFiller(randomDateTimeOffset).Create();

            ApprovalReview auditAppliedApprovalReview = inputApprovalReview.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(
                    inputApprovalReview,
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync(auditAppliedApprovalReview);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedApprovalReview.CreatedBy);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            SetupAccessBrokerToRefuse(AccessDenialReason.ActiveReviewAlreadyRecorded);

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
            ValueTask<ApprovalReview> addApprovalReviewTask =
                this.approvalReviewService.AddApprovalReviewAsync(
                    inputApprovalReview,
                    TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualApprovalReviewValidationException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    addApprovalReviewTask.AsTask);

            // then: wording of the service's own, naming no policy
            actualApprovalReviewValidationException.InnerException.Message.Should().Be(
                "The current user is not allowed to review approvals.");

            string thrownText =
                FlattenExceptionText(actualApprovalReviewValidationException);

            thrownText.Should().NotContain("refused");

            thrownText.Should().NotContain(
                nameof(AccessDenialReason.ActiveReviewAlreadyRecorded));

            actualApprovalReviewValidationException.Data.Count.Should().Be(0);
            actualApprovalReviewValidationException.InnerException.Data.Count.Should().Be(0);

            // the reason did go somewhere — to the warning, and before the throw
            logCallOrder.Should().HaveCount(2);
            logCallOrder[0].Should().StartWith("warning:");
            logCallOrder[1].Should().Be("error");

            logCallOrder[0].Should().Contain(
                nameof(AccessDenialReason.ActiveReviewAlreadyRecorded));

            logCallOrder[0].Should().Contain("refused");
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfTheAccessBrokerRefusesAsync()
        {
            // given: the caller's review names a DIFFERENT approval from the stored one. That
            // difference is the whole point — an amendment that could name its own ApprovalId
            // would let a reviewer change a verdict on a round that has already closed, so the
            // question must be asked about the STORED approval.
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ApprovalReview inputApprovalReview =
                CreateRandomModifyApprovalReview(randomDateTimeOffset, randomUserId);

            ApprovalReview auditAppliedApprovalReview = inputApprovalReview.DeepClone();
            ApprovalReview storageApprovalReview = auditAppliedApprovalReview.DeepClone();
            storageApprovalReview.ApprovalId = Guid.NewGuid();

            storageApprovalReview.UpdatedWhen =
                storageApprovalReview.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            // the audit broker hands back the row with the stored approval restored, which is
            // what lets the run reach the access broker at all — the ApprovalId pin refuses any
            // amendment that still names the caller's
            ApprovalReview auditPreservedApprovalReview =
                auditAppliedApprovalReview.DeepClone();

            auditPreservedApprovalReview.ApprovalId = storageApprovalReview.ApprovalId;

            inputApprovalReview.ApprovalId.Should().NotBe(storageApprovalReview.ApprovalId);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    inputApprovalReview,
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync(auditAppliedApprovalReview);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    auditAppliedApprovalReview.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalReview);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    auditAppliedApprovalReview,
                    storageApprovalReview))
                        .ReturnsAsync(auditPreservedApprovalReview);

            SetupAccessBrokerToRefuse(AccessDenialReason.ApprovalNotOpenForReview);

            var unauthorizedApprovalReviewException =
                new UnauthorizedApprovalReviewException(
                    message: "The current user is not allowed to review approvals.");

            var expectedApprovalReviewValidationException =
                new ApprovalReviewValidationException(
                    message: "Approval review validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: unauthorizedApprovalReviewException);

            // when
            ValueTask<ApprovalReview> modifyApprovalReviewTask =
                this.approvalReviewService.ModifyApprovalReviewAsync(
                    inputApprovalReview,
                    TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualApprovalReviewValidationException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    modifyApprovalReviewTask.AsTask);

            // then
            actualApprovalReviewValidationException.Should().BeEquivalentTo(
                expectedApprovalReviewValidationException);

            // the STORED approval, and the amendment flag set
            this.accessBrokerMock.Verify(broker =>
                    broker.MayRecordApprovalReviewAsync(
                        storageApprovalReview.ApprovalId,
                        true,
                        It.IsAny<SecurityContext>(),
                        It.IsAny<CancellationToken>()),
                Times.Once);

            // never the caller's, which is a different id on purpose
            this.accessBrokerMock.Verify(broker =>
                    broker.MayRecordApprovalReviewAsync(
                        inputApprovalReview.ApprovalId,
                        It.IsAny<bool>(),
                        It.IsAny<SecurityContext>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            // nothing was written
            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateApprovalReviewAsync(
                        It.IsAny<ApprovalReview>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            // and nothing was announced
            this.eventBrokerMock.Verify(broker =>
                    broker.PublishApprovalReviewAsync(
                        It.IsAny<EventEnvelope<ApprovalReview>>(),
                        It.IsAny<ApprovalReviewEventOperation>()),
                Times.Never);

            this.loggingBrokerMock.Verify(broker =>
                    broker.LogWarningAsync(It.IsAny<string>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                    broker.LogErrorAsync(It.Is(
                        SameExceptionAs(expectedApprovalReviewValidationException))),
                Times.Once);
        }

        [Fact]
        public async Task ShouldNotLeakTheAccessExplanationToTheCallerOnModifyDenialAsync()
        {
            // given
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ApprovalReview inputApprovalReview =
                CreateRandomModifyApprovalReview(randomDateTimeOffset, randomUserId);

            ApprovalReview auditAppliedApprovalReview = inputApprovalReview.DeepClone();
            ApprovalReview storageApprovalReview = auditAppliedApprovalReview.DeepClone();

            storageApprovalReview.UpdatedWhen =
                storageApprovalReview.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            ApprovalReview auditPreservedApprovalReview =
                auditAppliedApprovalReview.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    inputApprovalReview,
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync(auditAppliedApprovalReview);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    auditAppliedApprovalReview.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalReview);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    auditAppliedApprovalReview,
                    storageApprovalReview))
                        .ReturnsAsync(auditPreservedApprovalReview);

            SetupAccessBrokerToRefuse(AccessDenialReason.ApprovalNotOpenForReview);

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
            ValueTask<ApprovalReview> modifyApprovalReviewTask =
                this.approvalReviewService.ModifyApprovalReviewAsync(
                    inputApprovalReview,
                    TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualApprovalReviewValidationException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    modifyApprovalReviewTask.AsTask);

            // then
            actualApprovalReviewValidationException.InnerException.Message.Should().Be(
                "The current user is not allowed to review approvals.");

            string thrownText =
                FlattenExceptionText(actualApprovalReviewValidationException);

            thrownText.Should().NotContain("refused");

            thrownText.Should().NotContain(
                nameof(AccessDenialReason.ApprovalNotOpenForReview));

            actualApprovalReviewValidationException.Data.Count.Should().Be(0);
            actualApprovalReviewValidationException.InnerException.Data.Count.Should().Be(0);

            logCallOrder.Should().HaveCount(2);
            logCallOrder[0].Should().StartWith("warning:");
            logCallOrder[1].Should().Be("error");

            logCallOrder[0].Should().Contain(
                nameof(AccessDenialReason.ApprovalNotOpenForReview));

            logCallOrder[0].Should().Contain("refused");
        }

        // Everything a caller could read off what was thrown: every message in the chain and
        // every key and value in every Data dictionary. The leak guard asserts against this
        // rather than against the message alone, because Data surfaces outward too.
        private static string FlattenExceptionText(Exception exception)
        {
            var builder = new StringBuilder();

            for (Exception current = exception;
                current is not null;
                current = current.InnerException)
            {
                builder.AppendLine(current.Message);

                foreach (DictionaryEntry entry in current.Data)
                {
                    builder.AppendLine(Convert.ToString(entry.Key));

                    if (entry.Value is IEnumerable<string> values)
                    {
                        builder.AppendLine(string.Join(" ", values));

                        continue;
                    }

                    builder.AppendLine(Convert.ToString(entry.Value));
                }
            }

            return builder.ToString();
        }
    }
}
