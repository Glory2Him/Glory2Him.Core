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
using Force.DeepCloner;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Processings;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Processings.Links
{
    public partial class LinkProcessingServiceTests
    {
        [Theory]
        [InlineData(ApprovalStatus.Draft)]
        [InlineData(ApprovalStatus.Submitted)]
        [InlineData(ApprovalStatus.Dismissed)]
        public async Task ShouldModifyLinkInPlaceOnModifyIfActorIsOwnerAsync(
            ApprovalStatus approvalStatus)
        {
            // given: the owner edits a non-terminal link — same row, same version (design
            // §3.4 rules 4-5); only the permitted fields are mapped onto the entity loaded
            // from storage and CreatedBy never changes. Approved and Rejected are absent:
            // both are terminal and fork instead
            Link randomLink = CreateRandomLink();
            Link inputLink = randomLink;
            string actorUserId = GetRandomString();

            Link storageLink = CreateRandomStorageLink(
                linkId: inputLink.Id,
                approvalStatus: approvalStatus,
                createdBy: actorUserId);

            SetupGroupTipRead(storageLink);

            Link expectedMappedLink = storageLink.DeepClone();
            expectedMappedLink.Name = inputLink.Name;
            expectedMappedLink.Url = inputLink.Url;
            expectedMappedLink.LinkType = inputLink.LinkType;
            Link updatedLink = expectedMappedLink.DeepClone();
            Link expectedLink = updatedLink.DeepClone();

            SecurityContext securityContext = CreateAuthenticatedSecurityContext();

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: inputLink,
                securityContext: securityContext);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(inputLink))
                    .ReturnsAsync(inboundEnvelope);

            this.linkServiceMock.Setup(service =>
                service.RetrieveLinkByIdAsync(inputLink.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageLink);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(securityContext))
                    .ReturnsAsync(actorUserId);

            Link? capturedLink = null;

            this.linkServiceMock.Setup(service =>
                service.ModifyLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
                    .Callback<Link, CancellationToken>((link, cancellationToken) =>
                        capturedLink = link)
                    .ReturnsAsync(updatedLink);

            EventEnvelope<Link> outboundEnvelope = SetupCompletionFactPublish(
                inboundEnvelope: inboundEnvelope,
                resultLink: updatedLink,
                operation: LinkProcessingEventOperation.Modified);

            // when
            Link actualLink =
                await this.linkProcessingService.ModifyLinkAsync(
                    inputLink,
                    TestContext.Current.CancellationToken);

            // then
            actualLink.Should().BeEquivalentTo(expectedLink);
            capturedLink.Should().BeEquivalentTo(expectedMappedLink);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(inputLink),
                Times.Once);

            this.linkServiceMock.Verify(service =>
                service.RetrieveLinkByIdAsync(inputLink.Id, It.IsAny<CancellationToken>()),
                Times.Once);

            // even an in-place edit has to establish that this row is the tip, and the tip is
            // a fact about the group rather than a flag on the row
            this.linkServiceMock.Verify(service =>
                service.RetrieveAllLinksAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(securityContext),
                Times.Once);

            this.linkServiceMock.Verify(service =>
                service.ModifyLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()),
                Times.Once);

            this.linkServiceMock.Verify(service =>
                service.AddLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateNextAsync(inboundEnvelope, updatedLink),
                Times.Once);

            VerifyCompletionFactPublished(
                outboundEnvelope: outboundEnvelope,
                operation: LinkProcessingEventOperation.Modified);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.linkServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldNotCarryPublishDateOnModifyInPlaceAsync()
        {
            // given: PublishDate is an IApproval member, so under §9.7.1 rule 2's
            // subtraction rule it is not content and the general modify must never carry
            // it — it belongs solely to the approve operation, which owns ApprovalStatus,
            // IsPublished and PublishDate as one unit. A caller who could set it through
            // modify would schedule their own publication without ever meeting that gate.
            Link inputLink = CreateRandomLink();
            string actorUserId = GetRandomString();

            Link storageLink = CreateRandomStorageLink(
                linkId: inputLink.Id,
                approvalStatus: ApprovalStatus.Draft,
                createdBy: actorUserId);

            SetupGroupTipRead(storageLink);

            DateTimeOffset storedPublishDate = GetRandomDateTimeOffset();
            storageLink.PublishDate = storedPublishDate;
            inputLink.PublishDate = storedPublishDate.AddDays(GetRandomNumber());

            SecurityContext securityContext = CreateAuthenticatedSecurityContext();

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: inputLink,
                securityContext: securityContext);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(inputLink))
                    .ReturnsAsync(inboundEnvelope);

            this.linkServiceMock.Setup(service =>
                service.RetrieveLinkByIdAsync(inputLink.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageLink);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(securityContext))
                    .ReturnsAsync(actorUserId);

            Link? capturedLink = null;

            this.linkServiceMock.Setup(service =>
                service.ModifyLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
                    .Callback<Link, CancellationToken>((link, cancellationToken) =>
                        capturedLink = link)
                    .ReturnsAsync(storageLink);

            // when
            await this.linkProcessingService.ModifyLinkAsync(
                inputLink,
                TestContext.Current.CancellationToken);

            // then: the mapped row keeps storage's publish date, not the caller's
            capturedLink!.PublishDate.Should().Be(storedPublishDate);
        }

        [Fact]
        public async Task ShouldNotCarryCallerControlFieldsOnModifyInPlaceAsync()
        {
            // given: on every update the service loads the current entity and maps only the
            // permitted caller fields onto it, so a caller cannot promote themselves through
            // the update path — approval state, publication, versioning and ownership all
            // stay exactly as storage had them
            Link inputLink = CreateRandomLink();
            string actorUserId = GetRandomString();

            Link storageLink = CreateRandomStorageLink(
                linkId: inputLink.Id,
                approvalStatus: ApprovalStatus.Draft,
                createdBy: actorUserId);

            SetupGroupTipRead(storageLink);

            inputLink.GroupId = Guid.NewGuid();
            inputLink.Version = storageLink.Version + GetRandomNumber();
            inputLink.IsPublished = true;
            inputLink.ApprovalStatus = ApprovalStatus.Approved;
            inputLink.CreatedBy = GetRandomString();
            inputLink.IsDeleted = true;

            SecurityContext securityContext = CreateAuthenticatedSecurityContext();

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: inputLink,
                securityContext: securityContext);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(inputLink))
                    .ReturnsAsync(inboundEnvelope);

            this.linkServiceMock.Setup(service =>
                service.RetrieveLinkByIdAsync(inputLink.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageLink);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(securityContext))
                    .ReturnsAsync(actorUserId);

            Guid storedGroupId = storageLink.GroupId;
            int storedVersion = storageLink.Version;
            bool storedIsPublished = storageLink.IsPublished;
            Link? capturedLink = null;

            this.linkServiceMock.Setup(service =>
                service.ModifyLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
                    .Callback<Link, CancellationToken>((link, cancellationToken) =>
                        capturedLink = link)
                    .ReturnsAsync(storageLink);

            // when
            await this.linkProcessingService.ModifyLinkAsync(
                inputLink,
                TestContext.Current.CancellationToken);

            // then
            capturedLink!.GroupId.Should().Be(storedGroupId);
            capturedLink.Version.Should().Be(storedVersion);
            capturedLink.IsPublished.Should().Be(storedIsPublished);
            capturedLink.ApprovalStatus.Should().Be(ApprovalStatus.Draft);
            capturedLink.CreatedBy.Should().Be(actorUserId);
            capturedLink.IsDeleted.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(TerminalApprovalStatuses))]
        public async Task ShouldForkNewVersionOnModifyIfTerminalLinkIsModifiedByOwnerAsync(
            ApprovalStatus terminalApprovalStatus)
        {
            // given: a terminal link is immutable in place, even to its owner — the edit
            // inserts a new row at Version + 1, and that single insert is the whole fork.
            // The new row becomes the tip by being the highest version in the group, not by
            // being told it is one: there is no demotion write, so there is no window in
            // which the group has no tip at all (#265). The new version starts unpublished
            // in Draft (design §3.4 rules 7-12, rule 16). Rejected forks for the same reason
            // Approved does: the row records a decision, and editing it in place would
            // rewrite what was decided.
            Link randomLink = CreateRandomLink();
            Link inputLink = randomLink;
            string actorUserId = GetRandomString();
            Guid newVersionLinkId = Guid.NewGuid();

            Link storageLink = CreateRandomStorageLink(
                linkId: inputLink.Id,
                approvalStatus: terminalApprovalStatus,
                createdBy: actorUserId);

            SetupGroupTipRead(storageLink);

            var expectedNewVersionLink = new Link
            {
                Id = newVersionLinkId,
                Name = inputLink.Name,
                Url = inputLink.Url,
                LinkType = inputLink.LinkType,

                // the fork is still the modify operation, so the caller's publish date does
                // not ride in on it — a fresh draft has none until approve grants one
                PublishDate = null,
                GroupId = storageLink.GroupId,
                Version = storageLink.Version + 1,
                IsPublished = false,
                ApprovalStatus = ApprovalStatus.Draft,
                IsDeleted = false
            };

            Link addedLink = expectedNewVersionLink.DeepClone();
            Link expectedLink = addedLink.DeepClone();
            SecurityContext securityContext = CreateAuthenticatedSecurityContext();

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: inputLink,
                securityContext: securityContext);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(inputLink))
                    .ReturnsAsync(inboundEnvelope);

            this.linkServiceMock.Setup(service =>
                service.RetrieveLinkByIdAsync(inputLink.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageLink);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(securityContext))
                    .ReturnsAsync(actorUserId);

            this.identifierBrokerMock.Setup(broker =>
                broker.GetIdentifierAsync())
                    .ReturnsAsync(newVersionLinkId);

            Link? capturedNewVersionLink = null;

            this.linkServiceMock.Setup(service =>
                service.AddLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
                    .Callback<Link, CancellationToken>((link, cancellationToken) =>
                        capturedNewVersionLink = link)
                    .ReturnsAsync(addedLink);

            EventEnvelope<Link> outboundEnvelope = SetupCompletionFactPublish(
                inboundEnvelope: inboundEnvelope,
                resultLink: addedLink,
                operation: LinkProcessingEventOperation.Modified);

            // when
            Link actualLink =
                await this.linkProcessingService.ModifyLinkAsync(
                    inputLink,
                    TestContext.Current.CancellationToken);

            // then
            actualLink.Should().BeEquivalentTo(expectedLink);
            capturedNewVersionLink.Should().BeEquivalentTo(expectedNewVersionLink);

            // the guarantee the old demote-then-insert pair could only approximate: the new
            // row outranks the row it forked from, in the same group, so the tip resolves to
            // it the moment the insert lands — and to the old row if it never does
            capturedNewVersionLink!.GroupId.Should().Be(storageLink.GroupId);
            capturedNewVersionLink.Version.Should().BeGreaterThan(storageLink.Version);

            this.linkServiceMock.Verify(service =>
                service.RetrieveLinkByIdAsync(inputLink.Id, It.IsAny<CancellationToken>()),
                Times.Once);

            // the tip is a question about the group, so the service has to go and ask it
            this.linkServiceMock.Verify(service =>
                service.RetrieveAllLinksAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(securityContext),
                Times.Once);

            this.identifierBrokerMock.Verify(broker =>
                broker.GetIdentifierAsync(),
                Times.Once);

            // ONE write. The fork used to be two, and a second write that failed left the
            // group with no tip at all — the whole of #265.
            this.linkServiceMock.Verify(service =>
                service.AddLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()),
                Times.Once);

            this.linkServiceMock.Verify(service =>
                service.ModifyLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(inputLink),
                Times.Once);

            // the fork announces the amend exactly once
            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateNextAsync(inboundEnvelope, addedLink),
                Times.Once);

            VerifyCompletionFactPublished(
                outboundEnvelope: outboundEnvelope,
                operation: LinkProcessingEventOperation.Modified);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.linkServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(TerminalApprovalStatuses))]
        public async Task ShouldLeavePreviouslyPublishedRowPublishedOnForkAsync(
            ApprovalStatus terminalApprovalStatus)
        {
            // given: the fork writes nothing at all to the row it forked from, so a group
            // that had a published row keeps serving it while the new version moves through
            // review (§3.4 rule 12). An Approved row is normally the published one; a
            // Rejected row never was, so the group simply stays dark.
            Link inputLink = CreateRandomLink();
            string actorUserId = GetRandomString();

            Link storageLink = CreateRandomStorageLink(
                linkId: inputLink.Id,
                approvalStatus: terminalApprovalStatus,
                createdBy: actorUserId);

            SetupGroupTipRead(storageLink);

            storageLink.IsPublished = terminalApprovalStatus == ApprovalStatus.Approved;
            bool storedIsPublished = storageLink.IsPublished;

            SecurityContext securityContext = CreateAuthenticatedSecurityContext();

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: inputLink,
                securityContext: securityContext);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(inputLink))
                    .ReturnsAsync(inboundEnvelope);

            this.linkServiceMock.Setup(service =>
                service.RetrieveLinkByIdAsync(inputLink.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageLink);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(securityContext))
                    .ReturnsAsync(actorUserId);

            this.identifierBrokerMock.Setup(broker =>
                broker.GetIdentifierAsync())
                    .ReturnsAsync(Guid.NewGuid());

            Link? capturedNewVersionLink = null;

            this.linkServiceMock.Setup(service =>
                service.AddLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
                    .Callback<Link, CancellationToken>((link, cancellationToken) =>
                        capturedNewVersionLink = link)
                    .ReturnsAsync(storageLink);

            // when
            await this.linkProcessingService.ModifyLinkAsync(
                inputLink,
                TestContext.Current.CancellationToken);

            // then: the incumbent is left exactly as it was read — still published if it was
            // — and the new one starts dark. No write reaches the old row at all, so the fork
            // has no way to disturb its publication.
            storageLink.IsPublished.Should().Be(storedIsPublished);
            capturedNewVersionLink!.IsPublished.Should().BeFalse();

            // and it is the new row the derived tip now resolves to
            capturedNewVersionLink.GroupId.Should().Be(storageLink.GroupId);
            capturedNewVersionLink.Version.Should().BeGreaterThan(storageLink.Version);

            this.linkServiceMock.Verify(service =>
                service.ModifyLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [InlineData(ApprovalStatus.Draft, Roles.Reviewer)]
        [InlineData(ApprovalStatus.Draft, Roles.LinkReviewer)]
        [InlineData(ApprovalStatus.Submitted, Roles.Publisher)]
        [InlineData(ApprovalStatus.Submitted, Roles.LinkPublisher)]
        [InlineData(ApprovalStatus.Dismissed, Roles.Admin)]
        public async Task ShouldModifyLinkInPlaceOnModifyIfActorHasModifyRoleAsync(
            ApprovalStatus approvalStatus,
            string modifyingRole)
        {
            // given: while a link is not yet decided, a Reviewer, Publisher or Admin
            // (global or Link-scoped) may modify it in place alongside the owner; the link
            // stays on the same row and their identity lands on UpdatedBy downstream. A
            // terminal link is deliberately absent — it belongs to its owner alone.
            Link randomLink = CreateRandomLink();
            Link inputLink = randomLink;

            Link storageLink = CreateRandomStorageLink(
                linkId: inputLink.Id,
                approvalStatus: approvalStatus,
                createdBy: GetRandomString());

            SetupGroupTipRead(storageLink);

            Link updatedLink = storageLink.DeepClone();
            SecurityContext securityContext = CreateAuthenticatedSecurityContext(modifyingRole);

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: inputLink,
                securityContext: securityContext);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(inputLink))
                    .ReturnsAsync(inboundEnvelope);

            this.linkServiceMock.Setup(service =>
                service.RetrieveLinkByIdAsync(inputLink.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageLink);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(securityContext))
                    .ReturnsAsync(GetRandomString());

            this.linkServiceMock.Setup(service =>
                service.ModifyLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(updatedLink);

            // when
            Link actualLink =
                await this.linkProcessingService.ModifyLinkAsync(
                    inputLink,
                    TestContext.Current.CancellationToken);

            // then
            actualLink.Should().NotBeNull();

            this.linkServiceMock.Verify(service =>
                service.ModifyLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()),
                Times.Once);

            this.linkServiceMock.Verify(service =>
                service.AddLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
