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
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItems
{
    public partial class ContentItemServiceTests
    {
        // ── The approval state is not a caller's to assert ───────────────────────────
        //
        // ContentItem is the entity the whole approval workflow exists for, and its general
        // modify pinned only audit fields. Every test below fails if its rule is removed.

        // Sets up the brokers a modify reaches before the pin rules run, for a caller whose
        // write attempt is expected to be refused by ValidateAgainstStorageContentItemOnModify.
        private void SetupFailingModifyPathBrokers(
            ContentItem inputContentItem,
            ContentItem storageContentItem,
            string actorUserId,
            DateTimeOffset currentDateTimeOffset)
        {
            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(inputContentItem, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(inputContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(actorUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemByIdAsync(
                    inputContentItem.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    inputContentItem,
                    storageContentItem))
                        .ReturnsAsync(inputContentItem);
        }

        private async Task<ContentItemValidationException> AssertModifyIsRefusedAsync(
            ContentItem invalidContentItem,
            InvalidContentItemException invalidContentItemException)
        {
            var expectedContentItemValidationException =
                new ContentItemValidationException(
                    message: "Content item validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemException);

            ValueTask<ContentItem> modifyContentItemTask =
                this.contentItemService.ModifyContentItemAsync(
                    invalidContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actual =
                await Assert.ThrowsAsync<ContentItemValidationException>(
                    modifyContentItemTask.AsTask);

            actual.Should().BeEquivalentTo(expectedContentItemValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishContentItemAsync(
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    It.IsAny<ContentItemEventOperation>()),
                Times.Never);

            return actual;
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfApprovalStateWasChangedAndLogItAsync()
        {
            // given: the hole this suite exists to close. A Reviewer holds write permission on
            // the row for content edits, and without these pins the same modify call would let
            // them mark a stranger's draft approved and published — no review role check, no
            // publisher tier, no access decision, no approval conditions.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.ContentItemReviewer);

            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string actorUserId = GetRandomString();
            string ownerUserId = GetRandomString();

            ContentItem invalidContentItem =
                CreateRandomModifyContentItem(randomDateTimeOffset, actorUserId);

            invalidContentItem.CreatedBy = ownerUserId;

            ContentItem storageContentItem = invalidContentItem.DeepClone();
            storageContentItem.UpdatedWhen = storageContentItem.UpdatedWhen.AddDays(GetRandomNegativeNumber());
            storageContentItem.ApprovalStatus = ApprovalStatus.Draft;
            storageContentItem.IsPublished = false;
            storageContentItem.PublishDate = null;

            invalidContentItem.ApprovalStatus = ApprovalStatus.Approved;
            invalidContentItem.IsPublished = true;
            invalidContentItem.PublishDate = randomDateTimeOffset;

            var invalidContentItemException = new InvalidContentItemException(
                message: "Content item is invalid, fix the errors and try again.");

            // not the generic pin message: the status is guarded by the carve-out rule, which
            // permits Draft <-> Submitted for an eligible caller and refuses everything else,
            // so it reports against the STORED status rather than against a field name
            invalidContentItemException.AddData(
                key: nameof(ContentItem.ApprovalStatus),
                values: "Value is not the same as storage approval status");

            invalidContentItemException.AddData(
                key: nameof(ContentItem.IsPublished),
                values: $"Value is not the same as {nameof(ContentItem.IsPublished)}");

            invalidContentItemException.AddData(
                key: nameof(ContentItem.PublishDate),
                values: $"Date is not the same as {nameof(ContentItem.PublishDate)}");

            SetupFailingModifyPathBrokers(
                invalidContentItem, storageContentItem, actorUserId, randomDateTimeOffset);

            // when . then
            await AssertModifyIsRefusedAsync(invalidContentItem, invalidContentItemException);
        }

        // The bypass record is pinned hardest of all, because it is the only field here whose
        // whole purpose is to be read back later as evidence. Whoever bypass-approved a row
        // could otherwise reopen it through modify and quietly clear the flag that says so.

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfTheBypassFlagWasChangedAndLogItAsync()
        {
            // given
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.ContentItemReviewer);

            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string actorUserId = GetRandomString();
            string ownerUserId = GetRandomString();

            ContentItem invalidContentItem =
                CreateRandomModifyContentItem(randomDateTimeOffset, actorUserId);

            invalidContentItem.CreatedBy = ownerUserId;

            ContentItem storageContentItem = invalidContentItem.DeepClone();
            storageContentItem.UpdatedWhen = storageContentItem.UpdatedWhen.AddDays(GetRandomNegativeNumber());
            storageContentItem.IsApprovedByBypass = true;

            invalidContentItem.IsApprovedByBypass = false;

            var invalidContentItemException = new InvalidContentItemException(
                message: "Content item is invalid, fix the errors and try again.");

            invalidContentItemException.AddData(
                key: nameof(ContentItem.IsApprovedByBypass),
                values: $"Value is not the same as {nameof(ContentItem.IsApprovedByBypass)}");

            SetupFailingModifyPathBrokers(
                invalidContentItem, storageContentItem, actorUserId, randomDateTimeOffset);

            // when . then
            await AssertModifyIsRefusedAsync(invalidContentItem, invalidContentItemException);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfTheBypassReasonWasChangedAndLogItAsync()
        {
            // given
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.ContentItemReviewer);

            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string actorUserId = GetRandomString();
            string ownerUserId = GetRandomString();

            ContentItem invalidContentItem =
                CreateRandomModifyContentItem(randomDateTimeOffset, actorUserId);

            invalidContentItem.CreatedBy = ownerUserId;

            ContentItem storageContentItem = invalidContentItem.DeepClone();
            storageContentItem.UpdatedWhen = storageContentItem.UpdatedWhen.AddDays(GetRandomNegativeNumber());
            storageContentItem.ApprovedByBypassReason = GetRandomString();

            invalidContentItem.ApprovedByBypassReason = GetRandomString();

            var invalidContentItemException = new InvalidContentItemException(
                message: "Content item is invalid, fix the errors and try again.");

            invalidContentItemException.AddData(
                key: nameof(ContentItem.ApprovedByBypassReason),
                values: $"Text is not the same as {nameof(ContentItem.ApprovedByBypassReason)}");

            SetupFailingModifyPathBrokers(
                invalidContentItem, storageContentItem, actorUserId, randomDateTimeOffset);

            // when . then
            await AssertModifyIsRefusedAsync(invalidContentItem, invalidContentItemException);
        }

        // "No reason recorded" is the same fact whether it is stored as null or as empty, so a
        // caller sending one for the other is not attempting a change worth refusing.

        [Fact]
        public async Task ShouldAcceptANullForAnEmptyBypassReasonOnModifyAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string ownerUserId = GetRandomString();

            ContentItem inputContentItem =
                CreateRandomModifyContentItem(randomDateTimeOffset, ownerUserId);

            inputContentItem.ApprovedByBypassReason = null;

            ContentItem storageContentItem = inputContentItem.DeepClone();
            storageContentItem.UpdatedWhen = storageContentItem.UpdatedWhen.AddDays(GetRandomNegativeNumber());
            storageContentItem.ApprovedByBypassReason = string.Empty;

            ContentItem updatedContentItem = inputContentItem.DeepClone();
            ContentItem expectedContentItem = updatedContentItem.DeepClone();

            SetupPassingModifyPathBrokers(
                inputContentItem, storageContentItem, updatedContentItem, ownerUserId, randomDateTimeOffset);

            // when
            ContentItem actualContentItem =
                await this.contentItemService.ModifyContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItem.Should().BeEquivalentTo(expectedContentItem);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateContentItemAsync(inputContentItem, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        // ── The version lineage is the workflow's, not the caller's ──────────────────
        //
        // Version and GroupId are how an approved item's history is read back — and, now that
        // the tip is DERIVED from the highest Version in the group, they are also the whole of
        // how the tip is decided. Left writable, a caller could detach an item from its group
        // or raise its Version above the real tip and crown an old row, and the approved
        // version anyone reviewed would be gone.

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfTheVersionLineageWasChangedAndLogItAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string ownerUserId = GetRandomString();

            ContentItem invalidContentItem =
                CreateRandomModifyContentItem(randomDateTimeOffset, ownerUserId);

            invalidContentItem.Version = 2;

            ContentItem storageContentItem = invalidContentItem.DeepClone();
            storageContentItem.UpdatedWhen = storageContentItem.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            invalidContentItem.GroupId = Guid.NewGuid();

            // raising the version IS the attempt to crown this row as the tip, now that the tip
            // is the highest Version in the group rather than a flag the caller could flip
            invalidContentItem.Version = 7;

            var invalidContentItemException = new InvalidContentItemException(
                message: "Content item is invalid, fix the errors and try again.");

            invalidContentItemException.AddData(
                key: nameof(ContentItem.GroupId),
                values: $"Id is not the same as {nameof(ContentItem.GroupId)}");

            invalidContentItemException.AddData(
                key: nameof(ContentItem.Version),
                values: $"Value is not the same as {nameof(ContentItem.Version)}");

            SetupFailingModifyPathBrokers(
                invalidContentItem, storageContentItem, ownerUserId, randomDateTimeOffset);

            // when . then
            await AssertModifyIsRefusedAsync(invalidContentItem, invalidContentItemException);
        }

        // Design §12.4.1 rule 7a: the content type is create-only. Different content types
        // carry different validation rules, so an item must not be relabelled into a type its
        // content was never checked against — nor into one whose reviewers never saw it.

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfTheContentTypeWasChangedAndLogItAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string ownerUserId = GetRandomString();

            ContentItem invalidContentItem =
                CreateRandomModifyContentItem(randomDateTimeOffset, ownerUserId);

            invalidContentItem.ContentType = ContentType.Quote;

            ContentItem storageContentItem = invalidContentItem.DeepClone();
            storageContentItem.UpdatedWhen = storageContentItem.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            invalidContentItem.ContentType = ContentType.Testimony;

            var invalidContentItemException = new InvalidContentItemException(
                message: "Content item is invalid, fix the errors and try again.");

            invalidContentItemException.AddData(
                key: nameof(ContentItem.ContentType),
                values: $"Value is not the same as {nameof(ContentItem.ContentType)}");

            SetupFailingModifyPathBrokers(
                invalidContentItem, storageContentItem, ownerUserId, randomDateTimeOffset);

            // when . then
            await AssertModifyIsRefusedAsync(invalidContentItem, invalidContentItemException);
        }

        // ── The one carve-out: Draft <-> Submitted (design §9.2 rules 4-6) ───────────
        //
        // Submitting is inseparable from the edit that made the work ready, so the owner may
        // move the status between those two states through modify. Everything else about the
        // status stays pinned.

        public static TheoryData<ApprovalStatus, ApprovalStatus> SubmissionTransitions() =>
            new TheoryData<ApprovalStatus, ApprovalStatus>
            {
                { ApprovalStatus.Draft, ApprovalStatus.Submitted },
                { ApprovalStatus.Submitted, ApprovalStatus.Draft }
            };

        [Theory]
        [MemberData(nameof(SubmissionTransitions))]
        public async Task ShouldWriteTheSubmissionStatusOnModifyWhenTheOwnerMovesItAsync(
            ApprovalStatus storageStatus,
            ApprovalStatus inputStatus)
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string ownerUserId = GetRandomString();

            ContentItem inputContentItem =
                CreateRandomModifyContentItem(randomDateTimeOffset, ownerUserId);

            inputContentItem.ApprovalStatus = storageStatus;

            ContentItem storageContentItem = inputContentItem.DeepClone();
            storageContentItem.UpdatedWhen = storageContentItem.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            inputContentItem.ApprovalStatus = inputStatus;

            ContentItem updatedContentItem = inputContentItem.DeepClone();
            ContentItem expectedContentItem = updatedContentItem.DeepClone();

            SetupPassingModifyPathBrokers(
                inputContentItem, storageContentItem, updatedContentItem, ownerUserId, randomDateTimeOffset);

            // when
            ContentItem actualContentItem =
                await this.contentItemService.ModifyContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItem.Should().BeEquivalentTo(expectedContentItem);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateContentItemAsync(inputContentItem, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        // A Reviewer passes the write gate and may amend content, and must still never move an
        // approval status (design §8.6 HR-3). The carve-out is gated on ownership or the
        // Publisher tier, not on write permission.

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfANonOwnerMovesTheSubmissionStatusAsync()
        {
            // given
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.ContentItemReviewer);

            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string actorUserId = GetRandomString();
            string ownerUserId = GetRandomString();

            ContentItem invalidContentItem =
                CreateRandomModifyContentItem(randomDateTimeOffset, actorUserId);

            invalidContentItem.CreatedBy = ownerUserId;
            invalidContentItem.ApprovalStatus = ApprovalStatus.Draft;

            ContentItem storageContentItem = invalidContentItem.DeepClone();
            storageContentItem.UpdatedWhen = storageContentItem.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            invalidContentItem.ApprovalStatus = ApprovalStatus.Submitted;

            var invalidContentItemException = new InvalidContentItemException(
                message: "Content item is invalid, fix the errors and try again.");

            invalidContentItemException.AddData(
                key: nameof(ContentItem.ApprovalStatus),
                values: "Value is not the same as storage approval status");

            SetupFailingModifyPathBrokers(
                invalidContentItem, storageContentItem, actorUserId, randomDateTimeOffset);

            // when . then
            await AssertModifyIsRefusedAsync(invalidContentItem, invalidContentItemException);
        }

        // The Publisher tier may move the submission status on someone else's item — it is the
        // tier the dedicated approve operation itself requires.

        [Fact]
        public async Task ShouldWriteTheSubmissionStatusOnModifyWhenAPublisherMovesItAsync()
        {
            // given
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.ContentItemPublisher);

            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string actorUserId = GetRandomString();
            string ownerUserId = GetRandomString();

            ContentItem inputContentItem =
                CreateRandomModifyContentItem(randomDateTimeOffset, actorUserId);

            inputContentItem.CreatedBy = ownerUserId;
            inputContentItem.ApprovalStatus = ApprovalStatus.Draft;

            ContentItem storageContentItem = inputContentItem.DeepClone();
            storageContentItem.UpdatedWhen = storageContentItem.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            inputContentItem.ApprovalStatus = ApprovalStatus.Submitted;

            ContentItem updatedContentItem = inputContentItem.DeepClone();
            ContentItem expectedContentItem = updatedContentItem.DeepClone();

            SetupPassingModifyPathBrokers(
                inputContentItem, storageContentItem, updatedContentItem, actorUserId, randomDateTimeOffset);

            // when
            ContentItem actualContentItem =
                await this.contentItemService.ModifyContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItem.Should().BeEquivalentTo(expectedContentItem);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateContentItemAsync(inputContentItem, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        // Sets up every broker a modify touches on the happy path, for the cases above that
        // are expected to be written rather than refused.
        private void SetupPassingModifyPathBrokers(
            ContentItem inputContentItem,
            ContentItem storageContentItem,
            ContentItem updatedContentItem,
            string actorUserId,
            DateTimeOffset currentDateTimeOffset)
        {
            SetupFailingModifyPathBrokers(
                inputContentItem, storageContentItem, actorUserId, currentDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateContentItemAsync(inputContentItem, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(updatedContentItem);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishContentItemAsync(
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    ContentItemEventOperation.Modified))
                        .Returns(new ValueTask<EventPublishResult<ContentItem>>(
                            new EventPublishResult<ContentItem>()));
        }

        // ── Nor is it a caller's to assert on the way in ─────────────────────────────
        //
        // The pins above are worth nothing on their own if a row can simply arrive already
        // approved. Design §9.7.1's write surface bounds add to an ApprovalStatus of Draft
        // or Submitted and nothing else — never IsPublished, never PublishDate — because
        // publication is the approve operation's to grant (rules 1 and 3).

        // Sets up the brokers an add reaches before the rules run, for a caller whose write
        // is expected to be refused by ValidateOnAddContentItem.
        private void SetupFailingAddPathBrokers(
            ContentItem inputContentItem,
            string actorUserId,
            DateTimeOffset currentDateTimeOffset)
        {
            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(inputContentItem, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(inputContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(actorUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTimeOffset);
        }

        private async Task<ContentItemValidationException> AssertAddIsRefusedAsync(
            ContentItem invalidContentItem,
            InvalidContentItemException invalidContentItemException)
        {
            var expectedContentItemValidationException =
                new ContentItemValidationException(
                    message: "Content item validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemException);

            ValueTask<ContentItem> addContentItemTask =
                this.contentItemService.AddContentItemAsync(
                    invalidContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actual =
                await Assert.ThrowsAsync<ContentItemValidationException>(
                    addContentItemTask.AsTask);

            actual.Should().BeEquivalentTo(expectedContentItemValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishContentItemAsync(
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    It.IsAny<ContentItemEventOperation>()),
                Times.Never);

            return actual;
        }

        public static TheoryData<ApprovalStatus> VerdictApprovalStatuses() =>
            new TheoryData<ApprovalStatus>
            {
                ApprovalStatus.Approved,
                ApprovalStatus.Rejected,
                ApprovalStatus.Dismissed
            };

        [Theory]
        [MemberData(nameof(VerdictApprovalStatuses))]
        public async Task ShouldThrowValidationExceptionOnAddIfApprovalStatusIsAVerdictAndLogItAsync(
            ApprovalStatus verdictStatus)
        {
            // given: a verdict is the approval workflow's to record. Without this rule any
            // authenticated caller — no roles at all — could insert a content item that is
            // already Approved, skipping the workflow rather than bypassing it.
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ContentItem invalidContentItem =
                CreateContentItemFiller(randomDateTimeOffset, randomUserId).Create();

            invalidContentItem.ApprovalStatus = verdictStatus;

            var invalidContentItemException = new InvalidContentItemException(
                message: "Content item is invalid, fix the errors and try again.");

            invalidContentItemException.AddData(
                key: nameof(ContentItem.ApprovalStatus),
                values: $"Value must be {nameof(ApprovalStatus.Draft)} " +
                    $"or {nameof(ApprovalStatus.Submitted)} on add");

            SetupFailingAddPathBrokers(invalidContentItem, randomUserId, randomDateTimeOffset);

            // when . then
            await AssertAddIsRefusedAsync(invalidContentItem, invalidContentItemException);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfPublicationWasAssertedAndLogItAsync()
        {
            // given: a role-less caller publishing their own content item on the way in —
            // public the moment it lands, with no review role, no publisher tier, no access
            // decision and no approval conditions between them and the front page.
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ContentItem invalidContentItem =
                CreateContentItemFiller(randomDateTimeOffset, randomUserId).Create();

            invalidContentItem.IsPublished = true;
            invalidContentItem.PublishDate = randomDateTimeOffset;

            var invalidContentItemException = new InvalidContentItemException(
                message: "Content item is invalid, fix the errors and try again.");

            invalidContentItemException.AddData(
                key: nameof(ContentItem.IsPublished),
                values: "Value is not allowed on add");

            invalidContentItemException.AddData(
                key: nameof(ContentItem.PublishDate),
                values: "Date is not allowed on add");

            SetupFailingAddPathBrokers(invalidContentItem, randomUserId, randomDateTimeOffset);

            // when . then
            await AssertAddIsRefusedAsync(invalidContentItem, invalidContentItemException);
        }

        // A future publish date is the subtler half of the same rule: the row is not public
        // yet, so nothing looks wrong until the clock passes the date the caller chose.

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfPublicationWasScheduledAndLogItAsync()
        {
            // given
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ContentItem invalidContentItem =
                CreateContentItemFiller(randomDateTimeOffset, randomUserId).Create();

            invalidContentItem.PublishDate = randomDateTimeOffset.AddDays(GetRandomNumber());

            var invalidContentItemException = new InvalidContentItemException(
                message: "Content item is invalid, fix the errors and try again.");

            invalidContentItemException.AddData(
                key: nameof(ContentItem.PublishDate),
                values: "Date is not allowed on add");

            SetupFailingAddPathBrokers(invalidContentItem, randomUserId, randomDateTimeOffset);

            // when . then
            await AssertAddIsRefusedAsync(invalidContentItem, invalidContentItemException);
        }

        [Theory]
        [InlineData(ApprovalStatus.Draft)]
        [InlineData(ApprovalStatus.Submitted)]
        public async Task ShouldAcceptAContributableApprovalStatusOnAddAsync(
            ApprovalStatus contributableStatus)
        {
            // given: the positive half of the rule. Design §9.7.1 rule 1 says a row is written
            // with "the ApprovalStatus the caller asked for — Submitted on the common path,
            // Draft when saving work in progress", so narrowing the rule to Draft-only would
            // break the documented common path. Without this test that narrowing is invisible.
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ContentItem inputContentItem =
                CreateContentItemFiller(randomDateTimeOffset, randomUserId).Create();

            inputContentItem.ApprovalStatus = contributableStatus;
            ContentItem storageContentItem = inputContentItem.DeepClone();
            SetupFailingAddPathBrokers(inputContentItem, randomUserId, randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.InsertContentItemAsync(inputContentItem, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItem);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishContentItemAsync(
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    ContentItemEventOperation.Added))
                        .Returns(new ValueTask<EventPublishResult<ContentItem>>(
                            new EventPublishResult<ContentItem>()));

            // when
            ContentItem actualContentItem =
                await this.contentItemService.AddContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItem.ApprovalStatus.Should().Be(contributableStatus);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertContentItemAsync(inputContentItem, It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
