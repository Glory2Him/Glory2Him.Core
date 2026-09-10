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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Links;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Links
{
    public partial class LinkServiceTests
    {
        // WHAT THESE TESTS CAN AND CANNOT SAY. The group narrowing is a STORAGE predicate —
        // SelectLinksByGroupIdAsync carries it, with the token — so the stubs below hand back a
        // group's rows without re-implementing "GroupId == groupId". A stub that re-implemented
        // it would pass whether or not the real read still carried it; that the read is keyed on
        // the group at all is proved against a real catalogue by the acceptance test
        // LinkTests.GroupReads.ShouldServeExactlyTheGroupsNonDeletedVersionsFromTheGroupReadAsync,
        // which seeds an other-group row and asserts it never reaches the wire.
        //
        // What that acceptance test CANNOT see, and what #486 left unasserted when it removed
        // the link narrow-read fixture, is that the storage read is unfiltered on IsDeleted:
        // RetrieveLinksByGroupIdAsync runs the §14.7 visibility filter over whatever comes back,
        // which drops tombstones either way, so the wire payload is identical whether or not the
        // predicate carries an IsDeleted conjunct.
        //
        // What is proved here is the half the SERVICE owns: the group it asks storage for is the
        // one the caller named, and the §14.7 visibility filter runs over what comes back. That
        // rule moved down from LinkProcessingService when the processing group read stopped
        // filtering a second time — the filter has one home, so it is tested at it.
        [Fact]
        public async Task ShouldRetrieveOnlyPublicGroupLinksWhenCallerIsAnonymousAsync()
        {
            // given
            Guid inputGroupId = Guid.NewGuid();
            this.ambientSecurityContext = new SecurityContext { IsAuthenticated = false };
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            Link publicLink = CreateRandomGroupLink(inputGroupId);
            publicLink.ApprovalStatus = ApprovalStatus.Approved;
            publicLink.IsPublished = true;
            publicLink.PublishDate = null;

            Link pastPublishedLink = CreateRandomGroupLink(inputGroupId);
            pastPublishedLink.ApprovalStatus = ApprovalStatus.Approved;
            pastPublishedLink.IsPublished = true;
            pastPublishedLink.PublishDate = randomDateTimeOffset.AddDays(GetRandomNegativeNumber());

            Link draftLink = CreateRandomGroupLink(inputGroupId);
            draftLink.ApprovalStatus = ApprovalStatus.Draft;
            draftLink.IsPublished = false;

            Link futurePublishedLink = CreateRandomGroupLink(inputGroupId);
            futurePublishedLink.ApprovalStatus = ApprovalStatus.Approved;
            futurePublishedLink.IsPublished = true;
            futurePublishedLink.PublishDate = randomDateTimeOffset.AddDays(GetRandomNumber());

            Link deletedLink = CreateRandomGroupLink(inputGroupId);
            deletedLink.IsDeleted = true;
            deletedLink.ApprovalStatus = ApprovalStatus.Approved;
            deletedLink.IsPublished = true;
            deletedLink.PublishDate = null;

            var storageLinks = new List<Link>
            {
                publicLink,
                pastPublishedLink,
                draftLink,
                futurePublishedLink,
                deletedLink
            };

            var expectedLinks = new List<Link>
            {
                publicLink,
                pastPublishedLink
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectLinksByGroupIdAsync(inputGroupId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageLinks);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            IReadOnlyList<Link> actualLinks =
                await this.linkService.RetrieveLinksByGroupIdAsync(
                    inputGroupId,
                    TestContext.Current.CancellationToken);

            // then
            actualLinks.Should().BeEquivalentTo(expectedLinks);

            // the group the caller named, unaltered — the one thing about the narrowing this
            // seam can still prove
            this.storageBrokerMock.Verify(broker =>
                broker.SelectLinksByGroupIdAsync(inputGroupId, It.IsAny<CancellationToken>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRetrievePublicAndOwnGroupLinksWhenUserHasNoReviewRoleAsync()
        {
            // given: the owner follows their own versions of the group through the workflow,
            // while another caller's non-public version of the same group stays invisible
            Guid inputGroupId = Guid.NewGuid();
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string actorUserId = GetRandomString();

            Link publicLink = CreateRandomGroupLink(inputGroupId);
            publicLink.ApprovalStatus = ApprovalStatus.Approved;
            publicLink.IsPublished = true;
            publicLink.PublishDate = null;

            Link ownDraftLink = CreateRandomGroupLink(inputGroupId);
            ownDraftLink.ApprovalStatus = ApprovalStatus.Draft;
            ownDraftLink.IsPublished = false;
            ownDraftLink.CreatedBy = actorUserId;

            Link otherDraftLink = CreateRandomGroupLink(inputGroupId);
            otherDraftLink.ApprovalStatus = ApprovalStatus.Draft;
            otherDraftLink.IsPublished = false;

            Link ownDeletedLink = CreateRandomGroupLink(inputGroupId);
            ownDeletedLink.IsDeleted = true;
            ownDeletedLink.CreatedBy = actorUserId;

            var storageLinks = new List<Link>
            {
                publicLink,
                ownDraftLink,
                otherDraftLink,
                ownDeletedLink
            };

            var expectedLinks = new List<Link>
            {
                publicLink,
                ownDraftLink
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectLinksByGroupIdAsync(inputGroupId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageLinks);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(actorUserId);

            // when
            IReadOnlyList<Link> actualLinks =
                await this.linkService.RetrieveLinksByGroupIdAsync(
                    inputGroupId,
                    TestContext.Current.CancellationToken);

            // then
            actualLinks.Should().BeEquivalentTo(expectedLinks);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectLinksByGroupIdAsync(inputGroupId, It.IsAny<CancellationToken>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ReviewRoles))]
        public async Task ShouldRetrieveAllNonDeletedGroupLinksWhenUserHasReviewRoleAsync(
            string reviewRole)
        {
            // given: a review-role caller audits every non-deleted version of the group —
            // drafts and future-scheduled rows included — with neither the clock nor the
            // caller's identity consulted
            Guid inputGroupId = Guid.NewGuid();
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(reviewRole);

            Link publicLink = CreateRandomGroupLink(inputGroupId);
            publicLink.ApprovalStatus = ApprovalStatus.Approved;
            publicLink.IsPublished = true;
            publicLink.PublishDate = null;

            Link draftLink = CreateRandomGroupLink(inputGroupId);
            draftLink.ApprovalStatus = ApprovalStatus.Draft;
            draftLink.IsPublished = false;

            Link futurePublishedLink = CreateRandomGroupLink(inputGroupId);
            futurePublishedLink.ApprovalStatus = ApprovalStatus.Approved;
            futurePublishedLink.IsPublished = true;
            futurePublishedLink.PublishDate = GetRandomDateTimeOffset().AddDays(GetRandomNumber());

            Link deletedLink = CreateRandomGroupLink(inputGroupId);
            deletedLink.IsDeleted = true;

            var storageLinks = new List<Link>
            {
                publicLink,
                draftLink,
                futurePublishedLink,
                deletedLink
            };

            var expectedLinks = new List<Link>
            {
                publicLink,
                draftLink,
                futurePublishedLink
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectLinksByGroupIdAsync(inputGroupId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageLinks);

            // when
            IReadOnlyList<Link> actualLinks =
                await this.linkService.RetrieveLinksByGroupIdAsync(
                    inputGroupId,
                    TestContext.Current.CancellationToken);

            // then
            actualLinks.Should().BeEquivalentTo(expectedLinks);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectLinksByGroupIdAsync(inputGroupId, It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // Every row a group read hands back belongs to the group, and IsDeleted is pinned rather
        // than drawn — a posture-sensitive test must never depend on the draw.
        private static Link CreateRandomGroupLink(Guid groupId)
        {
            Link link = CreateRandomLink();
            link.GroupId = groupId;
            link.IsDeleted = false;

            return link;
        }
        /// <summary>
        /// THE LINEAGE'S OWN ORDER. This is the half of the paging contract that lives down here:
        /// the exposer turns OData's <c>EnsureStableOrdering</c> off precisely so this ordering
        /// survives, and with it off nothing else supplies one - the storage read carries no
        /// ORDER BY, so without this the route would page in whatever order SQL happened to
        /// return.
        ///
        /// <para>Seeded deliberately OUT of order, and out of Id order too, so a read that
        /// forwarded the storage order or leaned on the entity key would fail rather than pass by
        /// coincidence.</para>
        /// </summary>
        [Theory]
        [MemberData(nameof(ReviewRoles))]
        public async Task ShouldOrderGroupMembersByVersionOnRetrieveByGroupIdAsync(
            string reviewRole)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(reviewRole);
            Guid inputGroupId = Guid.NewGuid();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            List<Link> storageLinks = new[] { 3, 1, 2 }
                .Select(version =>
                {
                    Link link =
                        CreateLinkFiller(randomDateTimeOffset).Create();

                    link.GroupId = inputGroupId;
                    link.Version = version;
                    link.IsDeleted = false;

                    return link;
                })
                .ToList();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectLinksByGroupIdAsync(
                    inputGroupId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageLinks);

            // when
            IReadOnlyList<Link> actualLinks =
                await this.linkService.RetrieveLinksByGroupIdAsync(
                    inputGroupId,
                    TestContext.Current.CancellationToken);

            // then
            actualLinks.Select(link => link.Version)
                .Should().ContainInOrder(1, 2, 3);
        }

    }
}
