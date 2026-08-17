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
            // forks a new row with Version + 1 that becomes the latest, the previous latest
            // is demoted BEFORE the insert (one IsLatestVersion = true per group), and the
            // new version starts unpublished in Draft (design §3.4 rules 7-12, rule 16).
            // Rejected forks for the same reason Approved does: the row records a decision,
            // and editing it in place would rewrite what was decided.
            Link randomLink = CreateRandomLink();
            Link inputLink = randomLink;
            string actorUserId = GetRandomString();
            Guid newVersionLinkId = Guid.NewGuid();

            Link storageLink = CreateRandomStorageLink(
                linkId: inputLink.Id,
                approvalStatus: terminalApprovalStatus,
                createdBy: actorUserId);

            Link expectedDemotedLink = storageLink.DeepClone();
            expectedDemotedLink.IsLatestVersion = false;

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
                IsLatestVersion = true,
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

            var callOrder = new List<string>();
            Link? capturedDemotedLink = null;
            Link? capturedNewVersionLink = null;

            this.linkServiceMock.Setup(service =>
                service.ModifyLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
                    .Callback<Link, CancellationToken>((link, cancellationToken) =>
                    {
                        callOrder.Add("demote");
                        capturedDemotedLink = link;
                    })
                    .ReturnsAsync(expectedDemotedLink);

            this.linkServiceMock.Setup(service =>
                service.AddLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
                    .Callback<Link, CancellationToken>((link, cancellationToken) =>
                    {
                        callOrder.Add("add");
                        capturedNewVersionLink = link;
                    })
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
            capturedDemotedLink.Should().BeEquivalentTo(expectedDemotedLink);
            capturedNewVersionLink.Should().BeEquivalentTo(expectedNewVersionLink);
            callOrder.Should().Equal("demote", "add");

            this.linkServiceMock.Verify(service =>
                service.RetrieveLinkByIdAsync(inputLink.Id, It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(securityContext),
                Times.Once);

            this.identifierBrokerMock.Verify(broker =>
                broker.GetIdentifierAsync(),
                Times.Once);

            this.linkServiceMock.Verify(service =>
                service.ModifyLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()),
                Times.Once);

            this.linkServiceMock.Verify(service =>
                service.AddLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(inputLink),
                Times.Once);

            // the fork writes two foundation rows but announces the amend exactly once
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
            // given: the fork demotes the previous latest but leaves IsPublished alone
            // (§3.4 rule 12), so a group that had a published row keeps serving it while
            // the new version moves through review. An Approved row is normally the
            // published one; a Rejected row never was, so the group simply stays dark.
            Link inputLink = CreateRandomLink();
            string actorUserId = GetRandomString();

            Link storageLink = CreateRandomStorageLink(
                linkId: inputLink.Id,
                approvalStatus: terminalApprovalStatus,
                createdBy: actorUserId);

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

            Link? capturedDemotedLink = null;
            Link? capturedNewVersionLink = null;

            this.linkServiceMock.Setup(service =>
                service.ModifyLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
                    .Callback<Link, CancellationToken>((link, cancellationToken) =>
                        capturedDemotedLink = link)
                    .ReturnsAsync(storageLink);

            this.linkServiceMock.Setup(service =>
                service.AddLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
                    .Callback<Link, CancellationToken>((link, cancellationToken) =>
                        capturedNewVersionLink = link)
                    .ReturnsAsync(storageLink);

            // when
            await this.linkProcessingService.ModifyLinkAsync(
                inputLink,
                TestContext.Current.CancellationToken);

            // then
            capturedDemotedLink!.IsLatestVersion.Should().BeFalse();
            capturedDemotedLink.IsPublished.Should().Be(storedIsPublished);
            capturedNewVersionLink!.IsLatestVersion.Should().BeTrue();
            capturedNewVersionLink.IsPublished.Should().BeFalse();
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
