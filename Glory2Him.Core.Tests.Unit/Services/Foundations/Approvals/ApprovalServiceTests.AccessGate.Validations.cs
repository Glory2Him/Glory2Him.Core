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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Force.DeepCloner;
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Foundations.Approvals.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Approvals
{
    // The cross-entity half of "may this caller amend the approval record?" — the row-local role
    // test cannot see which entity an approval targets, so a bare Tag-Reviewer clears it for any
    // approval at all. These tests are about what the service does when the broker refuses, and
    // about the order the two halves run in.
    public partial class ApprovalServiceTests
    {
        /// <summary>
        /// #190's headline sentence, at the surface it was written about. A bare
        /// <c>Tag-Reviewer</c> passes the row-local suffix test — a review role is a review role
        /// as far as that check can tell — and is stopped only here, by the decision that has the
        /// entity behind the approval in hand.
        /// </summary>
        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfTheAccessBrokerRefusesAndLogItAsync()
        {
            // given: the caller holds a scoped review role, so the row-local gate passes and the
            // cross-entity decision is the only thing left that can refuse
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();

            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.TagReviewer);

            Approval randomApproval = CreateRandomModifyApproval(randomDateTimeOffset, randomUserId);
            Approval inputApproval = randomApproval;
            Approval storageApproval = randomApproval.DeepClone();
            storageApproval.CreatedBy = GetRandomString();

            storageApproval.UpdatedWhen =
                storageApproval.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            SetupModifyApprovalRun(inputApproval, storageApproval, randomDateTimeOffset);
            SetupAccessBrokerToRefuseAmendment(AccessDenialReason.NotInReviewTier);

            var unauthorizedApprovalException = new UnauthorizedApprovalException(
                message: "The current user is not allowed to modify this approval.");

            var expectedApprovalValidationException = new ApprovalValidationException(
                message: "Approval validation error occurred, fix the errors and try again.",
                innerException: unauthorizedApprovalException);

            // when
            ValueTask<Approval> modifyApprovalTask =
                this.approvalService.ModifyApprovalAsync(
                    inputApproval,
                    TestContext.Current.CancellationToken);

            ApprovalValidationException actualApprovalValidationException =
                await Assert.ThrowsAsync<ApprovalValidationException>(
                    modifyApprovalTask.AsTask);

            // then
            actualApprovalValidationException.Should().BeEquivalentTo(
                expectedApprovalValidationException);

            // asked about the STORED approval, never a caller-supplied value
            this.accessBrokerMock.Verify(broker =>
                    broker.MayAmendApprovalAsync(
                        storageApproval.Id,
                        It.IsAny<SecurityContext>(),
                        It.IsAny<CancellationToken>()),
                Times.Once);

            // nothing was written and no fact was published
            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateApprovalAsync(
                        It.IsAny<Approval>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishApprovalAsync(
                        It.IsAny<EventEnvelope<Approval>>(),
                        It.IsAny<ApprovalEventOperation>()),
                Times.Never);
        }

        /// <summary>
        /// §14.5 rule 2: the verdict's explanation is composed from resolved policy values and
        /// the denial reason names the rule that fired. Both belong in the server-side log and
        /// nowhere in what is thrown, because exception messages and their <c>Data</c> surface
        /// outward through a public event address.
        /// </summary>
        [Fact]
        public async Task ShouldNotLeakTheAccessExplanationToTheCallerOnModifyDenialAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();

            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.TagReviewer);

            Approval randomApproval = CreateRandomModifyApproval(randomDateTimeOffset, randomUserId);
            Approval inputApproval = randomApproval;
            Approval storageApproval = randomApproval.DeepClone();
            storageApproval.CreatedBy = GetRandomString();

            storageApproval.UpdatedWhen =
                storageApproval.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            SetupModifyApprovalRun(inputApproval, storageApproval, randomDateTimeOffset);
            SetupAccessBrokerToRefuseAmendment(AccessDenialReason.NotInReviewTier);

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
            ValueTask<Approval> modifyApprovalTask =
                this.approvalService.ModifyApprovalAsync(
                    inputApproval,
                    TestContext.Current.CancellationToken);

            ApprovalValidationException actualApprovalValidationException =
                await Assert.ThrowsAsync<ApprovalValidationException>(
                    modifyApprovalTask.AsTask);

            // then: wording of the service's own, naming no policy
            actualApprovalValidationException.InnerException.Message.Should().Be(
                "The current user is not allowed to modify this approval.");

            string thrownText =
                FlattenApprovalExceptionText(actualApprovalValidationException);

            thrownText.Should().NotContain(nameof(AccessDenialReason.NotInReviewTier));
            thrownText.Should().NotContain("review tier for this entity");

            actualApprovalValidationException.Data.Count.Should().Be(0);
            actualApprovalValidationException.InnerException.Data.Count.Should().Be(0);

            // the reason did go somewhere — to the warning, and before the throw
            logCallOrder.Should().HaveCount(2);
            logCallOrder[0].Should().StartWith("warning:");
            logCallOrder[1].Should().Be("error");

            logCallOrder[0].Should().Contain(nameof(AccessDenialReason.NotInReviewTier));
            logCallOrder[0].Should().Contain("review tier for this entity");
        }

        /// <summary>
        /// A role-less submitter may still amend their own approval — §14.7 posture D rule 3's
        /// "resubmission by the submitter". This is the case the two gates nearly destroyed: tier
        /// 1 admits owner-or-review-role, and if tier 2 answered only the tier half the AND of
        /// the two would collapse to review-tier-only and lock every submitter out of their own
        /// row. Run against a stub that MIRRORS the real decision rather than the fixture's
        /// blanket permit, because a permissive default cannot tell the two designs apart.
        /// </summary>
        [Fact]
        public async Task ShouldAllowTheSubmitterToModifyTheirOwnApprovalWithNoRolesAsync()
        {
            // given: the arrangement of the happy path, with ONE difference — the caller holds
            // no roles at all and is admitted purely as the approval's submitter
            string submitterUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Approval randomApproval =
                CreateRandomModifyApproval(randomDateTimeOffset, submitterUserId);

            Approval inputApproval = randomApproval;
            Approval auditAppliedApproval = inputApproval.DeepClone();
            Approval storageApproval = auditAppliedApproval.DeepClone();

            storageApproval.UpdatedWhen =
                storageApproval.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            Approval auditPreservedApproval = auditAppliedApproval.DeepClone();
            Approval updatedApproval = auditPreservedApproval.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(submitterUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(inputApproval, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedApproval);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalByIdAsync(
                    auditAppliedApproval.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApproval);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    auditAppliedApproval,
                    storageApproval))
                        .ReturnsAsync(auditPreservedApproval);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalAsync(auditPreservedApproval, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(updatedApproval);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalAsync(
                    It.IsAny<EventEnvelope<Approval>>(),
                    ApprovalEventOperation.Modified))
                    .Returns(new ValueTask<EventPublishResult<Approval>>(
                        new EventPublishResult<Approval>()));

            // the decision as production computes it — owner OR review tier — rather than the
            // fixture's blanket permit, which cannot tell the two designs apart
            SetupAccessBrokerToMirrorTheAmendmentDecision(
                approvalCreatedBy: storageApproval.CreatedBy,
                actorUserId: submitterUserId);

            // when
            Approval actualApproval =
                await this.approvalService.ModifyApprovalAsync(
                    inputApproval,
                    TestContext.Current.CancellationToken);

            // then: admitted, and the write happened
            actualApproval.Should().NotBeNull();

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateApprovalAsync(
                        auditPreservedApproval,
                        It.IsAny<CancellationToken>()),
                Times.Once);
        }

        /// <summary>
        /// The row-local gate runs FIRST, so a caller holding no review role at all is refused
        /// without the approval's entity ever being read. Without this the two gates can be
        /// swapped and nothing fails — the caller sees the same refusal while a cross-entity read
        /// has already happened on their behalf. The suite's usual <c>VerifyNoOtherCalls</c> tail
        /// cannot catch it: that convention excludes <c>accessBrokerMock</c>.
        /// </summary>
        [Fact]
        public async Task ShouldRefuseModifyRowLocallyBeforeConsultingTheAccessBrokerAsync()
        {
            // given: neither the owner nor any review role
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();

            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Approval randomApproval = CreateRandomModifyApproval(randomDateTimeOffset, randomUserId);
            Approval inputApproval = randomApproval;
            Approval storageApproval = randomApproval.DeepClone();
            storageApproval.CreatedBy = GetRandomString();

            storageApproval.UpdatedWhen =
                storageApproval.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            SetupModifyApprovalRun(inputApproval, storageApproval, randomDateTimeOffset);

            // when
            ValueTask<Approval> modifyApprovalTask =
                this.approvalService.ModifyApprovalAsync(
                    inputApproval,
                    TestContext.Current.CancellationToken);

            await Assert.ThrowsAsync<ApprovalValidationException>(
                modifyApprovalTask.AsTask);

            // then
            this.accessBrokerMock.Verify(broker =>
                    broker.MayAmendApprovalAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<SecurityContext>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }


        /// <summary>
        /// <c>EntityType</c> and <c>EntityId</c> are identity, not content: they say which row an
        /// approval is about. Unpinned, a caller authorized for the approval as it stands could
        /// repoint it at a different entity in the same write — and the access gate above, which
        /// asks about the STORED row, would have answered for the old target.
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShouldThrowValidationExceptionOnModifyIfTheApprovalIsRepointedAsync(
            bool repointEntityType)
        {
            // given: the owner, so every authorization gate passes and the pin is the only
            // thing that can refuse
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();

            Approval randomApproval = CreateRandomModifyApproval(randomDateTimeOffset, randomUserId);
            Approval inputApproval = randomApproval;
            Approval storageApproval = randomApproval.DeepClone();

            storageApproval.UpdatedWhen =
                storageApproval.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            // the STORED row points somewhere else than the payload claims
            if (repointEntityType)
            {
                storageApproval.EntityType = inputApproval.EntityType == EntityType.Tag
                    ? EntityType.Link
                    : EntityType.Tag;
            }
            else
            {
                storageApproval.EntityId = Guid.NewGuid();
            }

            SetupModifyApprovalRun(inputApproval, storageApproval, randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<Approval>()))
                        .ReturnsAsync(inputApproval);

            // when
            ValueTask<Approval> modifyApprovalTask =
                this.approvalService.ModifyApprovalAsync(
                    inputApproval,
                    TestContext.Current.CancellationToken);

            ApprovalValidationException actualApprovalValidationException =
                await Assert.ThrowsAsync<ApprovalValidationException>(
                    modifyApprovalTask.AsTask);

            // then
            string pinnedMember = repointEntityType
                ? nameof(Approval.EntityType)
                : nameof(Approval.EntityId);

            actualApprovalValidationException.InnerException!.Data.Keys
                .Cast<string>().Should().Contain(pinnedMember);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateApprovalAsync(
                        It.IsAny<Approval>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }


        /// <summary>
        /// The bypass pair records that the §8.5 conditions were WAIVED and why, so outside an
        /// approval decision it is pinned to storage — unpinned, an authorized caller could mark
        /// an approval bypassed, or erase an existing waiver and its stated reason, with no
        /// waiver ever decided.
        ///
        /// <para>The one path where the pair may change is the becoming-Approved modify, where
        /// it is DERIVED from the §8.6.1 verdict — the Modify.Outcome tests pin that side; this
        /// one pins that every other path still refuses.</para>
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShouldThrowValidationExceptionOnModifyIfTheBypassPairIsTouchedAsync(
            bool touchTheFlag)
        {
            // given: the owner, so every authorization gate passes and the pin is the only
            // thing that can refuse
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();

            Approval randomApproval = CreateRandomModifyApproval(randomDateTimeOffset, randomUserId);
            Approval inputApproval = randomApproval;
            Approval storageApproval = randomApproval.DeepClone();

            storageApproval.UpdatedWhen =
                storageApproval.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            // the STORED waiver differs from what the payload claims
            if (touchTheFlag)
            {
                storageApproval.IsApprovedByBypass = !inputApproval.IsApprovedByBypass;
            }
            else
            {
                storageApproval.ApprovedByBypassReason =
                    inputApproval.ApprovedByBypassReason + GetRandomString();
            }

            SetupModifyApprovalRun(inputApproval, storageApproval, randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<Approval>()))
                        .ReturnsAsync(inputApproval);

            // when
            ValueTask<Approval> modifyApprovalTask =
                this.approvalService.ModifyApprovalAsync(
                    inputApproval,
                    TestContext.Current.CancellationToken);

            ApprovalValidationException actualApprovalValidationException =
                await Assert.ThrowsAsync<ApprovalValidationException>(
                    modifyApprovalTask.AsTask);

            // then
            string pinnedMember = touchTheFlag
                ? nameof(Approval.IsApprovedByBypass)
                : nameof(Approval.ApprovedByBypassReason);

            actualApprovalValidationException.InnerException!.Data.Keys
                .Cast<string>().Should().Contain(pinnedMember);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateApprovalAsync(
                        It.IsAny<Approval>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }


        /// <summary>
        /// "No reason recorded" is the same fact whether storage holds null or the payload sends
        /// an empty string, so a caller sending one for the other is not attempting a change and
        /// must not be refused. Every sibling pin on this field coalesces for exactly this reason.
        ///
        /// <para>Both directions, because a pin that coalesced only one side would still refuse
        /// half the round-trips. Without the coalescing this theory fails on both rows while the
        /// rest of the suite stays green — every other Approval modify test clones storage from
        /// the input, so the two sides are always identical and never exercise null against
        /// empty.</para>
        /// </summary>
        [Theory]
        [InlineData(null, "")]
        [InlineData("", null)]
        public async Task ShouldTreatANullAndAnEmptyBypassReasonAsTheSameAsync(
            string inputReason,
            string storageReason)
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();

            Approval randomApproval = CreateRandomModifyApproval(randomDateTimeOffset, randomUserId);
            Approval inputApproval = randomApproval;
            inputApproval.ApprovedByBypassReason = inputReason;

            Approval storageApproval = randomApproval.DeepClone();
            storageApproval.ApprovedByBypassReason = storageReason;

            storageApproval.UpdatedWhen =
                storageApproval.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            Approval updatedApproval = inputApproval.DeepClone();

            SetupModifyApprovalRun(inputApproval, storageApproval, randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<Approval>()))
                        .ReturnsAsync(inputApproval);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalAsync(It.IsAny<Approval>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(updatedApproval);

            // when
            Approval actualApproval = await this.approvalService.ModifyApprovalAsync(
                inputApproval,
                TestContext.Current.CancellationToken);

            // then: accepted, not refused
            actualApproval.Should().NotBeNull();

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateApprovalAsync(
                        It.IsAny<Approval>(),
                        It.IsAny<CancellationToken>()),
                Times.Once);
        }

        private void SetupModifyApprovalRun(
            Approval inputApproval,
            Approval storageApproval,
            DateTimeOffset randomDateTimeOffset)
        {
            Approval auditAppliedApproval = inputApproval.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    inputApproval,
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync(auditAppliedApproval);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedApproval.UpdatedBy);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalByIdAsync(
                    inputApproval.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApproval);
        }

        private static string FlattenApprovalExceptionText(Exception exception)
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
