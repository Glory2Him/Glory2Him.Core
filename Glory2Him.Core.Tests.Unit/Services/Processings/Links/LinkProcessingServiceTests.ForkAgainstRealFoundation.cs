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
using Glory2Him.Core.Brokers.DateTimes;
using Glory2Him.Core.Brokers.EventEnvelopes;
using Glory2Him.Core.Brokers.Events;
using Glory2Him.Core.Brokers.Identifiers;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Brokers.Integrities;
using Glory2Him.Core.Brokers.Storages.Sql;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Services.Foundations.Links;
using Glory2Him.Core.Services.Processings.Links;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Processings.Links
{
    /// <summary>
    /// The one test in this suite that does NOT mock the foundation service.
    ///
    /// <para>Every other processing-service test hands <c>ILinkService</c> a mock, which means a
    /// validation added or tightened in the foundation can refuse a write this service makes and
    /// the whole suite still passes. That is not hypothetical: the version fork demoted the
    /// previous latest through the general modify, which pins <c>IsLatestVersion</c> against
    /// storage — on <c>ContentItem</c>, where the pin was actually present, forking an approved
    /// item could not complete at all, and nothing caught it (#263).</para>
    ///
    /// <para>So this one wires the REAL <c>LinkService</c> underneath and mocks only the brokers,
    /// which is where the process boundary actually is. It touches no external resource.</para>
    /// </summary>
    public partial class LinkProcessingServiceTests
    {
        [Theory]
        [InlineData(ApprovalStatus.Approved)]
        [InlineData(ApprovalStatus.Rejected)]
        public async Task ShouldForkTerminalLinkThroughTheRealFoundationServiceAsync(
            ApprovalStatus terminalStatus)
        {
            // given: the owner amends a terminal link. The fork must demote the current tip and
            // insert the new version, and BOTH writes have to survive the foundation's own
            // validations — which is the part a mocked foundation cannot tell us.
            var storageBrokerMock = new Mock<IStorageBroker>();
            var dateTimeBrokerMock = new Mock<IDateTimeBroker>();
            var identifierBrokerMock = new Mock<IIdentifierBroker>();
            var eventBrokerMock = new Mock<IEventBroker>();
            var eventEnvelopeBrokerMock = new Mock<IEventEnvelopeBroker>();
            var securityAuditBrokerMock = new Mock<ISecurityAuditBroker>();
            var accessBrokerMock = new Mock<IAccessBroker>();
            var envelopeIntegrityBrokerMock = new Mock<IEnvelopeIntegrityBroker>();
            var loggingBrokerMock = new Mock<ILoggingBroker>();

            string actorUserId = GetRandomString();
            Guid newVersionLinkId = Guid.NewGuid();
            DateTimeOffset now = GetRandomDateTimeOffset();

            var securityContext = new SecurityContext
            {
                IsAuthenticated = true,
                Roles = []
            };

            Link inputLink = CreateRandomLink();

            Link storageLink = CreateRandomStorageLink(
                linkId: inputLink.Id,
                approvalStatus: terminalStatus,
                createdBy: actorUserId);

            storageLink.IsLatestVersion = true;
            storageLink.IsPublished = terminalStatus == ApprovalStatus.Approved;
            storageLink.IsDeleted = false;

            Link storedRowAsFound = storageLink.DeepClone();

            eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.IsAny<Link>()))
                    .Returns((Link content) =>
                        new ValueTask<EventEnvelope<Link>>(
                            new EventEnvelope<Link>
                            {
                                Content = content,
                                SecurityContext = securityContext,
                                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
                            }));

            eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateNextAsync(
                    It.IsAny<EventEnvelope<Link>>(),
                    It.IsAny<Link>()))
                        .Returns((EventEnvelope<Link> source, Link content) =>
                            new ValueTask<EventEnvelope<Link>>(
                                new EventEnvelope<Link>
                                {
                                    Content = content,
                                    SecurityContext = source.SecurityContext,
                                    Metadata = new EventMetadata { EventId = Guid.NewGuid() }
                                }));

            storageBrokerMock.Setup(broker =>
                broker.SelectLinkByIdAsync(inputLink.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageLink);

            securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(actorUserId);

            // the audit brokers STAMP rather than pass through — the foundation's own validations
            // read what they wrote, so a pass-through mock would fail the write for a reason
            // that has nothing to do with what this test is about
            securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<Link>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync((Link entity, SecurityContext _) =>
                        {
                            entity.UpdatedBy = actorUserId;
                            entity.UpdatedWhen = now;

                            return entity;
                        });

            securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(
                    It.IsAny<Link>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync((Link entity, SecurityContext _) =>
                        {
                            entity.CreatedBy = actorUserId;
                            entity.UpdatedBy = actorUserId;
                            entity.CreatedWhen = now;
                            entity.UpdatedWhen = now;

                            return entity;
                        });

            securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    It.IsAny<Link>(),
                    It.IsAny<Link>()))
                        .ReturnsAsync((Link entity, Link _) => entity);

            dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(now);

            identifierBrokerMock.Setup(broker =>
                broker.GetIdentifierAsync())
                    .ReturnsAsync(newVersionLinkId);

            var writtenRows = new List<Link>();

            storageBrokerMock.Setup(broker =>
                broker.UpdateLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
                    .Callback<Link, CancellationToken>(
                        (entity, _) => writtenRows.Add(entity.DeepClone()))
                    .ReturnsAsync((Link entity, CancellationToken _) => entity);

            storageBrokerMock.Setup(broker =>
                broker.InsertLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
                    .Callback<Link, CancellationToken>(
                        (entity, _) => writtenRows.Add(entity.DeepClone()))
                    .ReturnsAsync((Link entity, CancellationToken _) => entity);

            eventBrokerMock.Setup(broker =>
                broker.PublishLinkAsync(
                    It.IsAny<EventEnvelope<Link>>(),
                    It.IsAny<LinkEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<Link>>(
                            new EventPublishResult<Link>()));

            var realLinkService = new LinkService(
                storageBroker: storageBrokerMock.Object,
                dateTimeBroker: dateTimeBrokerMock.Object,
                identifierBroker: identifierBrokerMock.Object,
                eventBroker: eventBrokerMock.Object,
                eventEnvelopeBroker: eventEnvelopeBrokerMock.Object,
                securityAuditBroker: securityAuditBrokerMock.Object,
                accessBroker: accessBrokerMock.Object,
                envelopeIntegrityBroker: envelopeIntegrityBrokerMock.Object,
                loggingBroker: loggingBrokerMock.Object);

            var processingService = new LinkProcessingService(
                linkService: realLinkService,
                dateTimeBroker: dateTimeBrokerMock.Object,
                identifierBroker: identifierBrokerMock.Object,
                eventEnvelopeBroker: eventEnvelopeBrokerMock.Object,
                eventBroker: eventBrokerMock.Object,
                securityAuditBroker: securityAuditBrokerMock.Object,
                envelopeIntegrityBroker: envelopeIntegrityBrokerMock.Object,
                loggingBroker: loggingBrokerMock.Object);

            // when
            Link actualLink = await processingService.ModifyLinkAsync(
                inputLink,
                TestContext.Current.CancellationToken);

            // then: the fork completed. Before the demote verb existed this threw, because the
            // demotion went through the general modify and the modify refuses an IVersion write.
            actualLink.Should().NotBeNull();
            actualLink.Id.Should().Be(newVersionLinkId);
            actualLink.Version.Should().Be(storedRowAsFound.Version + 1);
            actualLink.IsLatestVersion.Should().BeTrue();
            actualLink.IsPublished.Should().BeFalse();
            actualLink.ApprovalStatus.Should().Be(ApprovalStatus.Draft);

            // two rows written: the demoted tip, then the new version
            writtenRows.Should().HaveCount(2);
            writtenRows[0].Id.Should().Be(storedRowAsFound.Id);
            writtenRows[0].IsLatestVersion.Should().BeFalse();

            // the demotion leaves publication alone, so an approved previous version stays
            // publicly visible until the new one is approved (§3.4.1)
            writtenRows[0].IsPublished.Should().Be(storedRowAsFound.IsPublished);
            writtenRows[0].ApprovalStatus.Should().Be(terminalStatus);

            // the demotion announces itself as a demotion, never as an amendment
            eventBrokerMock.Verify(broker =>
                    broker.PublishLinkAsync(
                        It.IsAny<EventEnvelope<Link>>(),
                        LinkEventOperation.Demoted),
                Times.Once);

            eventBrokerMock.Verify(broker =>
                    broker.PublishLinkAsync(
                        It.IsAny<EventEnvelope<Link>>(),
                        LinkEventOperation.Modified),
                Times.Never);
        }
    }
}
