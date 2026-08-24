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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Force.DeepCloner;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Processings;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
using Glory2Him.Core.Models.Processings.ContentItems.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Processings.ContentItems
{
    /// <summary>
    /// The publication swap (design §9.7.7 rules 6–7, §12.4.1 rule 10).
    ///
    /// <para>A promotion is two rows of one entity in a guaranteed order: the group's incumbent
    /// published row is demoted, and only then is the promote forwarded to the foundation. The
    /// order is a CORRECTNESS requirement — the published slot is held by a unique index
    /// filtered on <c>IsPublished = 1</c>, so promoting first is not untidy, it is refused by
    /// the database.</para>
    ///
    /// <para>Everything in this file is pinned rather than random, and no two ids, groups or
    /// statuses share a value: a probe that mixed up target and incumbent, or group and row,
    /// must fail here rather than coincidentally pass.</para>
    /// </summary>
    public partial class ContentItemProcessingServiceTests
    {
        [Fact]
        public async Task ShouldForwardTheInboundEnvelopeToBothWritesOnApprovingContentItemAsync()
        {
            // given: BOTH writes must carry the workflow's identity, not just the unpublish. The
            // promote used to be a plain two-argument call, which mints a fresh context from the
            // ambient caller — on an automatic approval that is the reviewer whose own review
            // completed the round, and by then the Approval row is no longer Submitted, so the
            // decision function refuses the write deterministically.
            Guid targetContentItemId = Guid.Parse("cccccccc-1111-1111-1111-111111111111");
            Guid incumbentContentItemId = Guid.Parse("cccccccc-2222-2222-2222-222222222222");
            Guid groupId = Guid.Parse("cccccccc-3333-3333-3333-333333333333");

            ContentItem promoteCommand =
                CreatePublicationSwapPromoteCommand(targetContentItemId);

            EventEnvelope<ContentItem> inboundEnvelope =
                CreatePublicationSwapEnvelope(promoteCommand);

            ContentItem storageTargetContentItem = CreatePublicationSwapRow(
                contentItemId: targetContentItemId, groupId: groupId, isPublished: false);

            ContentItem incumbentContentItem = CreatePublicationSwapRow(
                contentItemId: incumbentContentItemId, groupId: groupId, isPublished: true);

            ContentItem promotedContentItem = storageTargetContentItem.DeepClone();
            promotedContentItem.IsPublished = true;
            promotedContentItem.ApprovalStatus = ApprovalStatus.Approved;

            SetupPublicationSwapProbe(
                targetContentItemId: targetContentItemId,
                storageTargetContentItem: storageTargetContentItem,
                groupRows: new List<ContentItem> { incumbentContentItem });

            EventEnvelope<ContentItem> capturedOnUnpublish = null;
            EventEnvelope<ContentItem> capturedOnPromote = null;

            this.contentItemServiceMock.Setup(service =>
                service.UnpublishContentItemByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    It.IsAny<CancellationToken>()))
                        .Callback<Guid, EventEnvelope<ContentItem>, CancellationToken>(
                            (_, envelope, _) => capturedOnUnpublish = envelope)
                        .ReturnsAsync(incumbentContentItem);

            this.contentItemServiceMock.Setup(service =>
                service.TransitionContentItemApprovalAsync(
                    It.IsAny<ContentItem>(),
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    It.IsAny<CancellationToken>()))
                        .Callback<ContentItem, EventEnvelope<ContentItem>, CancellationToken>(
                            (_, envelope, _) => capturedOnPromote = envelope)
                        .ReturnsAsync(promotedContentItem);

            SetupPublicationSwapReply(inboundEnvelope, promotedContentItem);

            // when
            await this.contentItemProcessingService.OnApprovingContentItemAsync(
                inboundEnvelope,
                TestContext.Current.CancellationToken);

            // then: the SAME envelope instance reaches both, so neither re-mints an identity
            capturedOnUnpublish.Should().BeSameAs(inboundEnvelope);
            capturedOnPromote.Should().BeSameAs(inboundEnvelope);

            capturedOnPromote.SecurityContext.IsSystemIdentity.Should().BeTrue();

            // and nothing minted a fresh context on this path
            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(It.IsAny<ContentItem>()),
                Times.Never);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateSystemAsync(It.IsAny<ContentItem>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldDecideEverySwapCallAgainstTheSameActorOnApprovingContentItemAsync()
        {
            // given: every call the swap makes is an identity decision, and they must all be
            // decided against ONE actor. The two writes carried the verified envelope from the
            // start; resolving the group used to go through the caller-FILTERED read, which
            // mints from the ambient caller — so half the operation was decided against whoever
            // happened to be on the thread (#291).
            //
            // Forwarding the envelope into that filtered read does NOT fix it: the swap runs on
            // the workflow's system identity, which has no roles and is not the row's owner, so
            // the filtered read answers not-found. The gated probe is what accepts it, and the
            // foundation Lookup tests pin that half.
            Guid targetContentItemId = Guid.Parse("dddddddd-1111-1111-1111-111111111111");
            Guid incumbentContentItemId = Guid.Parse("dddddddd-2222-2222-2222-222222222222");
            Guid groupId = Guid.Parse("dddddddd-3333-3333-3333-333333333333");

            ContentItem promoteCommand = CreatePublicationSwapPromoteCommand(targetContentItemId);

            EventEnvelope<ContentItem> inboundEnvelope =
                CreatePublicationSwapEnvelope(promoteCommand);

            ContentItem storageTargetContentItem = CreatePublicationSwapRow(
                contentItemId: targetContentItemId, groupId: groupId, isPublished: false);

            ContentItem incumbentContentItem = CreatePublicationSwapRow(
                contentItemId: incumbentContentItemId, groupId: groupId, isPublished: true);

            ContentItem promotedContentItem = storageTargetContentItem.DeepClone();
            promotedContentItem.IsPublished = true;
            promotedContentItem.ApprovalStatus = ApprovalStatus.Approved;

            EventEnvelope<ContentItem> capturedOnProbe = null;
            EventEnvelope<ContentItem> capturedOnUnpublish = null;
            EventEnvelope<ContentItem> capturedOnPromote = null;

            this.contentItemServiceMock.Setup(service =>
                service.FindPublishedSiblingContentItemIdAsync(
                    targetContentItemId,
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    It.IsAny<CancellationToken>()))
                        .Callback<Guid, EventEnvelope<ContentItem>, CancellationToken>(
                            (_, envelope, _) => capturedOnProbe = envelope)
                        .ReturnsAsync(incumbentContentItemId);

            this.contentItemServiceMock.Setup(service =>
                service.UnpublishContentItemByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    It.IsAny<CancellationToken>()))
                        .Callback<Guid, EventEnvelope<ContentItem>, CancellationToken>(
                            (_, envelope, _) => capturedOnUnpublish = envelope)
                        .ReturnsAsync(incumbentContentItem);

            this.contentItemServiceMock.Setup(service =>
                service.TransitionContentItemApprovalAsync(
                    It.IsAny<ContentItem>(),
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    It.IsAny<CancellationToken>()))
                        .Callback<ContentItem, EventEnvelope<ContentItem>, CancellationToken>(
                            (_, envelope, _) => capturedOnPromote = envelope)
                        .ReturnsAsync(promotedContentItem);

            SetupPublicationSwapReply(inboundEnvelope, promotedContentItem);

            // when
            await this.contentItemProcessingService.OnApprovingContentItemAsync(
                inboundEnvelope,
                TestContext.Current.CancellationToken);

            // then: the SAME instance reaches all three, the probe included
            capturedOnProbe.Should().BeSameAs(inboundEnvelope);
            capturedOnUnpublish.Should().BeSameAs(inboundEnvelope);
            capturedOnPromote.Should().BeSameAs(inboundEnvelope);

            // and the caller-FILTERED read is not on this path at all. Without this the test
            // passes on a service that resolves the group through the ambient caller again.
            this.contentItemServiceMock.Verify(service =>
                service.RetrieveContentItemByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            // nothing minted a fresh context anywhere on this path
            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(It.IsAny<ContentItem>()),
                Times.Never);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateSystemAsync(It.IsAny<ContentItem>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldPublishItsOwnCompletionFactOnApprovingContentItemAsync()
        {
            // given: distinct from the foundation's ContentItem-Approved. That one says a row
            // was decided; this one says the GROUP was left consistent — incumbent cleared, new
            // row promoted. A subscriber cannot infer the second from the first, because the
            // foundation publishes before this process has finished (§10.2 rule 5).
            Guid targetContentItemId = Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111");
            Guid groupId = Guid.Parse("aaaaaaaa-3333-3333-3333-333333333333");

            ContentItem promoteCommand =
                CreatePublicationSwapPromoteCommand(targetContentItemId);

            EventEnvelope<ContentItem> inboundEnvelope =
                CreatePublicationSwapEnvelope(promoteCommand);

            ContentItem storageTargetContentItem = CreatePublicationSwapRow(
                contentItemId: targetContentItemId,
                groupId: groupId,
                isPublished: false);

            ContentItem promotedContentItem = storageTargetContentItem.DeepClone();
            promotedContentItem.IsPublished = true;
            promotedContentItem.ApprovalStatus = ApprovalStatus.Approved;

            SetupPublicationSwapProbe(
                targetContentItemId: targetContentItemId,
                storageTargetContentItem: storageTargetContentItem,
                groupRows: new List<ContentItem>());

            this.contentItemServiceMock.Setup(service =>
                service.TransitionContentItemApprovalAsync(
                    It.IsAny<ContentItem>(),
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(promotedContentItem);

            SetupPublicationSwapReply(inboundEnvelope, promotedContentItem);

            // when
            await this.contentItemProcessingService.OnApprovingContentItemAsync(
                inboundEnvelope,
                TestContext.Current.CancellationToken);

            // then
            this.eventBrokerMock.Verify(broker =>
                broker.PublishContentItemProcessingAsync(
                    It.Is<EventEnvelope<ContentItem>>(envelope =>
                        envelope.Content.Id == targetContentItemId
                            && envelope.Content.IsPublished),
                    ContentItemProcessingEventOperation.Approved),
                Times.Once);
        }

        [Fact]
        public async Task ShouldUnpublishIncumbentBeforeForwardingPromoteOnApprovingContentItemAsync()
        {
            // given: the ordering IS the design. A shared step counter is stamped inside both
            // callbacks, so the assertion below is about sequence and not merely about both
            // calls having happened.
            Guid targetContentItemId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            Guid incumbentContentItemId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            Guid groupId = Guid.Parse("33333333-3333-3333-3333-333333333333");

            ContentItem promoteCommand =
                CreatePublicationSwapPromoteCommand(targetContentItemId);

            EventEnvelope<ContentItem> inboundEnvelope =
                CreatePublicationSwapEnvelope(promoteCommand);

            ContentItem storageTargetContentItem = CreatePublicationSwapRow(
                contentItemId: targetContentItemId,
                groupId: groupId,
                isPublished: false);

            ContentItem incumbentContentItem = CreatePublicationSwapRow(
                contentItemId: incumbentContentItemId,
                groupId: groupId,
                isPublished: true);

            ContentItem unpublishedIncumbent = incumbentContentItem.DeepClone();
            unpublishedIncumbent.IsPublished = false;

            ContentItem promotedContentItem = storageTargetContentItem.DeepClone();
            promotedContentItem.IsPublished = true;
            promotedContentItem.ApprovalStatus = ApprovalStatus.Approved;

            SetupPublicationSwapProbe(
                targetContentItemId: targetContentItemId,
                storageTargetContentItem: storageTargetContentItem,
                groupRows: new List<ContentItem> { incumbentContentItem });

            int stepCounter = 0;
            int unpublishStep = 0;
            int promoteStep = 0;

            this.contentItemServiceMock.Setup(service =>
                service.UnpublishContentItemByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    It.IsAny<CancellationToken>()))
                        .Callback(() => unpublishStep = ++stepCounter)
                        .ReturnsAsync(unpublishedIncumbent);

            this.contentItemServiceMock.Setup(service =>
                service.TransitionContentItemApprovalAsync(
                    It.IsAny<ContentItem>(),
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    It.IsAny<CancellationToken>()))
                        .Callback(() => promoteStep = ++stepCounter)
                        .ReturnsAsync(promotedContentItem);

            EventEnvelope<ContentItem> replyEnvelope =
                SetupPublicationSwapReply(inboundEnvelope, promotedContentItem);

            // when
            EventEnvelope<ContentItem>? actualEnvelope =
                await this.contentItemProcessingService.OnApprovingContentItemAsync(
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            // then: demote first, promote second. Reversed, the unique filtered index on
            // (GroupId, IsPublished) refuses the promote and the approval fails outright.
            unpublishStep.Should().Be(1);
            promoteStep.Should().Be(2);

            actualEnvelope.Should().BeSameAs(replyEnvelope);

            this.contentItemServiceMock.Verify(service =>
                service.UnpublishContentItemByIdAsync(
                    incumbentContentItemId,
                    inboundEnvelope,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.contentItemServiceMock.Verify(service =>
                service.TransitionContentItemApprovalAsync(
                    promoteCommand,
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldForwardPromoteWithNoUnpublishOnApprovingContentItemIfGroupHasNoIncumbentAsync()
        {
            // given: the first version of a group ever to be approved. Nothing holds the
            // published slot, so there is nothing to demote — and the promote must still go
            // through (§9.7.7 rule 6: for a group with no previous row the clause is vacuous).
            Guid targetContentItemId = Guid.Parse("44444444-4444-4444-4444-444444444444");
            Guid groupId = Guid.Parse("55555555-5555-5555-5555-555555555555");
            Guid otherGroupId = Guid.Parse("66666666-6666-6666-6666-666666666666");

            Guid otherGroupPublishedContentItemId =
                Guid.Parse("77777777-7777-7777-7777-777777777777");

            ContentItem promoteCommand =
                CreatePublicationSwapPromoteCommand(targetContentItemId);

            EventEnvelope<ContentItem> inboundEnvelope =
                CreatePublicationSwapEnvelope(promoteCommand);

            ContentItem storageTargetContentItem = CreatePublicationSwapRow(
                contentItemId: targetContentItemId,
                groupId: groupId,
                isPublished: false);

            // another group's live row, which is exactly what must NOT be demoted
            ContentItem otherGroupPublishedContentItem = CreatePublicationSwapRow(
                contentItemId: otherGroupPublishedContentItemId,
                groupId: otherGroupId,
                isPublished: true);

            ContentItem promotedContentItem = storageTargetContentItem.DeepClone();
            promotedContentItem.IsPublished = true;
            promotedContentItem.ApprovalStatus = ApprovalStatus.Approved;

            SetupPublicationSwapProbe(
                targetContentItemId: targetContentItemId,
                storageTargetContentItem: storageTargetContentItem,
                groupRows: new List<ContentItem>
                {
                    storageTargetContentItem.DeepClone(),
                    otherGroupPublishedContentItem
                });

            this.contentItemServiceMock.Setup(service =>
                service.TransitionContentItemApprovalAsync(
                    It.IsAny<ContentItem>(),
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(promotedContentItem);

            EventEnvelope<ContentItem> replyEnvelope =
                SetupPublicationSwapReply(inboundEnvelope, promotedContentItem);

            // when
            EventEnvelope<ContentItem>? actualEnvelope =
                await this.contentItemProcessingService.OnApprovingContentItemAsync(
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualEnvelope.Should().BeSameAs(replyEnvelope);

            this.contentItemServiceMock.Verify(service =>
                service.UnpublishContentItemByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.contentItemServiceMock.Verify(service =>
                service.UnpublishContentItemByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.contentItemServiceMock.Verify(service =>
                service.TransitionContentItemApprovalAsync(
                    promoteCommand,
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldUnpublishOnlyTheGroupsPublishedSiblingOnApprovingContentItemAsync()
        {
            // given: the incumbent is the row matching GroupId AND IsPublished AND not the
            // target itself. Every decoy below is seeded BEFORE the true incumbent, so a
            // predicate missing any one of the three conjuncts picks a decoy and this fails:
            //
            //   no IsPublished  → picks the same-group draft
            //   no GroupId      → picks the other group's live row
            //   no Id exclusion → picks the target row itself
            Guid targetContentItemId = Guid.Parse("88888888-8888-8888-8888-888888888888");

            Guid sameGroupDraftContentItemId =
                Guid.Parse("99999999-9999-9999-9999-999999999999");

            Guid otherGroupPublishedContentItemId =
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

            Guid incumbentContentItemId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            Guid groupId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
            Guid otherGroupId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

            ContentItem promoteCommand =
                CreatePublicationSwapPromoteCommand(targetContentItemId);

            EventEnvelope<ContentItem> inboundEnvelope =
                CreatePublicationSwapEnvelope(promoteCommand);

            // the target is ALREADY published — a re-approval of the live row. It matches the
            // group and the flag, and is excluded only by the id comparison.
            ContentItem storageTargetContentItem = CreatePublicationSwapRow(
                contentItemId: targetContentItemId,
                groupId: groupId,
                isPublished: true);

            ContentItem sameGroupDraftContentItem = CreatePublicationSwapRow(
                contentItemId: sameGroupDraftContentItemId,
                groupId: groupId,
                isPublished: false);

            ContentItem otherGroupPublishedContentItem = CreatePublicationSwapRow(
                contentItemId: otherGroupPublishedContentItemId,
                groupId: otherGroupId,
                isPublished: true);

            ContentItem incumbentContentItem = CreatePublicationSwapRow(
                contentItemId: incumbentContentItemId,
                groupId: groupId,
                isPublished: true);

            ContentItem unpublishedIncumbent = incumbentContentItem.DeepClone();
            unpublishedIncumbent.IsPublished = false;

            ContentItem promotedContentItem = storageTargetContentItem.DeepClone();
            promotedContentItem.ApprovalStatus = ApprovalStatus.Approved;

            SetupPublicationSwapProbe(
                targetContentItemId: targetContentItemId,
                storageTargetContentItem: storageTargetContentItem,
                groupRows: new List<ContentItem>
                {
                    sameGroupDraftContentItem,
                    otherGroupPublishedContentItem,
                    storageTargetContentItem.DeepClone(),
                    incumbentContentItem
                });

            var actualUnpublishedContentItemIds = new List<Guid>();

            this.contentItemServiceMock.Setup(service =>
                service.UnpublishContentItemByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    It.IsAny<CancellationToken>()))
                        .Callback<Guid, EventEnvelope<ContentItem>, CancellationToken>(
                            (contentItemId, _, _) =>
                                actualUnpublishedContentItemIds.Add(contentItemId))
                        .ReturnsAsync(unpublishedIncumbent);

            this.contentItemServiceMock.Setup(service =>
                service.TransitionContentItemApprovalAsync(
                    It.IsAny<ContentItem>(),
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(promotedContentItem);

            SetupPublicationSwapReply(inboundEnvelope, promotedContentItem);

            // when
            await this.contentItemProcessingService.OnApprovingContentItemAsync(
                inboundEnvelope,
                TestContext.Current.CancellationToken);

            // then: exactly one row was demoted, and it was the true incumbent
            actualUnpublishedContentItemIds.Should().ContainSingle()
                .Which.Should().Be(incumbentContentItemId);

            actualUnpublishedContentItemIds.Should().NotContain(sameGroupDraftContentItemId);
            actualUnpublishedContentItemIds.Should().NotContain(otherGroupPublishedContentItemId);
            actualUnpublishedContentItemIds.Should().NotContain(targetContentItemId);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldUnpublishSoftDeletedIncumbentOnApprovingContentItemAsync()
        {
            // given: THE TOMBSTONE CASE, and the single most important test in this file.
            //
            // A soft delete never clears IsPublished, and the unique index filter names that
            // column alone — so a removed-but-published row still OCCUPIES the group's
            // published slot. A probe that filtered on IsDeleted, as every read posture in
            // this codebase does, would not see it, would skip the demote, and would leave the
            // group permanently unpublishable: every future approval refused by the index,
            // forever, with nothing in the logs naming the tombstone (§9.7.7 rule 7).
            Guid targetContentItemId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
            Guid tombstoneContentItemId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
            Guid groupId = Guid.Parse("10101010-1010-1010-1010-101010101010");

            ContentItem promoteCommand =
                CreatePublicationSwapPromoteCommand(targetContentItemId);

            EventEnvelope<ContentItem> inboundEnvelope =
                CreatePublicationSwapEnvelope(promoteCommand);

            ContentItem storageTargetContentItem = CreatePublicationSwapRow(
                contentItemId: targetContentItemId,
                groupId: groupId,
                isPublished: false);

            ContentItem tombstoneContentItem = CreatePublicationSwapRow(
                contentItemId: tombstoneContentItemId,
                groupId: groupId,
                isPublished: true,
                isDeleted: true);

            ContentItem unpublishedTombstone = tombstoneContentItem.DeepClone();
            unpublishedTombstone.IsPublished = false;

            ContentItem promotedContentItem = storageTargetContentItem.DeepClone();
            promotedContentItem.IsPublished = true;
            promotedContentItem.ApprovalStatus = ApprovalStatus.Approved;

            SetupPublicationSwapProbe(
                targetContentItemId: targetContentItemId,
                storageTargetContentItem: storageTargetContentItem,
                groupRows: new List<ContentItem> { tombstoneContentItem });

            this.contentItemServiceMock.Setup(service =>
                service.UnpublishContentItemByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(unpublishedTombstone);

            this.contentItemServiceMock.Setup(service =>
                service.TransitionContentItemApprovalAsync(
                    It.IsAny<ContentItem>(),
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(promotedContentItem);

            EventEnvelope<ContentItem> replyEnvelope =
                SetupPublicationSwapReply(inboundEnvelope, promotedContentItem);

            // when
            EventEnvelope<ContentItem>? actualEnvelope =
                await this.contentItemProcessingService.OnApprovingContentItemAsync(
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            // then: the tombstone was demoted and the promote went through behind it
            this.contentItemServiceMock.Verify(service =>
                service.UnpublishContentItemByIdAsync(
                    tombstoneContentItemId,
                    inboundEnvelope,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.contentItemServiceMock.Verify(service =>
                service.TransitionContentItemApprovalAsync(
                    promoteCommand,
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            actualEnvelope.Should().BeSameAs(replyEnvelope);
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(ApprovalStatus.Rejected, false)]
        [InlineData(ApprovalStatus.Approved, false)]
        [InlineData(ApprovalStatus.Submitted, false)]
        public async Task ShouldNotProbeForIncumbentOnApprovingContentItemIfDecisionIsNotAPromotionAsync(
            ApprovalStatus approvalStatus,
            bool isPublished)
        {
            // given: only a PROMOTION takes the published slot. A rejection, a re-open, and an
            // Approved-but-unpublished override all take nothing into it, so no probe may run —
            // reading the whole table on every rejection would be a cost with no purpose, and
            // demoting a live sibling for a decision that publishes nothing would take the
            // group dark for no reason at all.
            Guid targetContentItemId = Guid.Parse("20202020-2020-2020-2020-202020202020");

            var decisionCommand = new ContentItem
            {
                Id = targetContentItemId,
                ApprovalStatus = approvalStatus,
                IsPublished = isPublished
            };

            EventEnvelope<ContentItem> inboundEnvelope =
                CreatePublicationSwapEnvelope(decisionCommand);

            var decidedContentItem = new ContentItem
            {
                Id = targetContentItemId,
                ApprovalStatus = approvalStatus,
                IsPublished = isPublished
            };

            this.contentItemServiceMock.Setup(service =>
                service.TransitionContentItemApprovalAsync(
                    It.IsAny<ContentItem>(),
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(decidedContentItem);

            EventEnvelope<ContentItem> replyEnvelope =
                SetupPublicationSwapReply(inboundEnvelope, decidedContentItem);

            // when
            EventEnvelope<ContentItem>? actualEnvelope =
                await this.contentItemProcessingService.OnApprovingContentItemAsync(
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualEnvelope.Should().BeSameAs(replyEnvelope);

            this.contentItemServiceMock.Verify(service =>
                service.FindPublishedSiblingContentItemIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.contentItemServiceMock.Verify(service =>
                service.UnpublishContentItemByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.contentItemServiceMock.Verify(service =>
                service.TransitionContentItemApprovalAsync(
                    decisionCommand,
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldForwardInboundEnvelopeToUnpublishOnApprovingContentItemAsync()
        {
            // given: the identity is CARRIED, never re-minted. The unpublish is gated to Admin
            // or the workflow (§8.6 HR-4), and on an automatic approval the ambient caller is
            // the reviewer whose own review closed the round — neither. Minting a fresh
            // envelope inside the swap would read that caller and the demote would be refused
            // for the one actor entitled to make it.
            Guid targetContentItemId = Guid.Parse("30303030-3030-3030-3030-303030303030");
            Guid incumbentContentItemId = Guid.Parse("40404040-4040-4040-4040-404040404040");
            Guid groupId = Guid.Parse("50505050-5050-5050-5050-505050505050");

            ContentItem promoteCommand =
                CreatePublicationSwapPromoteCommand(targetContentItemId);

            EventEnvelope<ContentItem> inboundEnvelope =
                CreatePublicationSwapEnvelope(promoteCommand);

            ContentItem storageTargetContentItem = CreatePublicationSwapRow(
                contentItemId: targetContentItemId,
                groupId: groupId,
                isPublished: false);

            ContentItem incumbentContentItem = CreatePublicationSwapRow(
                contentItemId: incumbentContentItemId,
                groupId: groupId,
                isPublished: true);

            ContentItem unpublishedIncumbent = incumbentContentItem.DeepClone();
            unpublishedIncumbent.IsPublished = false;

            ContentItem promotedContentItem = storageTargetContentItem.DeepClone();
            promotedContentItem.IsPublished = true;
            promotedContentItem.ApprovalStatus = ApprovalStatus.Approved;

            SetupPublicationSwapProbe(
                targetContentItemId: targetContentItemId,
                storageTargetContentItem: storageTargetContentItem,
                groupRows: new List<ContentItem> { incumbentContentItem });

            EventEnvelope<ContentItem>? actualUnpublishEnvelope = null;

            this.contentItemServiceMock.Setup(service =>
                service.UnpublishContentItemByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    It.IsAny<CancellationToken>()))
                        .Callback<Guid, EventEnvelope<ContentItem>, CancellationToken>(
                            (_, envelope, _) => actualUnpublishEnvelope = envelope)
                        .ReturnsAsync(unpublishedIncumbent);

            this.contentItemServiceMock.Setup(service =>
                service.TransitionContentItemApprovalAsync(
                    It.IsAny<ContentItem>(),
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(promotedContentItem);

            SetupPublicationSwapReply(inboundEnvelope, promotedContentItem);

            // when
            await this.contentItemProcessingService.OnApprovingContentItemAsync(
                inboundEnvelope,
                TestContext.Current.CancellationToken);

            // then: the same envelope instance, carrying the same security context
            actualUnpublishEnvelope.Should().BeSameAs(inboundEnvelope);

            actualUnpublishEnvelope!.SecurityContext.Should()
                .BeSameAs(inboundEnvelope.SecurityContext);

            actualUnpublishEnvelope.SecurityContext.IsSystemIdentity.Should().BeTrue();

            // the envelope-less overload mints its own context from the ambient caller, which
            // is exactly the mistake this rules out
            this.contentItemServiceMock.Verify(service =>
                service.UnpublishContentItemByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            // and nothing fresh was minted on the way
            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(It.IsAny<ContentItem>()),
                Times.Never);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateSystemAsync(It.IsAny<ContentItem>()),
                Times.Never);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldNotForwardPromoteOnApprovingContentItemIfUnpublishFailsAndLogItAsync()
        {
            // given: the demote is deliberately NOT caught. If the slot cannot be cleared the
            // promote must not be attempted — the index would refuse it anyway, and failing
            // here leaves the incumbent published rather than taking the group dark.
            Guid targetContentItemId = Guid.Parse("60606060-6060-6060-6060-606060606060");
            Guid incumbentContentItemId = Guid.Parse("70707070-7070-7070-7070-707070707070");
            Guid groupId = Guid.Parse("80808080-8080-8080-8080-808080808080");
            string randomMessage = GetRandomString();
            var innerException = new Xeption(message: randomMessage);

            var contentItemDependencyException = new ContentItemDependencyException(
                message: randomMessage,
                innerException: innerException);

            ContentItem promoteCommand =
                CreatePublicationSwapPromoteCommand(targetContentItemId);

            EventEnvelope<ContentItem> inboundEnvelope =
                CreatePublicationSwapEnvelope(promoteCommand);

            ContentItem storageTargetContentItem = CreatePublicationSwapRow(
                contentItemId: targetContentItemId,
                groupId: groupId,
                isPublished: false);

            ContentItem incumbentContentItem = CreatePublicationSwapRow(
                contentItemId: incumbentContentItemId,
                groupId: groupId,
                isPublished: true);

            SetupPublicationSwapProbe(
                targetContentItemId: targetContentItemId,
                storageTargetContentItem: storageTargetContentItem,
                groupRows: new List<ContentItem> { incumbentContentItem });

            this.contentItemServiceMock.Setup(service =>
                service.UnpublishContentItemByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(contentItemDependencyException);

            var expectedContentItemProcessingDependencyException =
                new ContentItemProcessingDependencyException(
                    message: "Content item processing dependency error occurred, contact support.",
                    innerException: innerException);

            // when
            ValueTask<EventEnvelope<ContentItem>?> onApprovingContentItemTask =
                this.contentItemProcessingService.OnApprovingContentItemAsync(
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingDependencyException actualContentItemProcessingDependencyException =
                await Assert.ThrowsAsync<ContentItemProcessingDependencyException>(
                    onApprovingContentItemTask.AsTask);

            // then
            actualContentItemProcessingDependencyException.Should().BeEquivalentTo(
                expectedContentItemProcessingDependencyException);

            this.contentItemServiceMock.Verify(service =>
                service.TransitionContentItemApprovalAsync(
                    It.IsAny<ContentItem>(),
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemProcessingDependencyException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(InvalidEventEnvelopes))]
        public async Task ShouldThrowValidationExceptionOnApprovingContentItemIfEnvelopeIsInvalidAndLogItAsync(
            EventEnvelope<ContentItem>? invalidEnvelope)
        {
            // given: a malformed approval command never reaches the swap — nothing is probed
            // and nothing is demoted
            var invalidContentItemProcessingEventException =
                new InvalidContentItemProcessingEventException(
                    message: "Invalid content item processing event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedContentItemProcessingValidationException =
                new ContentItemProcessingValidationException(
                    message: "Content item processing validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemProcessingEventException);

            // when
            ValueTask<EventEnvelope<ContentItem>?> onApprovingContentItemTask =
                this.contentItemProcessingService.OnApprovingContentItemAsync(
                    invalidEnvelope!,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingValidationException actualContentItemProcessingValidationException =
                await Assert.ThrowsAsync<ContentItemProcessingValidationException>(
                    onApprovingContentItemTask.AsTask);

            // then
            actualContentItemProcessingValidationException.Should().BeEquivalentTo(
                expectedContentItemProcessingValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemProcessingValidationException))),
                Times.Once);

            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task
            ShouldThrowValidationExceptionOnApprovingContentItemIfIntegrityVerificationFailsAndLogItAsync()
        {
            // given: the swap acts on a system identity read straight off the envelope, and
            // forwards that envelope to an Admin-gated unpublish. Without the signature check
            // anyone who can put a message on ContentItemProcessing-Approving declares
            // themselves the workflow and demotes any group's live row (§14.6 rule 4).
            Guid targetContentItemId = Guid.Parse("90909090-9090-9090-9090-909090909090");

            ContentItem promoteCommand =
                CreatePublicationSwapPromoteCommand(targetContentItemId);

            EventEnvelope<ContentItem> inboundEnvelope =
                CreatePublicationSwapEnvelope(promoteCommand);

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    inboundEnvelope,
                    "ContentItemProcessingApproving",
                    EnvelopeDirection.Request))
                        .ReturnsAsync(false);

            var invalidContentItemProcessingEventException =
                new InvalidContentItemProcessingEventException(
                    message: "Invalid content item processing event. " +
                        "Integrity verification failed.");

            var expectedContentItemProcessingValidationException =
                new ContentItemProcessingValidationException(
                    message: "Content item processing validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemProcessingEventException);

            // when
            ValueTask<EventEnvelope<ContentItem>?> onApprovingContentItemTask =
                this.contentItemProcessingService.OnApprovingContentItemAsync(
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingValidationException actualContentItemProcessingValidationException =
                await Assert.ThrowsAsync<ContentItemProcessingValidationException>(
                    onApprovingContentItemTask.AsTask);

            // then
            actualContentItemProcessingValidationException.Should().BeEquivalentTo(
                expectedContentItemProcessingValidationException);

            this.envelopeIntegrityBrokerMock.Verify(broker =>
                broker.VerifyAsync(
                    inboundEnvelope,
                    "ContentItemProcessingApproving",
                    EnvelopeDirection.Request),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemProcessingValidationException))),
                Times.Once);

            // a forged approval command never reaches the foundation
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyValidationExceptions))]
        public async Task
            ShouldThrowDependencyValidationExceptionOnApprovingContentItemIfDependencyValidationErrorOccursAndLogItAsync(
            Xeption dependencyValidationException)
        {
            // given: the incumbent probe is a dependency call like any other
            Guid targetContentItemId = Guid.Parse("a0a0a0a0-a0a0-a0a0-a0a0-a0a0a0a0a0a0");

            ContentItem promoteCommand =
                CreatePublicationSwapPromoteCommand(targetContentItemId);

            EventEnvelope<ContentItem> inboundEnvelope =
                CreatePublicationSwapEnvelope(promoteCommand);

            var expectedContentItemProcessingDependencyValidationException =
                new ContentItemProcessingDependencyValidationException(
                    message: "Content item processing dependency validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: (dependencyValidationException.InnerException as Xeption)!);

            this.contentItemServiceMock.Setup(service =>
                service.FindPublishedSiblingContentItemIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(dependencyValidationException);

            // when
            ValueTask<EventEnvelope<ContentItem>?> onApprovingContentItemTask =
                this.contentItemProcessingService.OnApprovingContentItemAsync(
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingDependencyValidationException
                actualContentItemProcessingDependencyValidationException =
                    await Assert.ThrowsAsync<ContentItemProcessingDependencyValidationException>(
                        onApprovingContentItemTask.AsTask);

            // then
            actualContentItemProcessingDependencyValidationException.Should().BeEquivalentTo(
                expectedContentItemProcessingDependencyValidationException);

            this.contentItemServiceMock.Verify(service =>
                service.TransitionContentItemApprovalAsync(
                    It.IsAny<ContentItem>(),
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemProcessingDependencyValidationException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnApprovingContentItemIfDependencyErrorOccursAndLogItAsync(
            Xeption dependencyException)
        {
            // given
            Guid targetContentItemId = Guid.Parse("b0b0b0b0-b0b0-b0b0-b0b0-b0b0b0b0b0b0");

            var decisionCommand = new ContentItem
            {
                Id = targetContentItemId,
                ApprovalStatus = ApprovalStatus.Rejected,
                IsPublished = false
            };

            EventEnvelope<ContentItem> inboundEnvelope =
                CreatePublicationSwapEnvelope(decisionCommand);

            var expectedContentItemProcessingDependencyException =
                new ContentItemProcessingDependencyException(
                    message: "Content item processing dependency error occurred, contact support.",
                    innerException: (dependencyException.InnerException as Xeption)!);

            this.contentItemServiceMock.Setup(service =>
                service.TransitionContentItemApprovalAsync(
                    It.IsAny<ContentItem>(),
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(dependencyException);

            // when
            ValueTask<EventEnvelope<ContentItem>?> onApprovingContentItemTask =
                this.contentItemProcessingService.OnApprovingContentItemAsync(
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingDependencyException actualContentItemProcessingDependencyException =
                await Assert.ThrowsAsync<ContentItemProcessingDependencyException>(
                    onApprovingContentItemTask.AsTask);

            // then
            actualContentItemProcessingDependencyException.Should().BeEquivalentTo(
                expectedContentItemProcessingDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemProcessingDependencyException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnApprovingContentItemIfOperationCanceledOccursAndLogItAsync()
        {
            // given: a cancellation that is NOT the caller's — the probe timed out
            Guid targetContentItemId = Guid.Parse("c0c0c0c0-c0c0-c0c0-c0c0-c0c0c0c0c0c0");
            var operationCanceledException = new OperationCanceledException();

            ContentItem promoteCommand =
                CreatePublicationSwapPromoteCommand(targetContentItemId);

            EventEnvelope<ContentItem> inboundEnvelope =
                CreatePublicationSwapEnvelope(promoteCommand);

            var timeoutException =
                new TimeoutException("The dependency operation timed out.");

            var timeoutContentItemProcessingException =
                new TimeoutContentItemProcessingException(
                    message: "Failed content item processing timeout error occurred, contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            var expectedContentItemProcessingDependencyException =
                new ContentItemProcessingDependencyException(
                    message: "Content item processing dependency error occurred, contact support.",
                    innerException: timeoutContentItemProcessingException);

            this.contentItemServiceMock.Setup(service =>
                service.FindPublishedSiblingContentItemIdAsync(
                    targetContentItemId,
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<EventEnvelope<ContentItem>?> onApprovingContentItemTask =
                this.contentItemProcessingService.OnApprovingContentItemAsync(
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingDependencyException actualContentItemProcessingDependencyException =
                await Assert.ThrowsAsync<ContentItemProcessingDependencyException>(
                    onApprovingContentItemTask.AsTask);

            // then
            actualContentItemProcessingDependencyException.Should().BeEquivalentTo(
                expectedContentItemProcessingDependencyException);

            this.contentItemServiceMock.Verify(service =>
                service.TransitionContentItemApprovalAsync(
                    It.IsAny<ContentItem>(),
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemProcessingDependencyException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnApprovingContentItemIfCancellationRequestedAsync()
        {
            // given: the caller's own cancellation is rethrown untouched, and nothing is
            // demoted on the way out
            Guid targetContentItemId = Guid.Parse("d0d0d0d0-d0d0-d0d0-d0d0-d0d0d0d0d0d0");

            ContentItem promoteCommand =
                CreatePublicationSwapPromoteCommand(targetContentItemId);

            EventEnvelope<ContentItem> inboundEnvelope =
                CreatePublicationSwapEnvelope(promoteCommand);

            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();

            // when
            ValueTask<EventEnvelope<ContentItem>?> onApprovingContentItemTask =
                this.contentItemProcessingService.OnApprovingContentItemAsync(
                    inboundEnvelope,
                    cancellationTokenSource.Token);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                onApprovingContentItemTask.AsTask);

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnApprovingContentItemIfServiceErrorOccursAndLogItAsync()
        {
            // given
            Guid targetContentItemId = Guid.Parse("e0e0e0e0-e0e0-e0e0-e0e0-e0e0e0e0e0e0");
            var serviceException = new Exception("Service error occurred.");

            ContentItem promoteCommand =
                CreatePublicationSwapPromoteCommand(targetContentItemId);

            EventEnvelope<ContentItem> inboundEnvelope =
                CreatePublicationSwapEnvelope(promoteCommand);

            var failedContentItemProcessingServiceException =
                new FailedContentItemProcessingServiceException(
                    message: "Failed content item processing service error occurred, please contact support.",
                    innerException: serviceException,
                    data: serviceException.Data);

            var expectedContentItemProcessingServiceException =
                new ContentItemProcessingServiceException(
                    message: "Content item processing service error occurred, contact support.",
                    innerException: failedContentItemProcessingServiceException);

            this.contentItemServiceMock.Setup(service =>
                service.FindPublishedSiblingContentItemIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    It.IsAny<CancellationToken>()))
                    .ThrowsAsync(serviceException);

            // when
            ValueTask<EventEnvelope<ContentItem>?> onApprovingContentItemTask =
                this.contentItemProcessingService.OnApprovingContentItemAsync(
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingServiceException actualContentItemProcessingServiceException =
                await Assert.ThrowsAsync<ContentItemProcessingServiceException>(
                    onApprovingContentItemTask.AsTask);

            // then
            actualContentItemProcessingServiceException.Should().BeEquivalentTo(
                expectedContentItemProcessingServiceException);

            this.contentItemServiceMock.Verify(service =>
                service.TransitionContentItemApprovalAsync(
                    It.IsAny<ContentItem>(),
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemProcessingServiceException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // ── suite-local helpers ─────────────────────────────────────────────────────────
        //
        // Prefixed so they cannot collide with the shared fixture's, and deliberately NOT
        // added to it: everything here is pinned rather than random, which is the opposite of
        // what the rest of the suite wants.

        // The workflow's command: Approved AND published, which is the one pairing that makes
        // a promotion and therefore the one that probes.
        private static ContentItem CreatePublicationSwapPromoteCommand(Guid contentItemId) =>
            new ContentItem
            {
                Id = contentItemId,
                ApprovalStatus = ApprovalStatus.Approved,
                IsPublished = true
            };

        // The approval command arrives on the workflow's verified envelope, carrying the
        // system identity the Admin-gated unpublish is admitted by.
        private static EventEnvelope<ContentItem> CreatePublicationSwapEnvelope(
            ContentItem command) =>
            new EventEnvelope<ContentItem>
            {
                Content = command,
                Metadata = new EventMetadata { EventId = Guid.NewGuid() },

                SecurityContext = new SecurityContext
                {
                    IsAuthenticated = true,
                    IsSystemIdentity = true,
                    Roles = []
                }
            };

        // A stored row with its group, publication flag and tombstone state pinned. Filler
        // gives every other column a value, and every id here is passed in rather than
        // generated so no two rows in a test can share one by accident. The content type is
        // pinned to a real member because this suite's filler ignores it, which would
        // otherwise leave every row on default(ContentType).
        private static ContentItem CreatePublicationSwapRow(
            Guid contentItemId,
            Guid groupId,
            bool isPublished,
            bool isDeleted = false)
        {
            ContentItem contentItem = CreateRandomContentItem();
            contentItem.Id = contentItemId;
            contentItem.GroupId = groupId;
            contentItem.ContentType = ContentType.Story;
            contentItem.IsPublished = isPublished;
            contentItem.IsDeleted = isDeleted;

            contentItem.ApprovalStatus = isPublished
                ? ApprovalStatus.Approved
                : ApprovalStatus.Submitted;

            return contentItem;
        }

        // Stubs the swap's single gated probe. The incumbent is resolved here the way the
        // real probe resolves it — published, same group as the STORED target, not the
        // target itself — and DELIBERATELY without dropping soft-deleted rows, because a
        // tombstone that kept IsPublished still holds the slot. The probe's own predicate
        // is pinned in the foundation Lookup tests; this helper only feeds the swap.
        private void SetupPublicationSwapProbe(
            Guid targetContentItemId,
            ContentItem storageTargetContentItem,
            List<ContentItem> groupRows)
        {
            // The swap probes through the UNFILTERED lookup, so the stub resolves the
            // incumbent the way that probe does — published, same group, not the
            // target — and DELIBERATELY does not drop soft-deleted rows. Stubbing
            // the collection read instead is what made the tombstone test a false
            // green: the real collection read filters those rows away.
            ContentItem? incumbent = groupRows.FirstOrDefault(contentItem =>
                contentItem.GroupId == storageTargetContentItem.GroupId
                    && contentItem.IsPublished
                    && contentItem.Id != storageTargetContentItem.Id);

            this.contentItemServiceMock.Setup(service =>
                service.FindPublishedSiblingContentItemIdAsync(
                    targetContentItemId,
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(incumbent?.Id);
        }

        // The reply the swap hands back, which is also the envelope its completion fact rides
        // on — the service chains once and reuses the result for both.
        private EventEnvelope<ContentItem> SetupPublicationSwapReply(
            EventEnvelope<ContentItem> inboundEnvelope,
            ContentItem decidedContentItem)
        {
            var replyEnvelope = new EventEnvelope<ContentItem>
            {
                Content = decidedContentItem,
                SecurityContext = inboundEnvelope.SecurityContext,

                Metadata = new EventMetadata
                {
                    EventId = Guid.NewGuid(),
                    CausationId = inboundEnvelope.Metadata.EventId.ToString()
                }
            };

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateNextAsync(inboundEnvelope, decidedContentItem))
                    .ReturnsAsync(replyEnvelope);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishContentItemProcessingAsync(
                    replyEnvelope,
                    ContentItemProcessingEventOperation.Approved))
                        .ReturnsAsync(new EventPublishResult<ContentItem>());

            return replyEnvelope;
        }
    }
}
