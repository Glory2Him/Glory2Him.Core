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
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.Links.Exceptions;
using Glory2Him.Core.Models.Processings.Links.Exceptions;
using Glory2Him.Core.Services.Processings.Links;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Processings.Links
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
    public partial class LinkProcessingServiceTests
    {
        [Fact]
        public async Task ShouldForwardTheInboundEnvelopeToBothWritesOnApprovingLinkAsync()
        {
            // given: BOTH writes must carry the workflow's identity, not just the unpublish. The
            // promote used to be a plain two-argument call, which mints a fresh context from the
            // ambient caller — on an automatic approval that is the reviewer whose own review
            // completed the round, and by then the Approval row is no longer Submitted, so the
            // decision function refuses the write deterministically.
            Guid targetLinkId = Guid.Parse("cccccccc-1111-1111-1111-111111111111");
            Guid incumbentLinkId = Guid.Parse("cccccccc-2222-2222-2222-222222222222");
            Guid groupId = Guid.Parse("cccccccc-3333-3333-3333-333333333333");

            Link promoteCommand = CreatePublicationSwapPromoteCommand(targetLinkId);

            EventEnvelope<Link> inboundEnvelope =
                CreatePublicationSwapEnvelope(promoteCommand);

            Link storageTargetLink = CreatePublicationSwapRow(
                linkId: targetLinkId, groupId: groupId, isPublished: false);

            Link incumbentLink = CreatePublicationSwapRow(
                linkId: incumbentLinkId, groupId: groupId, isPublished: true);

            Link promotedLink = storageTargetLink.DeepClone();
            promotedLink.IsPublished = true;
            promotedLink.ApprovalStatus = ApprovalStatus.Approved;

            SetupPublicationSwapProbe(
                targetLinkId: targetLinkId,
                storageTargetLink: storageTargetLink,
                groupRows: new List<Link> { incumbentLink });

            EventEnvelope<Link> capturedOnUnpublish = null;
            EventEnvelope<Link> capturedOnPromote = null;

            this.linkServiceMock.Setup(service =>
                service.UnpublishLinkByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<EventEnvelope<Link>>(),
                    It.IsAny<CancellationToken>()))
                        .Callback<Guid, EventEnvelope<Link>, CancellationToken>(
                            (_, envelope, _) => capturedOnUnpublish = envelope)
                        .ReturnsAsync(incumbentLink);

            this.linkServiceMock.Setup(service =>
                service.TransitionLinkApprovalAsync(
                    It.IsAny<Link>(),
                    It.IsAny<EventEnvelope<Link>>(),
                    It.IsAny<CancellationToken>()))
                        .Callback<Link, EventEnvelope<Link>, CancellationToken>(
                            (_, envelope, _) => capturedOnPromote = envelope)
                        .ReturnsAsync(promotedLink);

            SetupPublicationSwapReply(inboundEnvelope, promotedLink);

            // when
            await this.linkProcessingService.OnApprovingLinkAsync(
                inboundEnvelope,
                TestContext.Current.CancellationToken);

            // then: the SAME envelope instance reaches both, so neither re-mints an identity
            capturedOnUnpublish.Should().BeSameAs(inboundEnvelope);
            capturedOnPromote.Should().BeSameAs(inboundEnvelope);

            capturedOnPromote.SecurityContext.IsSystemIdentity.Should().BeTrue();

            // and nothing minted a fresh context on this path
            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(It.IsAny<Link>()),
                Times.Never);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateSystemAsync(It.IsAny<Link>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldDecideEverySwapCallAgainstTheSameActorOnApprovingLinkAsync()
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
            Guid targetLinkId = Guid.Parse("dddddddd-1111-1111-1111-111111111111");
            Guid incumbentLinkId = Guid.Parse("dddddddd-2222-2222-2222-222222222222");
            Guid groupId = Guid.Parse("dddddddd-3333-3333-3333-333333333333");

            Link promoteCommand = CreatePublicationSwapPromoteCommand(targetLinkId);

            EventEnvelope<Link> inboundEnvelope =
                CreatePublicationSwapEnvelope(promoteCommand);

            Link storageTargetLink = CreatePublicationSwapRow(
                linkId: targetLinkId, groupId: groupId, isPublished: false);

            Link incumbentLink = CreatePublicationSwapRow(
                linkId: incumbentLinkId, groupId: groupId, isPublished: true);

            Link promotedLink = storageTargetLink.DeepClone();
            promotedLink.IsPublished = true;
            promotedLink.ApprovalStatus = ApprovalStatus.Approved;

            EventEnvelope<Link> capturedOnProbe = null;
            EventEnvelope<Link> capturedOnUnpublish = null;
            EventEnvelope<Link> capturedOnPromote = null;

            this.linkServiceMock.Setup(service =>
                service.FindPublishedSiblingLinkIdAsync(
                    targetLinkId,
                    It.IsAny<EventEnvelope<Link>>(),
                    It.IsAny<CancellationToken>()))
                        .Callback<Guid, EventEnvelope<Link>, CancellationToken>(
                            (_, envelope, _) => capturedOnProbe = envelope)
                        .ReturnsAsync(incumbentLinkId);

            this.linkServiceMock.Setup(service =>
                service.UnpublishLinkByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<EventEnvelope<Link>>(),
                    It.IsAny<CancellationToken>()))
                        .Callback<Guid, EventEnvelope<Link>, CancellationToken>(
                            (_, envelope, _) => capturedOnUnpublish = envelope)
                        .ReturnsAsync(incumbentLink);

            this.linkServiceMock.Setup(service =>
                service.TransitionLinkApprovalAsync(
                    It.IsAny<Link>(),
                    It.IsAny<EventEnvelope<Link>>(),
                    It.IsAny<CancellationToken>()))
                        .Callback<Link, EventEnvelope<Link>, CancellationToken>(
                            (_, envelope, _) => capturedOnPromote = envelope)
                        .ReturnsAsync(promotedLink);

            SetupPublicationSwapReply(inboundEnvelope, promotedLink);

            // when
            await this.linkProcessingService.OnApprovingLinkAsync(
                inboundEnvelope,
                TestContext.Current.CancellationToken);

            // then: the SAME instance reaches all three, the probe included
            capturedOnProbe.Should().BeSameAs(inboundEnvelope);
            capturedOnUnpublish.Should().BeSameAs(inboundEnvelope);
            capturedOnPromote.Should().BeSameAs(inboundEnvelope);

            // and the caller-FILTERED read is not on this path at all. Without this the test
            // passes on a service that resolves the group through the ambient caller again.
            this.linkServiceMock.Verify(service =>
                service.RetrieveLinkByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            // nothing minted a fresh context anywhere on this path
            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(It.IsAny<Link>()),
                Times.Never);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateSystemAsync(It.IsAny<Link>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldPublishItsOwnCompletionFactOnApprovingLinkAsync()
        {
            // given: distinct from the foundation's Link-Approved. That one says a row was
            // decided; this one says the GROUP was left consistent — incumbent cleared, new row
            // promoted. A subscriber cannot infer the second from the first, because the
            // foundation publishes before this process has finished (§10.2 rule 5).
            Guid targetLinkId = Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111");
            Guid groupId = Guid.Parse("aaaaaaaa-3333-3333-3333-333333333333");

            Link promoteCommand = CreatePublicationSwapPromoteCommand(targetLinkId);

            EventEnvelope<Link> inboundEnvelope =
                CreatePublicationSwapEnvelope(promoteCommand);

            Link storageTargetLink = CreatePublicationSwapRow(
                linkId: targetLinkId,
                groupId: groupId,
                isPublished: false);

            Link promotedLink = storageTargetLink.DeepClone();
            promotedLink.IsPublished = true;
            promotedLink.ApprovalStatus = ApprovalStatus.Approved;

            SetupPublicationSwapProbe(
                targetLinkId: targetLinkId,
                storageTargetLink: storageTargetLink,
                groupRows: new List<Link>());

            this.linkServiceMock.Setup(service =>
                service.TransitionLinkApprovalAsync(
                    It.IsAny<Link>(),
                    It.IsAny<EventEnvelope<Link>>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(promotedLink);

            SetupPublicationSwapReply(inboundEnvelope, promotedLink);

            // when
            await this.linkProcessingService.OnApprovingLinkAsync(
                inboundEnvelope,
                TestContext.Current.CancellationToken);

            // then
            this.eventBrokerMock.Verify(broker =>
                broker.PublishLinkProcessingAsync(
                    It.Is<EventEnvelope<Link>>(envelope =>
                        envelope.Content.Id == targetLinkId
                            && envelope.Content.IsPublished),
                    LinkProcessingEventOperation.Approved),
                Times.Once);
        }

        [Fact]
        public async Task ShouldUnpublishIncumbentBeforeForwardingPromoteOnApprovingLinkAsync()
        {
            // given: the ordering IS the design. A shared step counter is stamped inside both
            // callbacks, so the assertion below is about sequence and not merely about both
            // calls having happened.
            Guid targetLinkId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            Guid incumbentLinkId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            Guid groupId = Guid.Parse("33333333-3333-3333-3333-333333333333");

            Link promoteCommand = CreatePublicationSwapPromoteCommand(targetLinkId);

            EventEnvelope<Link> inboundEnvelope =
                CreatePublicationSwapEnvelope(promoteCommand);

            Link storageTargetLink = CreatePublicationSwapRow(
                linkId: targetLinkId,
                groupId: groupId,
                isPublished: false);

            Link incumbentLink = CreatePublicationSwapRow(
                linkId: incumbentLinkId,
                groupId: groupId,
                isPublished: true);

            Link unpublishedIncumbent = incumbentLink.DeepClone();
            unpublishedIncumbent.IsPublished = false;

            Link promotedLink = storageTargetLink.DeepClone();
            promotedLink.IsPublished = true;
            promotedLink.ApprovalStatus = ApprovalStatus.Approved;

            SetupPublicationSwapProbe(
                targetLinkId: targetLinkId,
                storageTargetLink: storageTargetLink,
                groupRows: new List<Link> { incumbentLink });

            int stepCounter = 0;
            int unpublishStep = 0;
            int promoteStep = 0;

            this.linkServiceMock.Setup(service =>
                service.UnpublishLinkByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<EventEnvelope<Link>>(),
                    It.IsAny<CancellationToken>()))
                        .Callback(() => unpublishStep = ++stepCounter)
                        .ReturnsAsync(unpublishedIncumbent);

            this.linkServiceMock.Setup(service =>
                service.TransitionLinkApprovalAsync(
                    It.IsAny<Link>(),
                    It.IsAny<EventEnvelope<Link>>(),
                    It.IsAny<CancellationToken>()))
                        .Callback(() => promoteStep = ++stepCounter)
                        .ReturnsAsync(promotedLink);

            EventEnvelope<Link> replyEnvelope =
                SetupPublicationSwapReply(inboundEnvelope, promotedLink);

            // when
            EventEnvelope<Link>? actualEnvelope =
                await OnApprovingLinkForPublicationSwapAsync(
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            // then: demote first, promote second. Reversed, the unique filtered index on
            // (GroupId, IsPublished) refuses the promote and the approval fails outright.
            unpublishStep.Should().Be(1);
            promoteStep.Should().Be(2);

            actualEnvelope.Should().BeSameAs(replyEnvelope);

            this.linkServiceMock.Verify(service =>
                service.UnpublishLinkByIdAsync(
                    incumbentLinkId,
                    inboundEnvelope,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.linkServiceMock.Verify(service =>
                service.TransitionLinkApprovalAsync(
                    promoteCommand,
                    It.IsAny<EventEnvelope<Link>>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldForwardPromoteWithNoUnpublishOnApprovingLinkIfGroupHasNoIncumbentAsync()
        {
            // given: the first version of a group ever to be approved. Nothing holds the
            // published slot, so there is nothing to demote — and the promote must still go
            // through (§9.7.7 rule 6: for a group with no previous row the clause is vacuous).
            Guid targetLinkId = Guid.Parse("44444444-4444-4444-4444-444444444444");
            Guid groupId = Guid.Parse("55555555-5555-5555-5555-555555555555");
            Guid otherGroupId = Guid.Parse("66666666-6666-6666-6666-666666666666");
            Guid otherGroupPublishedLinkId = Guid.Parse("77777777-7777-7777-7777-777777777777");

            Link promoteCommand = CreatePublicationSwapPromoteCommand(targetLinkId);

            EventEnvelope<Link> inboundEnvelope =
                CreatePublicationSwapEnvelope(promoteCommand);

            Link storageTargetLink = CreatePublicationSwapRow(
                linkId: targetLinkId,
                groupId: groupId,
                isPublished: false);

            // another group's live row, which is exactly what must NOT be demoted
            Link otherGroupPublishedLink = CreatePublicationSwapRow(
                linkId: otherGroupPublishedLinkId,
                groupId: otherGroupId,
                isPublished: true);

            Link promotedLink = storageTargetLink.DeepClone();
            promotedLink.IsPublished = true;
            promotedLink.ApprovalStatus = ApprovalStatus.Approved;

            SetupPublicationSwapProbe(
                targetLinkId: targetLinkId,
                storageTargetLink: storageTargetLink,
                groupRows: new List<Link>
                {
                    storageTargetLink.DeepClone(),
                    otherGroupPublishedLink
                });

            this.linkServiceMock.Setup(service =>
                service.TransitionLinkApprovalAsync(
                    It.IsAny<Link>(),
                    It.IsAny<EventEnvelope<Link>>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(promotedLink);

            EventEnvelope<Link> replyEnvelope =
                SetupPublicationSwapReply(inboundEnvelope, promotedLink);

            // when
            EventEnvelope<Link>? actualEnvelope =
                await OnApprovingLinkForPublicationSwapAsync(
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualEnvelope.Should().BeSameAs(replyEnvelope);

            this.linkServiceMock.Verify(service =>
                service.UnpublishLinkByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<EventEnvelope<Link>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.linkServiceMock.Verify(service =>
                service.UnpublishLinkByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.linkServiceMock.Verify(service =>
                service.TransitionLinkApprovalAsync(
                    promoteCommand,
                    It.IsAny<EventEnvelope<Link>>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldUnpublishOnlyTheGroupsPublishedSiblingOnApprovingLinkAsync()
        {
            // given: the incumbent is the row matching GroupId AND IsPublished AND not the
            // target itself. Every decoy below is seeded BEFORE the true incumbent, so a
            // predicate missing any one of the three conjuncts picks a decoy and this fails:
            //
            //   no IsPublished  → picks the same-group draft
            //   no GroupId      → picks the other group's live row
            //   no Id exclusion → picks the target row itself
            Guid targetLinkId = Guid.Parse("88888888-8888-8888-8888-888888888888");
            Guid sameGroupDraftLinkId = Guid.Parse("99999999-9999-9999-9999-999999999999");
            Guid otherGroupPublishedLinkId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            Guid incumbentLinkId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            Guid groupId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
            Guid otherGroupId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

            Link promoteCommand = CreatePublicationSwapPromoteCommand(targetLinkId);

            EventEnvelope<Link> inboundEnvelope =
                CreatePublicationSwapEnvelope(promoteCommand);

            // the target is ALREADY published — a re-approval of the live row. It matches the
            // group and the flag, and is excluded only by the id comparison.
            Link storageTargetLink = CreatePublicationSwapRow(
                linkId: targetLinkId,
                groupId: groupId,
                isPublished: true);

            Link sameGroupDraftLink = CreatePublicationSwapRow(
                linkId: sameGroupDraftLinkId,
                groupId: groupId,
                isPublished: false);

            Link otherGroupPublishedLink = CreatePublicationSwapRow(
                linkId: otherGroupPublishedLinkId,
                groupId: otherGroupId,
                isPublished: true);

            Link incumbentLink = CreatePublicationSwapRow(
                linkId: incumbentLinkId,
                groupId: groupId,
                isPublished: true);

            Link unpublishedIncumbent = incumbentLink.DeepClone();
            unpublishedIncumbent.IsPublished = false;

            Link promotedLink = storageTargetLink.DeepClone();
            promotedLink.ApprovalStatus = ApprovalStatus.Approved;

            SetupPublicationSwapProbe(
                targetLinkId: targetLinkId,
                storageTargetLink: storageTargetLink,
                groupRows: new List<Link>
                {
                    sameGroupDraftLink,
                    otherGroupPublishedLink,
                    storageTargetLink.DeepClone(),
                    incumbentLink
                });

            var actualUnpublishedLinkIds = new List<Guid>();

            this.linkServiceMock.Setup(service =>
                service.UnpublishLinkByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<EventEnvelope<Link>>(),
                    It.IsAny<CancellationToken>()))
                        .Callback<Guid, EventEnvelope<Link>, CancellationToken>(
                            (linkId, _, _) => actualUnpublishedLinkIds.Add(linkId))
                        .ReturnsAsync(unpublishedIncumbent);

            this.linkServiceMock.Setup(service =>
                service.TransitionLinkApprovalAsync(
                    It.IsAny<Link>(),
                    It.IsAny<EventEnvelope<Link>>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(promotedLink);

            SetupPublicationSwapReply(inboundEnvelope, promotedLink);

            // when
            await OnApprovingLinkForPublicationSwapAsync(
                inboundEnvelope,
                TestContext.Current.CancellationToken);

            // then: exactly one row was demoted, and it was the true incumbent
            actualUnpublishedLinkIds.Should().ContainSingle()
                .Which.Should().Be(incumbentLinkId);

            actualUnpublishedLinkIds.Should().NotContain(sameGroupDraftLinkId);
            actualUnpublishedLinkIds.Should().NotContain(otherGroupPublishedLinkId);
            actualUnpublishedLinkIds.Should().NotContain(targetLinkId);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldUnpublishSoftDeletedIncumbentOnApprovingLinkAsync()
        {
            // given: THE TOMBSTONE CASE, and the single most important test in this file.
            //
            // A soft delete never clears IsPublished, and the unique index filter names that
            // column alone — so a removed-but-published row still OCCUPIES the group's
            // published slot. A probe that filtered on IsDeleted, as every read posture in
            // this codebase does, would not see it, would skip the demote, and would leave the
            // group permanently unpublishable: every future approval refused by the index,
            // forever, with nothing in the logs naming the tombstone (§9.7.7 rule 7).
            Guid targetLinkId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
            Guid tombstoneLinkId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
            Guid groupId = Guid.Parse("10101010-1010-1010-1010-101010101010");

            Link promoteCommand = CreatePublicationSwapPromoteCommand(targetLinkId);

            EventEnvelope<Link> inboundEnvelope =
                CreatePublicationSwapEnvelope(promoteCommand);

            Link storageTargetLink = CreatePublicationSwapRow(
                linkId: targetLinkId,
                groupId: groupId,
                isPublished: false);

            Link tombstoneLink = CreatePublicationSwapRow(
                linkId: tombstoneLinkId,
                groupId: groupId,
                isPublished: true,
                isDeleted: true);

            Link unpublishedTombstone = tombstoneLink.DeepClone();
            unpublishedTombstone.IsPublished = false;

            Link promotedLink = storageTargetLink.DeepClone();
            promotedLink.IsPublished = true;
            promotedLink.ApprovalStatus = ApprovalStatus.Approved;

            SetupPublicationSwapProbe(
                targetLinkId: targetLinkId,
                storageTargetLink: storageTargetLink,
                groupRows: new List<Link> { tombstoneLink });

            this.linkServiceMock.Setup(service =>
                service.UnpublishLinkByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<EventEnvelope<Link>>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(unpublishedTombstone);

            this.linkServiceMock.Setup(service =>
                service.TransitionLinkApprovalAsync(
                    It.IsAny<Link>(),
                    It.IsAny<EventEnvelope<Link>>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(promotedLink);

            EventEnvelope<Link> replyEnvelope =
                SetupPublicationSwapReply(inboundEnvelope, promotedLink);

            // when
            EventEnvelope<Link>? actualEnvelope =
                await OnApprovingLinkForPublicationSwapAsync(
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            // then: the tombstone was demoted and the promote went through behind it
            this.linkServiceMock.Verify(service =>
                service.UnpublishLinkByIdAsync(
                    tombstoneLinkId,
                    inboundEnvelope,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.linkServiceMock.Verify(service =>
                service.TransitionLinkApprovalAsync(
                    promoteCommand,
                    It.IsAny<EventEnvelope<Link>>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            actualEnvelope.Should().BeSameAs(replyEnvelope);
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(ApprovalStatus.Rejected, false)]
        [InlineData(ApprovalStatus.Approved, false)]
        [InlineData(ApprovalStatus.Submitted, false)]
        public async Task ShouldNotProbeForIncumbentOnApprovingLinkIfDecisionIsNotAPromotionAsync(
            ApprovalStatus approvalStatus,
            bool isPublished)
        {
            // given: only a PROMOTION takes the published slot. A rejection, a re-open, and an
            // Approved-but-unpublished override all take nothing into it, so no probe may run —
            // reading the whole table on every rejection would be a cost with no purpose, and
            // demoting a live sibling for a decision that publishes nothing would take the
            // group dark for no reason at all.
            Guid targetLinkId = Guid.Parse("20202020-2020-2020-2020-202020202020");

            var decisionCommand = new Link
            {
                Id = targetLinkId,
                ApprovalStatus = approvalStatus,
                IsPublished = isPublished
            };

            EventEnvelope<Link> inboundEnvelope =
                CreatePublicationSwapEnvelope(decisionCommand);

            var decidedLink = new Link
            {
                Id = targetLinkId,
                ApprovalStatus = approvalStatus,
                IsPublished = isPublished
            };

            this.linkServiceMock.Setup(service =>
                service.TransitionLinkApprovalAsync(
                    It.IsAny<Link>(),
                    It.IsAny<EventEnvelope<Link>>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(decidedLink);

            EventEnvelope<Link> replyEnvelope =
                SetupPublicationSwapReply(inboundEnvelope, decidedLink);

            // when
            EventEnvelope<Link>? actualEnvelope =
                await OnApprovingLinkForPublicationSwapAsync(
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualEnvelope.Should().BeSameAs(replyEnvelope);

            this.linkServiceMock.Verify(service =>
                service.FindPublishedSiblingLinkIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<EventEnvelope<Link>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.linkServiceMock.Verify(service =>
                service.UnpublishLinkByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<EventEnvelope<Link>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.linkServiceMock.Verify(service =>
                service.TransitionLinkApprovalAsync(
                    decisionCommand,
                    It.IsAny<EventEnvelope<Link>>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.linkServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldForwardInboundEnvelopeToUnpublishOnApprovingLinkAsync()
        {
            // given: the identity is CARRIED, never re-minted. The unpublish is gated to Admin
            // or the workflow (§8.6 HR-4), and on an automatic approval the ambient caller is
            // the reviewer whose own review closed the round — neither. Minting a fresh
            // envelope inside the swap would read that caller and the demote would be refused
            // for the one actor entitled to make it.
            Guid targetLinkId = Guid.Parse("30303030-3030-3030-3030-303030303030");
            Guid incumbentLinkId = Guid.Parse("40404040-4040-4040-4040-404040404040");
            Guid groupId = Guid.Parse("50505050-5050-5050-5050-505050505050");

            Link promoteCommand = CreatePublicationSwapPromoteCommand(targetLinkId);

            EventEnvelope<Link> inboundEnvelope =
                CreatePublicationSwapEnvelope(promoteCommand);

            Link storageTargetLink = CreatePublicationSwapRow(
                linkId: targetLinkId,
                groupId: groupId,
                isPublished: false);

            Link incumbentLink = CreatePublicationSwapRow(
                linkId: incumbentLinkId,
                groupId: groupId,
                isPublished: true);

            Link unpublishedIncumbent = incumbentLink.DeepClone();
            unpublishedIncumbent.IsPublished = false;

            Link promotedLink = storageTargetLink.DeepClone();
            promotedLink.IsPublished = true;
            promotedLink.ApprovalStatus = ApprovalStatus.Approved;

            SetupPublicationSwapProbe(
                targetLinkId: targetLinkId,
                storageTargetLink: storageTargetLink,
                groupRows: new List<Link> { incumbentLink });

            EventEnvelope<Link>? actualUnpublishEnvelope = null;

            this.linkServiceMock.Setup(service =>
                service.UnpublishLinkByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<EventEnvelope<Link>>(),
                    It.IsAny<CancellationToken>()))
                        .Callback<Guid, EventEnvelope<Link>, CancellationToken>(
                            (_, envelope, _) => actualUnpublishEnvelope = envelope)
                        .ReturnsAsync(unpublishedIncumbent);

            this.linkServiceMock.Setup(service =>
                service.TransitionLinkApprovalAsync(
                    It.IsAny<Link>(),
                    It.IsAny<EventEnvelope<Link>>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(promotedLink);

            SetupPublicationSwapReply(inboundEnvelope, promotedLink);

            // when
            await OnApprovingLinkForPublicationSwapAsync(
                inboundEnvelope,
                TestContext.Current.CancellationToken);

            // then: the same envelope instance, carrying the same security context
            actualUnpublishEnvelope.Should().BeSameAs(inboundEnvelope);

            actualUnpublishEnvelope!.SecurityContext.Should()
                .BeSameAs(inboundEnvelope.SecurityContext);

            actualUnpublishEnvelope.SecurityContext.IsSystemIdentity.Should().BeTrue();

            // the envelope-less overload mints its own context from the ambient caller, which
            // is exactly the mistake this rules out
            this.linkServiceMock.Verify(service =>
                service.UnpublishLinkByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            // and nothing fresh was minted on the way
            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(It.IsAny<Link>()),
                Times.Never);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateSystemAsync(It.IsAny<Link>()),
                Times.Never);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldNotForwardPromoteOnApprovingLinkIfUnpublishFailsAndLogItAsync()
        {
            // given: the demote is deliberately NOT caught. If the slot cannot be cleared the
            // promote must not be attempted — the index would refuse it anyway, and failing
            // here leaves the incumbent published rather than taking the group dark.
            Guid targetLinkId = Guid.Parse("60606060-6060-6060-6060-606060606060");
            Guid incumbentLinkId = Guid.Parse("70707070-7070-7070-7070-707070707070");
            Guid groupId = Guid.Parse("80808080-8080-8080-8080-808080808080");
            string randomMessage = GetRandomString();
            var innerException = new Xeption(message: randomMessage);

            var linkDependencyException = new LinkDependencyException(
                message: randomMessage,
                innerException: innerException);

            Link promoteCommand = CreatePublicationSwapPromoteCommand(targetLinkId);

            EventEnvelope<Link> inboundEnvelope =
                CreatePublicationSwapEnvelope(promoteCommand);

            Link storageTargetLink = CreatePublicationSwapRow(
                linkId: targetLinkId,
                groupId: groupId,
                isPublished: false);

            Link incumbentLink = CreatePublicationSwapRow(
                linkId: incumbentLinkId,
                groupId: groupId,
                isPublished: true);

            SetupPublicationSwapProbe(
                targetLinkId: targetLinkId,
                storageTargetLink: storageTargetLink,
                groupRows: new List<Link> { incumbentLink });

            this.linkServiceMock.Setup(service =>
                service.UnpublishLinkByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<EventEnvelope<Link>>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(linkDependencyException);

            var expectedLinkProcessingDependencyException =
                new LinkProcessingDependencyException(
                    message: "Link processing dependency error occurred, contact support.",
                    innerException: innerException);

            // when
            ValueTask<EventEnvelope<Link>?> onApprovingLinkTask =
                OnApprovingLinkForPublicationSwapAsync(
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            LinkProcessingDependencyException actualLinkProcessingDependencyException =
                await Assert.ThrowsAsync<LinkProcessingDependencyException>(
                    onApprovingLinkTask.AsTask);

            // then
            actualLinkProcessingDependencyException.Should().BeEquivalentTo(
                expectedLinkProcessingDependencyException);

            this.linkServiceMock.Verify(service =>
                service.TransitionLinkApprovalAsync(
                    It.IsAny<Link>(),
                    It.IsAny<EventEnvelope<Link>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkProcessingDependencyException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(MalformedEnvelopes))]
        public async Task ShouldThrowValidationExceptionOnApprovingLinkIfEnvelopeIsMalformedAndLogItAsync(
            EventEnvelope<Link>? malformedEnvelope)
        {
            // given: a malformed approval command never reaches the swap — nothing is probed
            // and nothing is demoted
            var invalidLinkProcessingEventException =
                new InvalidLinkProcessingEventException(
                    message: "Invalid link processing event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedLinkProcessingValidationException =
                new LinkProcessingValidationException(
                    message: "Link processing validation error occurred, fix the errors and try again.",
                    innerException: invalidLinkProcessingEventException);

            // when
            ValueTask<EventEnvelope<Link>?> onApprovingLinkTask =
                OnApprovingLinkForPublicationSwapAsync(
                    malformedEnvelope!,
                    TestContext.Current.CancellationToken);

            LinkProcessingValidationException actualLinkProcessingValidationException =
                await Assert.ThrowsAsync<LinkProcessingValidationException>(
                    onApprovingLinkTask.AsTask);

            // then
            actualLinkProcessingValidationException.Should().BeEquivalentTo(
                expectedLinkProcessingValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkProcessingValidationException))),
                Times.Once);

            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();
            this.linkServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task
            ShouldThrowValidationExceptionOnApprovingLinkIfIntegrityVerificationFailsAndLogItAsync()
        {
            // given: the swap acts on a system identity read straight off the envelope, and
            // forwards that envelope to an Admin-gated unpublish. Without the signature check
            // anyone who can put a message on LinkProcessing-Approving declares themselves the
            // workflow and demotes any group's live row (§14.6 rule 4).
            Guid targetLinkId = Guid.Parse("90909090-9090-9090-9090-909090909090");
            Link promoteCommand = CreatePublicationSwapPromoteCommand(targetLinkId);

            EventEnvelope<Link> inboundEnvelope =
                CreatePublicationSwapEnvelope(promoteCommand);

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    inboundEnvelope,
                    "LinkProcessingApproving",
                    EnvelopeDirection.Request))
                        .ReturnsAsync(false);

            var invalidLinkProcessingEventException =
                new InvalidLinkProcessingEventException(
                    message: "Invalid link processing event. " +
                        "Integrity verification failed.");

            var expectedLinkProcessingValidationException =
                new LinkProcessingValidationException(
                    message: "Link processing validation error occurred, fix the errors and try again.",
                    innerException: invalidLinkProcessingEventException);

            // when
            ValueTask<EventEnvelope<Link>?> onApprovingLinkTask =
                OnApprovingLinkForPublicationSwapAsync(
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            LinkProcessingValidationException actualLinkProcessingValidationException =
                await Assert.ThrowsAsync<LinkProcessingValidationException>(
                    onApprovingLinkTask.AsTask);

            // then
            actualLinkProcessingValidationException.Should().BeEquivalentTo(
                expectedLinkProcessingValidationException);

            this.envelopeIntegrityBrokerMock.Verify(broker =>
                broker.VerifyAsync(
                    inboundEnvelope,
                    "LinkProcessingApproving",
                    EnvelopeDirection.Request),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkProcessingValidationException))),
                Times.Once);

            // a forged approval command never reaches the foundation
            this.linkServiceMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyValidationExceptions))]
        public async Task
            ShouldThrowDependencyValidationExceptionOnApprovingLinkIfDependencyValidationErrorOccursAndLogItAsync(
            Xeption dependencyValidationException)
        {
            // given: the incumbent probe is a dependency call like any other
            Guid targetLinkId = Guid.Parse("a0a0a0a0-a0a0-a0a0-a0a0-a0a0a0a0a0a0");
            Link promoteCommand = CreatePublicationSwapPromoteCommand(targetLinkId);

            EventEnvelope<Link> inboundEnvelope =
                CreatePublicationSwapEnvelope(promoteCommand);

            var expectedLinkProcessingDependencyValidationException =
                new LinkProcessingDependencyValidationException(
                    message: "Link processing dependency validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: (dependencyValidationException.InnerException as Xeption)!);

            this.linkServiceMock.Setup(service =>
                service.FindPublishedSiblingLinkIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<EventEnvelope<Link>>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(dependencyValidationException);

            // when
            ValueTask<EventEnvelope<Link>?> onApprovingLinkTask =
                OnApprovingLinkForPublicationSwapAsync(
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            LinkProcessingDependencyValidationException
                actualLinkProcessingDependencyValidationException =
                    await Assert.ThrowsAsync<LinkProcessingDependencyValidationException>(
                        onApprovingLinkTask.AsTask);

            // then
            actualLinkProcessingDependencyValidationException.Should().BeEquivalentTo(
                expectedLinkProcessingDependencyValidationException);

            this.linkServiceMock.Verify(service =>
                service.TransitionLinkApprovalAsync(
                    It.IsAny<Link>(),
                    It.IsAny<EventEnvelope<Link>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkProcessingDependencyValidationException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnApprovingLinkIfDependencyErrorOccursAndLogItAsync(
            Xeption dependencyException)
        {
            // given
            Guid targetLinkId = Guid.Parse("b0b0b0b0-b0b0-b0b0-b0b0-b0b0b0b0b0b0");

            var decisionCommand = new Link
            {
                Id = targetLinkId,
                ApprovalStatus = ApprovalStatus.Rejected,
                IsPublished = false
            };

            EventEnvelope<Link> inboundEnvelope =
                CreatePublicationSwapEnvelope(decisionCommand);

            var expectedLinkProcessingDependencyException =
                new LinkProcessingDependencyException(
                    message: "Link processing dependency error occurred, contact support.",
                    innerException: (dependencyException.InnerException as Xeption)!);

            this.linkServiceMock.Setup(service =>
                service.TransitionLinkApprovalAsync(
                    It.IsAny<Link>(),
                    It.IsAny<EventEnvelope<Link>>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(dependencyException);

            // when
            ValueTask<EventEnvelope<Link>?> onApprovingLinkTask =
                OnApprovingLinkForPublicationSwapAsync(
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            LinkProcessingDependencyException actualLinkProcessingDependencyException =
                await Assert.ThrowsAsync<LinkProcessingDependencyException>(
                    onApprovingLinkTask.AsTask);

            // then
            actualLinkProcessingDependencyException.Should().BeEquivalentTo(
                expectedLinkProcessingDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkProcessingDependencyException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnApprovingLinkIfOperationCanceledOccursAndLogItAsync()
        {
            // given: a cancellation that is NOT the caller's — the probe timed out
            Guid targetLinkId = Guid.Parse("c0c0c0c0-c0c0-c0c0-c0c0-c0c0c0c0c0c0");
            var operationCanceledException = new OperationCanceledException();
            Link promoteCommand = CreatePublicationSwapPromoteCommand(targetLinkId);

            EventEnvelope<Link> inboundEnvelope =
                CreatePublicationSwapEnvelope(promoteCommand);

            var timeoutException =
                new TimeoutException("The dependency operation timed out.");

            var timeoutLinkProcessingException =
                new TimeoutLinkProcessingException(
                    message: "Failed link processing timeout error occurred, contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            var expectedLinkProcessingDependencyException =
                new LinkProcessingDependencyException(
                    message: "Link processing dependency error occurred, contact support.",
                    innerException: timeoutLinkProcessingException);

            this.linkServiceMock.Setup(service =>
                service.FindPublishedSiblingLinkIdAsync(
                    targetLinkId,
                    It.IsAny<EventEnvelope<Link>>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<EventEnvelope<Link>?> onApprovingLinkTask =
                OnApprovingLinkForPublicationSwapAsync(
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            LinkProcessingDependencyException actualLinkProcessingDependencyException =
                await Assert.ThrowsAsync<LinkProcessingDependencyException>(
                    onApprovingLinkTask.AsTask);

            // then
            actualLinkProcessingDependencyException.Should().BeEquivalentTo(
                expectedLinkProcessingDependencyException);

            this.linkServiceMock.Verify(service =>
                service.TransitionLinkApprovalAsync(
                    It.IsAny<Link>(),
                    It.IsAny<EventEnvelope<Link>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkProcessingDependencyException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnApprovingLinkIfCancellationRequestedAsync()
        {
            // given: the caller's own cancellation is rethrown untouched, and nothing is
            // demoted on the way out
            Guid targetLinkId = Guid.Parse("d0d0d0d0-d0d0-d0d0-d0d0-d0d0d0d0d0d0");
            Link promoteCommand = CreatePublicationSwapPromoteCommand(targetLinkId);

            EventEnvelope<Link> inboundEnvelope =
                CreatePublicationSwapEnvelope(promoteCommand);

            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();

            // when
            ValueTask<EventEnvelope<Link>?> onApprovingLinkTask =
                OnApprovingLinkForPublicationSwapAsync(
                    inboundEnvelope,
                    cancellationTokenSource.Token);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                onApprovingLinkTask.AsTask);

            this.linkServiceMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnApprovingLinkIfServiceErrorOccursAndLogItAsync()
        {
            // given
            Guid targetLinkId = Guid.Parse("e0e0e0e0-e0e0-e0e0-e0e0-e0e0e0e0e0e0");
            var serviceException = new Exception("Service error occurred.");
            Link promoteCommand = CreatePublicationSwapPromoteCommand(targetLinkId);

            EventEnvelope<Link> inboundEnvelope =
                CreatePublicationSwapEnvelope(promoteCommand);

            var failedLinkProcessingServiceException =
                new FailedLinkProcessingServiceException(
                    message: "Failed link processing service error occurred, please contact support.",
                    innerException: serviceException,
                    data: serviceException.Data);

            var expectedLinkProcessingServiceException =
                new LinkProcessingServiceException(
                    message: "Link processing service error occurred, contact support.",
                    innerException: failedLinkProcessingServiceException);

            this.linkServiceMock.Setup(service =>
                service.FindPublishedSiblingLinkIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<EventEnvelope<Link>>(),
                    It.IsAny<CancellationToken>()))
                    .ThrowsAsync(serviceException);

            // when
            ValueTask<EventEnvelope<Link>?> onApprovingLinkTask =
                OnApprovingLinkForPublicationSwapAsync(
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            LinkProcessingServiceException actualLinkProcessingServiceException =
                await Assert.ThrowsAsync<LinkProcessingServiceException>(
                    onApprovingLinkTask.AsTask);

            // then
            actualLinkProcessingServiceException.Should().BeEquivalentTo(
                expectedLinkProcessingServiceException);

            this.linkServiceMock.Verify(service =>
                service.TransitionLinkApprovalAsync(
                    It.IsAny<Link>(),
                    It.IsAny<EventEnvelope<Link>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkProcessingServiceException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // ── suite-local helpers ─────────────────────────────────────────────────────────
        //
        // Prefixed so they cannot collide with the shared fixture's, and deliberately NOT
        // added to it: everything here is pinned rather than random, which is the opposite of
        // what the rest of the suite wants.

        // OnApprovingLinkAsync is declared on the concrete LinkProcessingService and not on
        // ILinkProcessingService, so the suite reaches it through the very instance the
        // fixture built — same mocks, so every Verify above still speaks about it.
        private ValueTask<EventEnvelope<Link>?> OnApprovingLinkForPublicationSwapAsync(
            EventEnvelope<Link> envelope,
            CancellationToken cancellationToken) =>
            this.linkProcessingService.OnApprovingLinkAsync(envelope, cancellationToken);

        // The workflow's command: Approved AND published, which is the one pairing that makes
        // a promotion and therefore the one that probes.
        private static Link CreatePublicationSwapPromoteCommand(Guid linkId) =>
            new Link
            {
                Id = linkId,
                ApprovalStatus = ApprovalStatus.Approved,
                IsPublished = true
            };

        // The approval command arrives on the workflow's verified envelope, carrying the
        // system identity the Admin-gated unpublish is admitted by.
        private static EventEnvelope<Link> CreatePublicationSwapEnvelope(Link command) =>
            new EventEnvelope<Link>
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
        // generated so no two rows in a test can share one by accident.
        private static Link CreatePublicationSwapRow(
            Guid linkId,
            Guid groupId,
            bool isPublished,
            bool isDeleted = false)
        {
            Link link = CreateRandomLink();
            link.Id = linkId;
            link.GroupId = groupId;
            link.IsPublished = isPublished;
            link.IsDeleted = isDeleted;

            link.ApprovalStatus = isPublished
                ? ApprovalStatus.Approved
                : ApprovalStatus.Submitted;

            return link;
        }

        // Stubs the swap's single gated probe. The incumbent is resolved here the way the
        // real probe resolves it — published, same group as the STORED target, not the
        // target itself — and DELIBERATELY without dropping soft-deleted rows, because a
        // tombstone that kept IsPublished still holds the slot. The probe's own predicate
        // is pinned in the foundation Lookup tests; this helper only feeds the swap.
        private void SetupPublicationSwapProbe(
            Guid targetLinkId,
            Link storageTargetLink,
            List<Link> groupRows)
        {
            // The swap probes through the UNFILTERED lookup, so the stub resolves the
            // incumbent the way that probe does — published, same group, not the
            // target — and DELIBERATELY does not drop soft-deleted rows. Stubbing
            // the collection read instead is what made the tombstone test a false
            // green: the real collection read filters those rows away.
            Link? incumbent = groupRows.FirstOrDefault(link =>
                link.GroupId == storageTargetLink.GroupId
                    && link.IsPublished
                    && link.Id != storageTargetLink.Id);

            this.linkServiceMock.Setup(service =>
                service.FindPublishedSiblingLinkIdAsync(
                    targetLinkId,
                    It.IsAny<EventEnvelope<Link>>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(incumbent?.Id);
        }

        private EventEnvelope<Link> SetupPublicationSwapReply(
            EventEnvelope<Link> inboundEnvelope,
            Link decidedLink)
        {
            var replyEnvelope = new EventEnvelope<Link>
            {
                Content = decidedLink,
                SecurityContext = inboundEnvelope.SecurityContext,

                Metadata = new EventMetadata
                {
                    EventId = Guid.NewGuid(),
                    CausationId = inboundEnvelope.Metadata.EventId.ToString()
                }
            };

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateNextAsync(inboundEnvelope, decidedLink))
                    .ReturnsAsync(replyEnvelope);

            return replyEnvelope;
        }
    }
}
