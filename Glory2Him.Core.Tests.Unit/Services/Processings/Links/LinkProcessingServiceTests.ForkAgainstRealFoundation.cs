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
using Glory2Him.Core.Brokers.DateTimes;
using Glory2Him.Core.Brokers.EventEnvelopes;
using Glory2Him.Core.Brokers.Events;
using Glory2Him.Core.Brokers.Identifiers;
using Glory2Him.Core.Brokers.Integrities;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Brokers.Storages.Sql;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Processings.Links.Exceptions;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Services.Foundations.Links;
using Glory2Him.Core.Services.Processings.Links;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Processings.Links
{
    /// <summary>
    /// The one test file in this suite that does NOT mock the foundation service.
    ///
    /// <para>Every other processing-service test hands <c>ILinkService</c> a mock, which means a
    /// validation added or tightened in the foundation can refuse a write this service makes and
    /// the whole suite still passes. That is not hypothetical: the version fork used to demote the
    /// previous tip through the general modify, which pins the <c>IVersion</c> members against
    /// storage — on <c>ContentItem</c>, where the pin was actually present, forking an approved
    /// item could not complete at all, and nothing caught it (#263).</para>
    ///
    /// <para>The fork is now a SINGLE INSERT and the tip is derived from <c>Version</c>, so the
    /// two-write hazard behind #265 is gone by construction. This file is what says so out loud:
    /// one test proves the fork lands with one write and no update at all, and the other kills
    /// that write and proves the group is left exactly as it was — previous tip intact, still
    /// resolving as the tip, and still editable. Under the old demote-then-insert fork that same
    /// failure left the group with NO tip and permanently uneditable.</para>
    ///
    /// <para>Both wire the REAL <c>LinkService</c> underneath and mock only the brokers, which is
    /// where the process boundary actually is. Neither touches an external resource.</para>
    /// </summary>
    public partial class LinkProcessingServiceTests
    {
        // The real foundation stacked under the processing service, plus the handles a test
        // needs to see what reached storage. GroupRows is the table the derived tip is read
        // from: an insert that lands is appended to it, and one that fails is not, which is
        // exactly the distinction these tests turn on.
        private sealed class RealFoundationForkHarness
        {
            public required ILinkProcessingService ProcessingService { get; init; }
            public required Mock<IStorageBroker> StorageBrokerMock { get; init; }
            public required Mock<IEventBroker> EventBrokerMock { get; init; }
            public required List<Link> GroupRows { get; init; }
            public required List<Link> InsertedRows { get; init; }
            public required List<Link> UpdatedRows { get; init; }
        }

        // Wires LinkProcessingService -> the REAL LinkService -> mocked brokers. The audit
        // brokers STAMP rather than pass through, because the foundation's own validations read
        // what they wrote and a pass-through mock would fail the write for a reason that has
        // nothing to do with what these tests are about.
        private static RealFoundationForkHarness CreateRealFoundationForkHarness(
            Link storageLink,
            string actorUserId,
            Guid newVersionLinkId,
            DateTimeOffset now)
        {
            var storageBrokerMock = new Mock<IStorageBroker>();
            var dateTimeBrokerMock = new Mock<IDateTimeBroker>();
            var identifierBrokerMock = new Mock<IIdentifierBroker>();
            var eventBrokerMock = new Mock<IEventBroker>();
            var eventEnvelopeBrokerMock = new Mock<IEventEnvelopeBroker>();
            var securityAuditBrokerMock = new Mock<ISecurityAuditBroker>();
            var accessBrokerMock = new Mock<IAccessBroker>();
            var envelopeIntegrityBrokerMock = new Mock<IEnvelopeIntegrityBroker>();
            var loggingBrokerMock = new Mock<ILoggingBroker>();

            var securityContext = new SecurityContext
            {
                IsAuthenticated = true,
                Roles = []
            };

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
                broker.SelectLinkByIdAsync(storageLink.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageLink);

            // The group as storage holds it. The tip is DERIVED from these rows, so this list
            // IS the answer to "which row is the tip" — nothing else records it.
            var groupRows = new List<Link> { storageLink };

            storageBrokerMock.Setup(broker =>
                broker.SelectAllLinksAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(() => groupRows.AsQueryable());

            securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(actorUserId);

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

            var insertedRows = new List<Link>();
            var updatedRows = new List<Link>();

            storageBrokerMock.Setup(broker =>
                broker.UpdateLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
                    .Callback<Link, CancellationToken>(
                        (entity, _) => updatedRows.Add(entity.DeepClone()))
                    .ReturnsAsync((Link entity, CancellationToken _) => entity);

            // an insert that lands JOINS THE GROUP, which is what moves the derived tip
            storageBrokerMock.Setup(broker =>
                broker.InsertLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
                    .Callback<Link, CancellationToken>((entity, _) =>
                    {
                        insertedRows.Add(entity.DeepClone());
                        groupRows.Add(entity.DeepClone());
                    })
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

            return new RealFoundationForkHarness
            {
                ProcessingService = processingService,
                StorageBrokerMock = storageBrokerMock,
                EventBrokerMock = eventBrokerMock,
                GroupRows = groupRows,
                InsertedRows = insertedRows,
                UpdatedRows = updatedRows
            };
        }

        // The row the fork starts from: terminal, owned by the actor, and published if the
        // status is one that would have been. Its Version is pinned above 1 so "the new row
        // outranks it" is a real comparison rather than a comparison with the floor.
        private static Link CreateRealFoundationForkStorageLink(
            Guid linkId,
            ApprovalStatus terminalStatus,
            string actorUserId)
        {
            Link storageLink = CreateRandomStorageLink(
                linkId: linkId,
                approvalStatus: terminalStatus,
                createdBy: actorUserId);

            storageLink.IsPublished = terminalStatus == ApprovalStatus.Approved;
            storageLink.IsDeleted = false;

            return storageLink;
        }

        [Theory]
        [InlineData(ApprovalStatus.Approved)]
        [InlineData(ApprovalStatus.Rejected)]
        public async Task ShouldForkTerminalLinkThroughTheRealFoundationServiceAsync(
            ApprovalStatus terminalStatus)
        {
            // given: the owner amends a terminal link. The fork is ONE write — insert the new
            // version — and it has to survive the foundation's own validations, which is the
            // part a mocked foundation cannot tell us.
            string actorUserId = GetRandomString();
            Guid newVersionLinkId = Guid.NewGuid();
            DateTimeOffset now = GetRandomDateTimeOffset();
            Link inputLink = CreateRandomLink();

            Link storageLink = CreateRealFoundationForkStorageLink(
                linkId: inputLink.Id,
                terminalStatus: terminalStatus,
                actorUserId: actorUserId);

            Link storedRowAsFound = storageLink.DeepClone();

            RealFoundationForkHarness harness = CreateRealFoundationForkHarness(
                storageLink: storageLink,
                actorUserId: actorUserId,
                newVersionLinkId: newVersionLinkId,
                now: now);

            // when
            Link actualLink = await harness.ProcessingService.ModifyLinkAsync(
                inputLink,
                TestContext.Current.CancellationToken);

            // then: the fork completed, on a fresh row one version above the one it forked from
            actualLink.Should().NotBeNull();
            actualLink.Id.Should().Be(newVersionLinkId);
            actualLink.GroupId.Should().Be(storedRowAsFound.GroupId);
            actualLink.Version.Should().Be(storedRowAsFound.Version + 1);
            actualLink.IsPublished.Should().BeFalse();
            actualLink.ApprovalStatus.Should().Be(ApprovalStatus.Draft);

            // ONE row written, and it is an insert. The fork used to demote the old tip first,
            // and that second write is the whole of #265 — there is no longer a write to fail.
            harness.InsertedRows.Should().HaveCount(1);
            harness.UpdatedRows.Should().BeEmpty();

            harness.StorageBrokerMock.Verify(broker =>
                broker.UpdateLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()),
                Times.Never);

            // the previous version is untouched: same publication, same decision. An approved
            // previous version therefore stays publicly visible until the new one is approved
            // (§3.4.1), which no write on this path could have disturbed.
            storageLink.Should().BeEquivalentTo(storedRowAsFound);

            // and the group's tip has MOVED to the new row, on Version alone
            Link groupTip = await harness.ProcessingService.RetrieveLatestLinkByGroupIdAsync(
                storedRowAsFound.GroupId,
                TestContext.Current.CancellationToken);

            groupTip.Id.Should().Be(newVersionLinkId);

            // one Added fact for the inserted row, and never a Modified one: the old row was
            // not amended, so announcing an amendment would misdescribe what happened
            harness.EventBrokerMock.Verify(broker =>
                    broker.PublishLinkAsync(
                        It.IsAny<EventEnvelope<Link>>(),
                        LinkEventOperation.Added),
                Times.Once);

            harness.EventBrokerMock.Verify(broker =>
                    broker.PublishLinkAsync(
                        It.IsAny<EventEnvelope<Link>>(),
                        LinkEventOperation.Modified),
                Times.Never);
        }

        [Theory]
        [InlineData(ApprovalStatus.Approved)]
        [InlineData(ApprovalStatus.Rejected)]
        public async Task ShouldLeavePreviousTipIntactAndEditableIfForkInsertFailsAsync(
            ApprovalStatus terminalStatus)
        {
            // given: the fork's single write dies at storage. This is #265's exact scenario —
            // under the old demote-then-insert fork the demotion had already landed by now, so
            // the group was left with no row claiming to be the tip and no operation able to
            // create one: the whole version chain became permanently uneditable. Derived from
            // Version, that state cannot be represented, and this test is what says so.
            string actorUserId = GetRandomString();
            Guid newVersionLinkId = Guid.NewGuid();
            DateTimeOffset now = GetRandomDateTimeOffset();
            Link inputLink = CreateRandomLink();

            Link storageLink = CreateRealFoundationForkStorageLink(
                linkId: inputLink.Id,
                terminalStatus: terminalStatus,
                actorUserId: actorUserId);

            Link storedRowAsFound = storageLink.DeepClone();

            RealFoundationForkHarness harness = CreateRealFoundationForkHarness(
                storageLink: storageLink,
                actorUserId: actorUserId,
                newVersionLinkId: newVersionLinkId,
                now: now);

            var storageFailureException = new Exception(message: GetRandomString());
            bool shouldInsertFail = true;

            harness.StorageBrokerMock.Setup(broker =>
                broker.InsertLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
                    .Returns((Link entity, CancellationToken _) =>
                    {
                        if (shouldInsertFail)
                        {
                            throw storageFailureException;
                        }

                        harness.InsertedRows.Add(entity.DeepClone());
                        harness.GroupRows.Add(entity.DeepClone());

                        return new ValueTask<Link>(entity);
                    });

            // when
            ValueTask<Link> forkTask = harness.ProcessingService.ModifyLinkAsync(
                inputLink,
                TestContext.Current.CancellationToken);

            // the storage failure surfaces through the foundation as a dependency error, which
            // is beside the point here — what matters is the state it left behind
            await Assert.ThrowsAsync<LinkProcessingDependencyException>(forkTask.AsTask);

            // then: nothing landed. Not the insert, and — the point — no compensating write
            // either, because the fork never had a first write to compensate for.
            harness.InsertedRows.Should().BeEmpty();
            harness.UpdatedRows.Should().BeEmpty();

            harness.StorageBrokerMock.Verify(broker =>
                broker.UpdateLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()),
                Times.Never);

            // the previous tip is byte-for-byte as the fork found it
            storageLink.Should().BeEquivalentTo(storedRowAsFound);
            harness.GroupRows.Should().ContainSingle().Which.Id.Should().Be(storedRowAsFound.Id);

            // and it is STILL the group's tip — derived from the rows that exist, so a write
            // that never happened cannot have moved it
            Link groupTip = await harness.ProcessingService.RetrieveLatestLinkByGroupIdAsync(
                storedRowAsFound.GroupId,
                TestContext.Current.CancellationToken);

            groupTip.Id.Should().Be(storedRowAsFound.Id);
            groupTip.Version.Should().Be(storedRowAsFound.Version);

            // STILL EDITABLE, which is the guarantee #265 broke: the owner retries the same
            // amendment against the same row, storage is healthy this time, and the fork lands.
            // Under the old fork this retry was refused — the row was no longer the latest
            // version, and no row in the group was.
            shouldInsertFail = false;

            Link retriedLink = await harness.ProcessingService.ModifyLinkAsync(
                inputLink,
                TestContext.Current.CancellationToken);

            retriedLink.Id.Should().Be(newVersionLinkId);
            retriedLink.GroupId.Should().Be(storedRowAsFound.GroupId);
            retriedLink.Version.Should().Be(storedRowAsFound.Version + 1);
            retriedLink.ApprovalStatus.Should().Be(ApprovalStatus.Draft);
            harness.InsertedRows.Should().HaveCount(1);
            harness.UpdatedRows.Should().BeEmpty();

            // and the tip has moved to the row that actually landed
            Link retriedGroupTip = await harness.ProcessingService.RetrieveLatestLinkByGroupIdAsync(
                storedRowAsFound.GroupId,
                TestContext.Current.CancellationToken);

            retriedGroupTip.Id.Should().Be(newVersionLinkId);
        }
    }
}
