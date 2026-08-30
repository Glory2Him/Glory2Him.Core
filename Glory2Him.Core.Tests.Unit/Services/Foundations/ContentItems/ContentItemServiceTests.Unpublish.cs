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
using Microsoft.Data.SqlClient;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItems
{
    /// <summary>
    /// The publication swap's first write — clearing the group's published slot so a newly
    /// approved sibling can take it (design §9.7.7 rules 6–7, §12.4.1 rule 10).
    ///
    /// <para>The verb owns <c>IsPublished</c> and <c>PublishDate</c> and nothing else. The
    /// incumbent is SUPERSEDED, not un-approved, so <c>ApprovalStatus</c> stays
    /// <c>Approved</c> and the row's <c>Version</c> is untouched (§3.4 rule 18) — which is why
    /// every test below plants those two at values the operation could not reach by accident
    /// and asserts they survived.</para>
    ///
    /// <para>Every field assertion is made against a snapshot taken INSIDE the storage
    /// callback, never against the row handed in. The service mutates the very instance the
    /// read returned and passes it onward, so comparing a saved field with the input object
    /// compares it with itself and passes however the operation behaved.</para>
    /// </summary>
    public partial class ContentItemServiceTests
    {
        // Pinned, and pinned DIFFERENTLY from each other. The id, the group and the row the
        // swap is promoting are three distinct values, so a service that confused one for
        // another cannot coincidentally satisfy an assertion.
        private static readonly Guid UnpublishIncumbentContentItemId =
            new Guid("11111111-1111-1111-1111-111111111111");

        private static readonly Guid UnpublishGroupId =
            new Guid("22222222-2222-2222-2222-222222222222");

        private static readonly Guid UnpublishPromotedContentItemId =
            new Guid("33333333-3333-3333-3333-333333333333");

        // The incumbent's place in its version chain. Pinned above 1 so a service that reset
        // or recomputed it could not land back on the same number by accident.
        private const int UnpublishVersion = 3;

        private static readonly DateTimeOffset UnpublishPublishDate =
            new DateTimeOffset(2024, 3, 4, 5, 6, 7, TimeSpan.Zero);

        // The incumbent as the swap finds it: Approved, actually published with a real
        // PublishDate — so the clearing is observable rather than already true — and sitting at
        // a pinned Version, because where a row sits in its chain is no business of an
        // unpublish. The content type is pinned to a real member because the filler ignores it
        // and every unpinned row would otherwise share default(ContentType).
        private static ContentItem CreateUnpublishStorageContentItem()
        {
            ContentItem contentItem = CreateRandomContentItem();
            contentItem.Id = UnpublishIncumbentContentItemId;
            contentItem.GroupId = UnpublishGroupId;
            contentItem.ContentType = ContentType.Story;
            contentItem.IsDeleted = false;
            contentItem.Version = UnpublishVersion;
            contentItem.ApprovalStatus = ApprovalStatus.Approved;
            contentItem.IsPublished = true;
            contentItem.PublishDate = UnpublishPublishDate;

            return contentItem;
        }

        // The envelope the swap is already acting under. Its CONTENT is the row being
        // promoted, deliberately a different id from the incumbent: a service that took the
        // target off the envelope instead of the argument would unpublish the wrong row, and
        // the storage-read verification below would see it.
        private static EventEnvelope<ContentItem> CreateUnpublishInboundEnvelope(
            SecurityContext securityContext) =>
            new EventEnvelope<ContentItem>
            {
                Content = new ContentItem { Id = UnpublishPromotedContentItemId },
                SecurityContext = securityContext,
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

        // Neither Administrators nor the workflow. The empty set is the plain contributor; the review
        // tier is included because holding write permission on the row is not authority to
        // move an approved one.
        public static TheoryData<string[]> UnpublishRefusedRoleSets() =>
            new TheoryData<string[]>
            {
                new string[0],
                new[] { Roles.Reviewers },
                new[] { Roles.ContentItemReviewers },
            };

        private void SetupUnpublishSaveBrokers() =>
            SetupUnpublishSaveBrokers(savedContentItemSink: null);

        // The tail every permitted unpublish runs through. The snapshot is taken inside the
        // update callback, for the aliasing reason in the class summary.
        private void SetupUnpublishSaveBrokers(Action<ContentItem> savedContentItemSink)
        {
            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<ContentItem>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync((ContentItem entity, SecurityContext _) => entity);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateContentItemAsync(
                    It.IsAny<ContentItem>(),
                    It.IsAny<CancellationToken>()))
                        .Callback<ContentItem, CancellationToken>(
                            (entity, _) => savedContentItemSink?.Invoke(entity.DeepClone()))
                        .ReturnsAsync((ContentItem entity, CancellationToken _) => entity);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishContentItemAsync(
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    It.IsAny<ContentItemEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<ContentItem>>(
                            new EventPublishResult<ContentItem>()));
        }

        // Runs a permitted unpublish over the PUBLIC id-only overload and hands back a
        // snapshot of the row that reached the storage broker.
        private async ValueTask<ContentItem> CaptureSavedContentItemOnUnpublishAsync(
            ContentItem storageContentItem)
        {
            ContentItem savedContentItem = null;
            SetupContentItemStorageRead(storageContentItem);
            SetupUnpublishSaveBrokers(savedContentItemSink: entity => savedContentItem = entity);

            await this.contentItemService.UnpublishContentItemByIdAsync(
                storageContentItem.Id,
                TestContext.Current.CancellationToken);

            return savedContentItem;
        }

        // The same capture, driven over the envelope-forwarding overload the swap actually
        // calls, so a test can prove what the workflow's identity was permitted to write.
        private async ValueTask<ContentItem> CaptureSavedContentItemOnEnvelopeUnpublishAsync(
            ContentItem storageContentItem,
            EventEnvelope<ContentItem> inboundEnvelope)
        {
            ContentItem savedContentItem = null;
            SetupContentItemStorageRead(storageContentItem);
            SetupUnpublishSaveBrokers(savedContentItemSink: entity => savedContentItem = entity);

            await this.contentItemService.UnpublishContentItemByIdAsync(
                storageContentItem.Id,
                inboundEnvelope,
                TestContext.Current.CancellationToken);

            return savedContentItem;
        }

        [Fact]
        public async Task ShouldClearPublicationOnUnpublishAsync()
        {
            // given: the incumbent holds the group's published slot with a real PublishDate.
            // Both fields are the whole of this verb's remit, and a date left behind is a date
            // nothing reads.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            ContentItem storageContentItem = CreateUnpublishStorageContentItem();

            // when
            ContentItem savedContentItem =
                await CaptureSavedContentItemOnUnpublishAsync(storageContentItem);

            // then
            savedContentItem.Should().NotBeNull();
            savedContentItem.IsPublished.Should().BeFalse();
            savedContentItem.PublishDate.Should().BeNull();

            // the argument's row, never the caller's — and read exactly once
            savedContentItem.Id.Should().Be(UnpublishIncumbentContentItemId);
            savedContentItem.GroupId.Should().Be(UnpublishGroupId);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectContentItemByIdAsync(
                        UnpublishIncumbentContentItemId,
                        It.IsAny<CancellationToken>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateContentItemAsync(
                        It.IsAny<ContentItem>(),
                        It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ShouldNotTouchApprovalOrVersionFieldsOnUnpublishAsync()
        {
            // given: the incumbent is SUPERSEDED, not un-approved — it was approved and the
            // record of that must stand (§9.7.7 rule 6). Version is a separate operation's
            // field entirely (§3.4 rule 18): publication and the edit tip move independently,
            // and the tip is DERIVED from Version, so an unpublish that moved this row's
            // Version would silently re-point the whole group's tip.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            ContentItem storageContentItem = CreateUnpublishStorageContentItem();

            // snapshotted BEFORE the act — the service copies onto the instance the read
            // handed it, so a post-act comparison would compare the row with itself
            ContentItem expectedStorageContentItem = storageContentItem.DeepClone();

            // when
            ContentItem savedContentItem =
                await CaptureSavedContentItemOnUnpublishAsync(storageContentItem);

            // then
            savedContentItem.ApprovalStatus.Should().Be(ApprovalStatus.Approved);
            savedContentItem.Version.Should().Be(UnpublishVersion);
            savedContentItem.GroupId.Should().Be(UnpublishGroupId);

            // and nothing else moved either: the whole row against the snapshot, excluding
            // only the two fields this verb owns
            savedContentItem.Should().BeEquivalentTo(
                expectedStorageContentItem,
                options => options
                    .Excluding(contentItem => contentItem.IsPublished)
                    .Excluding(contentItem => contentItem.PublishDate));
        }

        [Fact]
        public async Task ShouldPublishTheUnpublishedFactOnUnpublishAsync()
        {
            // given: its OWN fact. Modified would re-enter the approval workflow that caused
            // this write, and Approved would tell every subscriber a decision was taken when
            // none was — the incumbent's verdict has not changed.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            ContentItem storageContentItem = CreateUnpublishStorageContentItem();

            // when
            await CaptureSavedContentItemOnUnpublishAsync(storageContentItem);

            // then
            this.eventBrokerMock.Verify(broker =>
                    broker.PublishContentItemAsync(
                        It.IsAny<EventEnvelope<ContentItem>>(),
                        ContentItemEventOperation.Unpublished),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishContentItemAsync(
                        It.IsAny<EventEnvelope<ContentItem>>(),
                        ContentItemEventOperation.Modified),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishContentItemAsync(
                        It.IsAny<EventEnvelope<ContentItem>>(),
                        ContentItemEventOperation.Approved),
                Times.Never);

            // the inbound delivery and the outbound fact, both on this operation's own
            // subscription name
            this.storageBrokerMock.Verify(broker =>
                    broker.InsertProcessedEventAsync(
                        It.Is<ProcessedEvent>(processedEvent =>
                            processedEvent.ReceiverName ==
                                EventBrokerIdentifiers
                                    .ContentItemOnContentItemUnpublishedSubscriptionName),
                        It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task ShouldReturnContentItemWithoutWritingOnUnpublishIfAlreadyUnpublishedAsync()
        {
            // given: idempotent. The swap probes for an incumbent and may race another that
            // already cleared the slot — refusing here would fail an approval for work that
            // is already done, and writing anyway would announce an Unpublished fact for a
            // row nothing unpublished.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            ContentItem storageContentItem = CreateUnpublishStorageContentItem();
            storageContentItem.IsPublished = false;
            storageContentItem.PublishDate = null;

            ContentItem expectedContentItem = storageContentItem.DeepClone();

            SetupContentItemStorageRead(storageContentItem);
            SetupUnpublishSaveBrokers();

            // when
            ContentItem actualContentItem =
                await this.contentItemService.UnpublishContentItemByIdAsync(
                    storageContentItem.Id,
                    TestContext.Current.CancellationToken);

            // then: handed back untouched, and the row it still claims to be
            actualContentItem.Should().BeEquivalentTo(expectedContentItem);
            actualContentItem.ApprovalStatus.Should().Be(ApprovalStatus.Approved);
            actualContentItem.Version.Should().Be(UnpublishVersion);

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

            this.storageBrokerMock.Verify(broker =>
                    broker.InsertProcessedEventAsync(
                        It.IsAny<ProcessedEvent>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldUnpublishSoftDeletedContentItemAsync()
        {
            // given: THE reason this verb loads the row itself instead of going through
            // LoadTransitionTargetAsync. A soft delete never clears IsPublished, and the
            // unique index that holds the published slot is filtered on that column alone —
            // so a tombstone still occupies it. Refusing to clear one would leave the group
            // permanently unpublishable (§9.7.7 rule 7). Anyone "tidying up" the load to
            // reuse the shared helper breaks that, and this test is what catches it.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            ContentItem storageContentItem = CreateUnpublishStorageContentItem();
            storageContentItem.IsDeleted = true;

            // when
            ContentItem savedContentItem =
                await CaptureSavedContentItemOnUnpublishAsync(storageContentItem);

            // then: cleared, and still a tombstone — the unpublish revives nothing
            savedContentItem.Should().NotBeNull();
            savedContentItem.IsPublished.Should().BeFalse();
            savedContentItem.PublishDate.Should().BeNull();
            savedContentItem.IsDeleted.Should().BeTrue();

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishContentItemAsync(
                        It.IsAny<EventEnvelope<ContentItem>>(),
                        ContentItemEventOperation.Unpublished),
                Times.Once);
        }

        [Fact]
        public async Task ShouldUnpublishContentItemBySystemIdentityAsync()
        {
            // given: the workflow's own write, which no human is entitled to make — the
            // automatic approval's caller is the reviewer whose review completed the round,
            // and the row being unpublished is Approved. The context is roleless on purpose:
            // the flag is the whole of its authority, so a pass with roles attached would not
            // prove the flag did anything.
            this.ambientSecurityContext = CreateSystemSecurityContext();
            ContentItem storageContentItem = CreateUnpublishStorageContentItem();

            // when
            ContentItem savedContentItem =
                await CaptureSavedContentItemOnUnpublishAsync(storageContentItem);

            // then
            savedContentItem.Should().NotBeNull();
            savedContentItem.IsPublished.Should().BeFalse();
            savedContentItem.PublishDate.Should().BeNull();
            savedContentItem.ApprovalStatus.Should().Be(ApprovalStatus.Approved);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnUnpublishIfIdIsInvalidAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);

            var invalidContentItemException =
                new InvalidContentItemException(
                    message: "ContentItem is invalid, fix the errors and try again.");

            invalidContentItemException.UpsertDataList(
                key: nameof(ContentItem.Id),
                value: "Id is required");

            var expectedContentItemValidationException =
                new ContentItemValidationException(
                    message: "Content item validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemException);

            // when
            ValueTask<ContentItem> unpublishTask =
                this.contentItemService.UnpublishContentItemByIdAsync(
                    Guid.Empty,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualException =
                await Assert.ThrowsAsync<ContentItemValidationException>(unpublishTask.AsTask);

            // then: an invalid id never reaches storage
            actualException.Should().BeEquivalentTo(expectedContentItemValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectContentItemByIdAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowNotFoundOnUnpublishIfTheRowIsMissingAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemByIdAsync(
                    UnpublishIncumbentContentItemId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((ContentItem)null);

            var notFoundContentItemException =
                new NotFoundContentItemException(
                    message: $"Content item not found with id: {UnpublishIncumbentContentItemId}.");

            var expectedContentItemValidationException =
                new ContentItemValidationException(
                    message: "Content item validation error occurred, fix the errors and try again.",
                    innerException: notFoundContentItemException);

            // when
            ValueTask<ContentItem> unpublishTask =
                this.contentItemService.UnpublishContentItemByIdAsync(
                    UnpublishIncumbentContentItemId,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualException =
                await Assert.ThrowsAsync<ContentItemValidationException>(unpublishTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedContentItemValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateContentItemAsync(
                        It.IsAny<ContentItem>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowUnauthorizedOnUnpublishIfCallerIsNotAuthenticatedAsync(
            SecurityContext unauthenticatedContext)
        {
            // given
            this.ambientSecurityContext = unauthenticatedContext;

            var unauthorizedContentItemException =
                new UnauthorizedContentItemException(
                    message: "The current user is not authenticated.");

            var expectedContentItemValidationException =
                new ContentItemValidationException(
                    message: "Content item validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedContentItemException);

            // when
            ValueTask<ContentItem> unpublishTask =
                this.contentItemService.UnpublishContentItemByIdAsync(
                    UnpublishIncumbentContentItemId,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualException =
                await Assert.ThrowsAsync<ContentItemValidationException>(unpublishTask.AsTask);

            // then: the gate refuses before any row is read
            actualException.Should().BeEquivalentTo(expectedContentItemValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectContentItemByIdAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [InlineData(Roles.Publishers)]
        [InlineData(Roles.ContentItemPublishers)]
        public async Task ShouldThrowUnauthorizedOnUnpublishIfCallerIsAPublisherAsync(
            string publisherRole)
        {
            // given: the sharp edge of this gate. The publisher tier decides approvals — but
            // the row being unpublished is itself Approved, and §8.6 HR-4 bars a publisher
            // from moving an approved row, the same reason the status override is
            // Administrators-gated. Widening this verb to the publisher tier would hand a publisher an
            // indirect route to demote content an administrator approved.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(publisherRole);

            var unauthorizedContentItemException =
                new UnauthorizedContentItemException(
                    message: "The current user is not allowed to unpublish this "
                        + "contentItem.");

            var expectedContentItemValidationException =
                new ContentItemValidationException(
                    message: "Content item validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedContentItemException);

            // when
            ValueTask<ContentItem> unpublishTask =
                this.contentItemService.UnpublishContentItemByIdAsync(
                    UnpublishIncumbentContentItemId,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualException =
                await Assert.ThrowsAsync<ContentItemValidationException>(unpublishTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedContentItemValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectContentItemByIdAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [MemberData(nameof(UnpublishRefusedRoleSets))]
        public async Task ShouldThrowUnauthorizedOnUnpublishIfCallerHoldsNoPrivilegedRoleAsync(
            string[] roles)
        {
            // given: a plain contributor and the review tier alike. Neither is Administrators nor the
            // workflow, and holding write permission on the row is not authority to move an
            // approved one (§8.6 HR-3, HR-4).
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(roles);

            var unauthorizedContentItemException =
                new UnauthorizedContentItemException(
                    message: "The current user is not allowed to unpublish this "
                        + "contentItem.");

            var expectedContentItemValidationException =
                new ContentItemValidationException(
                    message: "Content item validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedContentItemException);

            // when
            ValueTask<ContentItem> unpublishTask =
                this.contentItemService.UnpublishContentItemByIdAsync(
                    UnpublishIncumbentContentItemId,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualException =
                await Assert.ThrowsAsync<ContentItemValidationException>(unpublishTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedContentItemValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectContentItemByIdAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldCarryTheInboundIdentityOnEnvelopeUnpublishAsync()
        {
            // given: the overload the swap actually calls. The AMBIENT caller is a plain
            // contributor — on an automatic approval that is the reviewer whose own review
            // completed the round, and they may not unpublish anything. The inbound envelope
            // carries the workflow's system identity, roleless, and that is what must decide.
            // If this overload minted a fresh context instead of chaining, the unpublish would
            // be refused for the one actor entitled to make it.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            EventEnvelope<ContentItem> inboundEnvelope =
                CreateUnpublishInboundEnvelope(CreateSystemSecurityContext());

            ContentItem storageContentItem = CreateUnpublishStorageContentItem();

            // when
            ContentItem savedContentItem =
                await CaptureSavedContentItemOnEnvelopeUnpublishAsync(
                    storageContentItem, inboundEnvelope);

            // then: it succeeded, and it did the same work the public overload does
            savedContentItem.Should().NotBeNull();
            savedContentItem.IsPublished.Should().BeFalse();
            savedContentItem.PublishDate.Should().BeNull();
            savedContentItem.ApprovalStatus.Should().Be(ApprovalStatus.Approved);
            savedContentItem.Version.Should().Be(UnpublishVersion);

            // CHAINED, never minted: CreateNextAsync copies the security context forward off
            // the swap's envelope and keeps causation linked. CreateAsync would read the
            // ambient caller instead, which is the whole failure this test exists to catch.
            this.eventEnvelopeBrokerMock.Verify(broker =>
                    broker.CreateNextAsync(
                        inboundEnvelope,
                        It.Is<ContentItem>(contentItem =>
                            contentItem.Id == UnpublishIncumbentContentItemId)),
                Times.Once);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                    broker.CreateAsync(It.IsAny<ContentItem>()),
                Times.Never);

            // the ARGUMENT's row, not the envelope's content — the envelope carries the row
            // being promoted, which must never be the one unpublished
            this.storageBrokerMock.Verify(broker =>
                    broker.SelectContentItemByIdAsync(
                        UnpublishIncumbentContentItemId,
                        It.IsAny<CancellationToken>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectContentItemByIdAsync(
                        UnpublishPromotedContentItemId,
                        It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishContentItemAsync(
                        It.IsAny<EventEnvelope<ContentItem>>(),
                        ContentItemEventOperation.Unpublished),
                Times.Once);
        }

        [Fact]
        public async Task ShouldThrowUnauthorizedOnEnvelopeUnpublishIfTheInboundIdentityIsNotPermittedAsync()
        {
            // given: the mirror of the test above, and what proves the identity is genuinely
            // taken FROM the envelope rather than merely surviving alongside an ambient one
            // that would have passed anyway. The ambient caller is an administrator; the envelope
            // carries a plain contributor. The envelope must lose.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);

            EventEnvelope<ContentItem> inboundEnvelope =
                CreateUnpublishInboundEnvelope(CreateAuthenticatedSecurityContext());

            var unauthorizedContentItemException =
                new UnauthorizedContentItemException(
                    message: "The current user is not allowed to unpublish this "
                        + "contentItem.");

            var expectedContentItemValidationException =
                new ContentItemValidationException(
                    message: "Content item validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedContentItemException);

            // when
            ValueTask<ContentItem> unpublishTask =
                this.contentItemService.UnpublishContentItemByIdAsync(
                    UnpublishIncumbentContentItemId,
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualException =
                await Assert.ThrowsAsync<ContentItemValidationException>(unpublishTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedContentItemValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectContentItemByIdAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnUnpublishIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);

            var expectedContentItemDependencyException = new ContentItemDependencyException(
                message: "Content item dependency error occurred, contact support.",
                innerException: expectedInnerException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemByIdAsync(
                    UnpublishIncumbentContentItemId,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<ContentItem> unpublishTask =
                this.contentItemService.UnpublishContentItemByIdAsync(
                    UnpublishIncumbentContentItemId,
                    TestContext.Current.CancellationToken);

            ContentItemDependencyException actualContentItemDependencyException =
                await Assert.ThrowsAsync<ContentItemDependencyException>(unpublishTask.AsTask);

            // then
            actualContentItemDependencyException.Should().BeEquivalentTo(
                expectedContentItemDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                    broker.LogErrorAsync(It.Is(
                        SameExceptionAs(expectedContentItemDependencyException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateContentItemAsync(
                        It.IsAny<ContentItem>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowCriticalDependencyExceptionOnUnpublishIfSqlErrorOccursAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            SqlException sqlException = GetSqlException();

            var failedStorageContentItemException = new FailedStorageContentItemException(
                message: "Failed content item storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedContentItemDependencyException = new ContentItemDependencyException(
                message: "Content item dependency error occurred, contact support.",
                innerException: failedStorageContentItemException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemByIdAsync(
                    UnpublishIncumbentContentItemId,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(sqlException);

            // when
            ValueTask<ContentItem> unpublishTask =
                this.contentItemService.UnpublishContentItemByIdAsync(
                    UnpublishIncumbentContentItemId,
                    TestContext.Current.CancellationToken);

            ContentItemDependencyException actualContentItemDependencyException =
                await Assert.ThrowsAsync<ContentItemDependencyException>(unpublishTask.AsTask);

            // then
            actualContentItemDependencyException.Should().BeEquivalentTo(
                expectedContentItemDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                    broker.LogCriticalAsync(It.Is(
                        SameExceptionAs(expectedContentItemDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnUnpublishIfCancellationRequestedAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<ContentItem> unpublishTask =
                this.contentItemService.UnpublishContentItemByIdAsync(
                    UnpublishIncumbentContentItemId,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(unpublishTask.AsTask);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnEnvelopeUnpublishIfCancellationRequestedAsync()
        {
            // given: the swap's route checks the token before it chains an envelope, so a
            // cancelled request never mints causation for work it will not do
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            var cancellationToken = new CancellationToken(canceled: true);

            EventEnvelope<ContentItem> inboundEnvelope =
                CreateUnpublishInboundEnvelope(CreateSystemSecurityContext());

            // when
            ValueTask<ContentItem> unpublishTask =
                this.contentItemService.UnpublishContentItemByIdAsync(
                    UnpublishIncumbentContentItemId,
                    inboundEnvelope,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(unpublishTask.AsTask);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                    broker.CreateNextAsync(
                        It.IsAny<EventEnvelope<ContentItem>>(),
                        It.IsAny<ContentItem>()),
                Times.Never);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
