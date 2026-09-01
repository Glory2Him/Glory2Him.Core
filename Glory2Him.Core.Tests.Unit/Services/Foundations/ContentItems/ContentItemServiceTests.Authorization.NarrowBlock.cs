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
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItems
{
    /// <summary>
    /// The narrow tier of the BLOCK — <c>ContentItem-{ContentType}-ReadOnly</c>, design §18.6
    /// rule 2. The matrix is uniform across all three tiers: two of them could already block a
    /// user and the third could only grant, and this is the tier that closes that.
    ///
    /// <para><b>Two properties, and each case pairs them.</b> A block whose scope covers the row
    /// wins over every grant, <c>Administrators</c> included; a block whose scope does not cover
    /// the row is <i>silent</i> — not weakened, not outvoted, simply not asked. Asserting only
    /// the first would let somebody widen the block to every content type and keep the suite
    /// green.
    /// </para>
    ///
    /// <para>Every row here names its <c>ContentType</c> explicitly. The filler ignores that
    /// property, so a random content item always carries the enum's default member — and a test
    /// that let it default would pass whether or not the composed role name was scoped at
    /// all.</para>
    /// </summary>
    public partial class ContentItemServiceTests
    {
        private const ContentType BlockedContentType = ContentType.Quote;
        private const ContentType UnblockedContentType = ContentType.Story;

        private static string QuoteBlock =>
            Roles.ReadOnlyFor(EntityType.ContentItem, BlockedContentType);

        /// <summary>
        /// The precedence table of §18.6 rule 2, one case per row: the grant paired with the
        /// block is what the block has to outrank. <c>Administrators</c> is included because it
        /// is the row a future refactor is most likely to "optimise" into an early allow.
        /// </summary>
        public static TheoryData<string> GrantsTheBlockOutranks() =>
            new TheoryData<string>
            {
                Roles.ReviewersFor(EntityType.ContentItem, ContentType.Quote),
                Roles.PublishersFor(EntityType.ContentItem, ContentType.Quote),
                Roles.ContentItemReviewers,
                Roles.ContentItemPublishers,
                Roles.Reviewers,
                Roles.Publishers,
                Roles.Administrators,
            };

        /// <summary>
        /// The other half of the table: the same grants against a row the block does not cover.
        /// A <c>Quote</c> block says nothing about a <c>Story</c>, so every one of these reaches
        /// the next gate.
        /// </summary>
        public static TheoryData<string> GrantsOnARowTheBlockDoesNotCover() =>
            GrantsTheBlockOutranks();

        private static ContentItemValidationException ExpectedBlockedException() =>
            new ContentItemValidationException(
                message: "Content item validation error occurred, fix the errors and try again.",
                innerException: new UnauthorizedContentItemException(
                    message: "The current user is blocked from contributing content items."));

        // ── ADD: the content type comes off the incoming item ────────────────────────

        [Theory]
        [MemberData(nameof(GrantsTheBlockOutranks))]
        public async Task ShouldRefuseAddWhenTheNarrowBlockCoversTheIncomingContentTypeAsync(
            string grantTheBlockOutranks)
        {
            // given
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(QuoteBlock, grantTheBlockOutranks);

            ContentItem someContentItem = CreateRandomContentItem();
            someContentItem.ContentType = BlockedContentType;

            ContentItemValidationException expectedException = ExpectedBlockedException();

            // when
            ValueTask<ContentItem> addContentItemTask =
                this.contentItemService.AddContentItemAsync(
                    someContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualException =
                await Assert.ThrowsAsync<ContentItemValidationException>(
                    addContentItemTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(GrantsOnARowTheBlockDoesNotCover))]
        public async Task ShouldNotRefuseAddWhenTheNarrowBlockNamesADifferentContentTypeAsync(
            string grantTheBlockDoesNotOutrank)
        {
            // given: the same block, a row it does not cover. The write is expected to get PAST
            // the gate — asserted by the audit stamp the gate would otherwise have prevented,
            // not by a successful add, because everything downstream is a separate rule.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(QuoteBlock, grantTheBlockDoesNotOutrank);

            ContentItem someContentItem = CreateRandomContentItem();
            someContentItem.ContentType = UnblockedContentType;

            await AssertAddReachesTheAuditStampAsync(someContentItem);
        }

        [Fact]
        public async Task ShouldNotRefuseAddWhenTheNarrowBlockNamesADifferentEntityTypeAsync()
        {
            // given: Tag-Quote-ReadOnly is not a name anything seeds, and that is the point —
            // the composed name carries the ENTITY type as well as the content type, so a block
            // spelled for another entity can never be matched against a content item.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(
                Roles.ReadOnlyFor(EntityType.Tag, BlockedContentType));

            ContentItem someContentItem = CreateRandomContentItem();
            someContentItem.ContentType = BlockedContentType;

            await AssertAddReachesTheAuditStampAsync(someContentItem);
        }

        // The gate is the FIRST thing an add does, so reaching the audit stamp is exactly the
        // proof that it was passed. Stopping there keeps these cases about the veto rather than
        // about the dozen validation rules that follow it.
        private async Task AssertAddReachesTheAuditStampAsync(ContentItem inputContentItem)
        {
            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(inputContentItem, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(inputContentItem);

            // when
            ValueTask<ContentItem> addContentItemTask =
                this.contentItemService.AddContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            await Record.ExceptionAsync(addContentItemTask.AsTask);

            // then
            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(inputContentItem, It.IsAny<SecurityContext>()),
                Times.Once);
        }

        // ── MODIFY: the content type comes off the STORED row ────────────────────────

        [Theory]
        [MemberData(nameof(GrantsTheBlockOutranks))]
        public async Task ShouldRefuseModifyWhenTheNarrowBlockCoversTheStoredContentTypeAsync(
            string grantTheBlockOutranks)
        {
            // given
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(QuoteBlock, grantTheBlockOutranks);

            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string actorUserId = GetRandomString();

            ContentItem inputContentItem =
                CreateRandomModifyContentItem(randomDateTimeOffset, actorUserId);

            inputContentItem.ContentType = BlockedContentType;

            ContentItem storageContentItem = inputContentItem.DeepClone();
            storageContentItem.UpdatedWhen =
                storageContentItem.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            await AssertModifyIsBlockedAsync(
                inputContentItem: inputContentItem,
                storageContentItem: storageContentItem,
                actorUserId: actorUserId,
                currentDateTimeOffset: randomDateTimeOffset);
        }

        [Fact]
        public async Task ShouldRefuseModifyOfTheirOwnRowWhenTheNarrowBlockCoversItAsync()
        {
            // given: the edge the issue rules on. There is no owner carve-out — a contributor
            // sanctioned on a content type may no longer edit even the quotes they wrote
            // themselves, because the owner admit is a grant like any other and the veto
            // outranks it. The consequence accepted deliberately: taking their own content down
            // needs an unblocked owner-or-Administrators path.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(QuoteBlock);

            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string ownerUserId = GetRandomString();

            ContentItem inputContentItem =
                CreateRandomModifyContentItem(randomDateTimeOffset, ownerUserId);

            inputContentItem.ContentType = BlockedContentType;

            ContentItem storageContentItem = inputContentItem.DeepClone();
            storageContentItem.CreatedBy = ownerUserId;
            storageContentItem.UpdatedWhen =
                storageContentItem.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            await AssertModifyIsBlockedAsync(
                inputContentItem: inputContentItem,
                storageContentItem: storageContentItem,
                actorUserId: ownerUserId,
                currentDateTimeOffset: randomDateTimeOffset);
        }

        [Fact]
        public async Task ShouldRefuseModifyAgainstTheStoredTypeWhenTheCallerRelabelsTheEditAsync()
        {
            // given: the reason the modify path reads the STORED row rather than the caller's
            // copy. ContentType is create-only (§12.4.1 rule 7a), so a blocked contributor
            // presenting their quote as a story would otherwise walk straight past the veto —
            // and the pin that would eventually catch the relabel runs several rules later.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(QuoteBlock);

            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string ownerUserId = GetRandomString();

            ContentItem inputContentItem =
                CreateRandomModifyContentItem(randomDateTimeOffset, ownerUserId);

            inputContentItem.ContentType = UnblockedContentType;

            ContentItem storageContentItem = inputContentItem.DeepClone();
            storageContentItem.ContentType = BlockedContentType;
            storageContentItem.CreatedBy = ownerUserId;
            storageContentItem.UpdatedWhen =
                storageContentItem.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            await AssertModifyIsBlockedAsync(
                inputContentItem: inputContentItem,
                storageContentItem: storageContentItem,
                actorUserId: ownerUserId,
                currentDateTimeOffset: randomDateTimeOffset);
        }

        [Theory]
        [MemberData(nameof(GrantsOnARowTheBlockDoesNotCover))]
        public async Task ShouldNotRefuseModifyWhenTheNarrowBlockNamesADifferentContentTypeAsync(
            string grantTheBlockDoesNotOutrank)
        {
            // given
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(QuoteBlock, grantTheBlockDoesNotOutrank);

            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string actorUserId = GetRandomString();

            ContentItem inputContentItem =
                CreateRandomModifyContentItem(randomDateTimeOffset, actorUserId);

            inputContentItem.ContentType = UnblockedContentType;

            ContentItem storageContentItem = inputContentItem.DeepClone();
            storageContentItem.UpdatedWhen =
                storageContentItem.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            SetupFailingModifyPathBrokers(
                inputContentItem: inputContentItem,
                storageContentItem: storageContentItem,
                actorUserId: actorUserId,
                currentDateTimeOffset: randomDateTimeOffset);

            // when
            ValueTask<ContentItem> modifyContentItemTask =
                this.contentItemService.ModifyContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            await Record.ExceptionAsync(modifyContentItemTask.AsTask);

            // then: the ownership question is only reached once the veto has been passed, and
            // the second GetUserIdAsync is the write gate asking it.
            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Exactly(2));
        }

        private async Task AssertModifyIsBlockedAsync(
            ContentItem inputContentItem,
            ContentItem storageContentItem,
            string actorUserId,
            DateTimeOffset currentDateTimeOffset)
        {
            ContentItemValidationException expectedException = ExpectedBlockedException();

            SetupFailingModifyPathBrokers(
                inputContentItem: inputContentItem,
                storageContentItem: storageContentItem,
                actorUserId: actorUserId,
                currentDateTimeOffset: currentDateTimeOffset);

            // when
            ValueTask<ContentItem> modifyContentItemTask =
                this.contentItemService.ModifyContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualException =
                await Assert.ThrowsAsync<ContentItemValidationException>(
                    modifyContentItemTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedException);

            // The veto runs AHEAD of the ownership question, so the write gate's own
            // GetUserIdAsync is never reached — one call, from the shape validation.
            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

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
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);
        }

        // ── WITHDRAWAL AND REMOVAL: the stored row again ─────────────────────────────

        [Fact]
        public async Task ShouldRefuseRemovingTheirOwnRowWhenTheNarrowBlockCoversItAsync()
        {
            // given: withdrawal is a write, and the block covers the holder's own rows.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(QuoteBlock);

            string ownerUserId = GetRandomString();
            ContentItem storageContentItem = CreateRandomContentItem();
            storageContentItem.ContentType = BlockedContentType;
            storageContentItem.CreatedBy = ownerUserId;
            Guid contentItemId = storageContentItem.Id;

            ContentItemValidationException expectedException = ExpectedBlockedException();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemByIdAsync(
                    contentItemId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(ownerUserId);

            // when
            ValueTask<ContentItem> removeContentItemByIdTask =
                this.contentItemService.RemoveContentItemByIdAsync(
                    contentItemId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ContentItemValidationException actualException =
                await Assert.ThrowsAsync<ContentItemValidationException>(
                    removeContentItemByIdTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedException);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateContentItemAsync(
                    It.IsAny<ContentItem>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);
        }

        [Fact]
        public async Task ShouldRefuseHardRemoveWhenTheNarrowBlockCoversTheStoredRowAsync()
        {
            // given: an administrator holding the narrow block. This is the one write a
            // content-type-blocked Administrators could otherwise still perform, and it is the
            // destructive one — a block that stops the reversible takedown but not the
            // irreversible one is the wrong way round.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Administrators, QuoteBlock);

            ContentItem storageContentItem = CreateRandomContentItem();
            storageContentItem.ContentType = BlockedContentType;
            Guid contentItemId = storageContentItem.Id;

            ContentItemValidationException expectedException = ExpectedBlockedException();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemByIdAsync(
                    contentItemId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageContentItem);

            // when
            ValueTask<ContentItem> hardRemoveContentItemByIdTask =
                this.contentItemService.HardRemoveContentItemByIdAsync(
                    contentItemId,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualException =
                await Assert.ThrowsAsync<ContentItemValidationException>(
                    hardRemoveContentItemByIdTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedException);

            this.storageBrokerMock.Verify(broker =>
                broker.DeleteContentItemAsync(
                    It.IsAny<ContentItem>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);
        }

        [Fact]
        public async Task ShouldNotRefuseHardRemoveWhenTheNarrowBlockNamesADifferentContentTypeAsync()
        {
            // given
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Administrators, QuoteBlock);

            ContentItem storageContentItem = CreateRandomContentItem();
            storageContentItem.ContentType = UnblockedContentType;
            Guid contentItemId = storageContentItem.Id;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemByIdAsync(
                    contentItemId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageContentItem);

            this.storageBrokerMock.Setup(broker =>
                broker.DeleteContentItemAsync(
                    storageContentItem,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageContentItem);

            // when
            await this.contentItemService.HardRemoveContentItemByIdAsync(
                contentItemId,
                TestContext.Current.CancellationToken);

            // then
            this.storageBrokerMock.Verify(broker =>
                broker.DeleteContentItemAsync(
                    storageContentItem,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        // ── SUBMIT: a status-only write is still a write ─────────────────────────────

        [Fact]
        public async Task ShouldRefuseSubmittingTheirOwnRowWhenTheNarrowBlockCoversItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(QuoteBlock);

            string ownerUserId = GetRandomString();
            ContentItem storageContentItem = CreateRandomContentItem();
            storageContentItem.ContentType = BlockedContentType;
            storageContentItem.CreatedBy = ownerUserId;
            storageContentItem.ApprovalStatus = ApprovalStatus.Draft;
            Guid contentItemId = storageContentItem.Id;

            ContentItemValidationException expectedException = ExpectedBlockedException();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemByIdAsync(
                    contentItemId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(ownerUserId);

            // when
            ValueTask<ContentItem> submitContentItemTask =
                this.contentItemService.SubmitContentItemByIdAsync(
                    contentItemId,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualException =
                await Assert.ThrowsAsync<ContentItemValidationException>(
                    submitContentItemTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedException);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateContentItemAsync(
                    It.IsAny<ContentItem>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);
        }
    }
}
