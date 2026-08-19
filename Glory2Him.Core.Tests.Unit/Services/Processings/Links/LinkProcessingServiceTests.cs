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
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using Glory2Him.Core.Brokers.DateTimes;
using Glory2Him.Core.Brokers.EventEnvelopes;
using Glory2Him.Core.Brokers.Events;
using Glory2Him.Core.Brokers.Identifiers;
using Glory2Him.Core.Brokers.Integrities;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Processings;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.Links.Exceptions;
using Glory2Him.Core.Services.Foundations.Links;
using Glory2Him.Core.Services.Processings.Links;
using Moq;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Processings.Links
{
    public partial class LinkProcessingServiceTests
    {
        private readonly Mock<ILinkService> linkServiceMock;
        private readonly Mock<IDateTimeBroker> dateTimeBrokerMock;
        private readonly Mock<IIdentifierBroker> identifierBrokerMock;
        private readonly Mock<IEventEnvelopeBroker> eventEnvelopeBrokerMock;
        private readonly Mock<IEventBroker> eventBrokerMock;
        private readonly Mock<ISecurityAuditBroker> securityAuditBrokerMock;
        private readonly Mock<IEnvelopeIntegrityBroker> envelopeIntegrityBrokerMock;
        private readonly Mock<ILoggingBroker> loggingBrokerMock;
        private readonly ILinkProcessingService linkProcessingService;

        public LinkProcessingServiceTests()
        {
            this.linkServiceMock = new Mock<ILinkService>();
            this.dateTimeBrokerMock = new Mock<IDateTimeBroker>();
            this.identifierBrokerMock = new Mock<IIdentifierBroker>();
            this.eventEnvelopeBrokerMock = new Mock<IEventEnvelopeBroker>();
            this.eventBrokerMock = new Mock<IEventBroker>();
            this.securityAuditBrokerMock = new Mock<ISecurityAuditBroker>();
            this.envelopeIntegrityBrokerMock = new Mock<IEnvelopeIntegrityBroker>();
            this.loggingBrokerMock = new Mock<ILoggingBroker>();

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    It.IsAny<EventEnvelope<Link>>(),
                    It.IsAny<string>(),
                    It.IsAny<EnvelopeDirection>()))
                        .ReturnsAsync(true);

            this.linkProcessingService = new LinkProcessingService(
                linkService: this.linkServiceMock.Object,
                dateTimeBroker: this.dateTimeBrokerMock.Object,
                identifierBroker: this.identifierBrokerMock.Object,
                eventEnvelopeBroker: this.eventEnvelopeBrokerMock.Object,
                eventBroker: this.eventBrokerMock.Object,
                securityAuditBroker: this.securityAuditBrokerMock.Object,
                envelopeIntegrityBroker: this.envelopeIntegrityBrokerMock.Object,
                loggingBroker: this.loggingBrokerMock.Object);
        }

        public static TheoryData<Xeption> DependencyValidationExceptions()
        {
            string randomMessage = GetRandomString();
            var innerException = new Xeption(message: randomMessage);

            return new TheoryData<Xeption>
            {
                new LinkValidationException(
                    message: randomMessage,
                    innerException: innerException),

                new LinkDependencyValidationException(
                    message: randomMessage,
                    innerException: innerException)
            };
        }

        public static TheoryData<Xeption> DependencyExceptions()
        {
            string randomMessage = GetRandomString();
            var innerException = new Xeption(message: randomMessage);

            return new TheoryData<Xeption>
            {
                new LinkDependencyException(
                    message: randomMessage,
                    innerException: innerException),

                new LinkServiceException(
                    message: randomMessage,
                    innerException: innerException)
            };
        }

        // the two statuses a modify may not amend in place — an edit of either forks a new
        // version instead (design §3.4 rules 7-8, rule 16)
        public static TheoryData<ApprovalStatus> TerminalApprovalStatuses() =>
            new TheoryData<ApprovalStatus>
            {
                ApprovalStatus.Approved,
                ApprovalStatus.Rejected
            };

        private static Expression<Func<Xeption, bool>> SameExceptionAs(Xeption expectedException) =>
            actualException => actualException.SameExceptionAs(expectedException);

        // every completed do-work publishes the processing service's own fact on the next
        // envelope in the causation chain; the helpers keep that setup out of each test
        private EventEnvelope<Link> SetupCompletionFactPublish(
            EventEnvelope<Link> inboundEnvelope,
            Link resultLink,
            LinkProcessingEventOperation operation)
        {
            var outboundEnvelope = new EventEnvelope<Link>
            {
                Content = resultLink,
                SecurityContext = inboundEnvelope.SecurityContext,

                Metadata = new EventMetadata
                {
                    EventId = Guid.NewGuid(),
                    CausationId = inboundEnvelope.Metadata.EventId.ToString()
                }
            };

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateNextAsync(inboundEnvelope, resultLink))
                    .ReturnsAsync(outboundEnvelope);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishLinkProcessingAsync(outboundEnvelope, operation))
                    .ReturnsAsync(new EventPublishResult<Link>());

            return outboundEnvelope;
        }

        private void VerifyCompletionFactPublished(
            EventEnvelope<Link> outboundEnvelope,
            LinkProcessingEventOperation operation)
        {
            this.eventBrokerMock.Verify(broker =>
                broker.PublishLinkProcessingAsync(outboundEnvelope, operation),
                Times.Once);

            this.eventBrokerMock.VerifyNoOtherCalls();
        }

        // the remove request payload is minted inside the service, so it is matched on the
        // instruction it carries: the id of the row to remove and the optional reason
        private static Expression<Func<Link, bool>> SameRemoveRequestAs(
            Guid expectedLinkId,
            string? expectedDeletionReason) =>
            actualLink => actualLink.Id == expectedLinkId
                && actualLink.DeletionReason == expectedDeletionReason;

        // the retrieve request payload carries only the id of the version to read
        private static Expression<Func<Link, bool>> SameRetrieveRequestAs(
            Guid expectedLinkId) =>
            actualLink => actualLink.Id == expectedLinkId;

        // a group read's request payload carries only the group whose versions to read
        private static Expression<Func<Link, bool>> SameGroupRetrieveRequestAs(
            Guid expectedGroupId) =>
            actualLink => actualLink.GroupId == expectedGroupId;

        // an unfiltered collection read carries no instruction at all — the request payload
        // is an empty link minted only to capture the ambient security context
        private static Expression<Func<Link, bool>> SameRetrieveAllRequest() =>
            actualLink => actualLink.Id == Guid.Empty
                && actualLink.GroupId == Guid.Empty;

        private static string GetRandomString() =>
            new MnemonicString(wordCount: GetRandomNumber()).GetValue();

        private static int GetRandomNumber() =>
            new IntRange(min: 2, max: 10).GetValue();

        private static DateTimeOffset GetRandomDateTimeOffset() =>
            new DateTimeRange(earliestDate: new DateTime()).GetValue();

        // a deserialized request envelope can carry a null SecurityContext, so the gate
        // must treat null and not-authenticated identically
        public static TheoryData<SecurityContext?> UnauthenticatedSecurityContexts() =>
            new TheoryData<SecurityContext?>
            {
                null,
                new SecurityContext { IsAuthenticated = false }
            };

        private static SecurityContext CreateAuthenticatedSecurityContext(params string[] roles) =>
            new SecurityContext
            {
                IsAuthenticated = true,
                Roles = roles
            };

        private static EventEnvelope<Link> CreateEventEnvelope(
            Link link,
            SecurityContext securityContext) =>
            new EventEnvelope<Link>
            {
                Content = link,
                SecurityContext = securityContext,
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

        private static IQueryable<Link> CreateRandomLinks() =>
            CreateLinkFiller(dateTimeOffset: GetRandomDateTimeOffset())
                .Create(count: GetRandomNumber())
                .AsQueryable();

        private static Link CreateRandomLink() =>
            CreateLinkFiller(dateTimeOffset: GetRandomDateTimeOffset()).Create();

        // Where the "current" row sits in its version chain. Pinned above 1 so a superseded
        // row can be expressed by seeding a live sibling above it, and so a service that
        // simply assumed version 1 could not satisfy an assertion by coincidence.
        private const int StorageLinkVersion = 4;

        // the "current" row the modify flow loads from storage: the modifiable tip of its
        // group. It is the tip because nothing in the group outranks its Version — there is
        // no flag saying so, so SetupGroupTipRead below is what makes the claim true.
        private static Link CreateRandomStorageLink(
            Guid linkId,
            ApprovalStatus approvalStatus,
            string createdBy)
        {
            Link storageLink = CreateRandomLink();
            storageLink.Id = linkId;
            storageLink.ApprovalStatus = approvalStatus;
            storageLink.CreatedBy = createdBy;
            storageLink.Version = StorageLinkVersion;
            storageLink.IsDeleted = false;

            return storageLink;
        }

        // The tip is DERIVED — the highest Version among the group's live rows — so the modify
        // flow asks the question of the whole table through RetrieveAllLinksAsync. A test that
        // wants its storage row treated as the tip has to let that read see the group, and one
        // that wants it superseded seeds a higher-versioned sibling here rather than clearing a
        // flag that no longer exists.
        private void SetupGroupTipRead(params Link[] groupLinks) =>
            this.linkServiceMock.Setup(service =>
                service.RetrieveAllLinksAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(groupLinks.AsQueryable());

        // A live row in the same group that outranks the given one, which is exactly what
        // makes that one no longer the tip.
        private static Link CreateSupersedingLink(Link storageLink)
        {
            Link supersedingLink = CreateRandomLink();
            supersedingLink.GroupId = storageLink.GroupId;
            supersedingLink.Version = storageLink.Version + 1;
            supersedingLink.IsDeleted = false;

            return supersedingLink;
        }

        // a row that satisfies canonical content visibility (§14.1) as of currentDateTime
        private static Link CreateRandomPubliclyVisibleLink(
            Guid linkId,
            DateTimeOffset currentDateTime,
            bool hasPublishDate)
        {
            Link storageLink = CreateRandomLink();
            storageLink.Id = linkId;
            storageLink.ApprovalStatus = ApprovalStatus.Approved;
            storageLink.IsPublished = true;
            storageLink.IsDeleted = false;

            storageLink.PublishDate = hasPublishDate
                ? currentDateTime.AddDays(-1)
                : null;

            return storageLink;
        }

        // a row that misses canonical visibility (§14.1): unpublished and not approved —
        // visible only to its owner and the review roles on collection reads
        private static Link CreateRandomNonPublicLink(string createdBy)
        {
            Link storageLink = CreateRandomLink();
            storageLink.ApprovalStatus = ApprovalStatus.Draft;
            storageLink.IsPublished = false;
            storageLink.IsDeleted = false;
            storageLink.CreatedBy = createdBy;

            return storageLink;
        }

        private static Link CreateRandomDeletedLink(DateTimeOffset currentDateTime)
        {
            Link storageLink = CreateRandomPubliclyVisibleLink(
                linkId: Guid.NewGuid(),
                currentDateTime: currentDateTime,
                hasPublishDate: true);

            storageLink.IsDeleted = true;

            return storageLink;
        }

        private static Filler<Link> CreateLinkFiller(DateTimeOffset dateTimeOffset)
        {
            var filler = new Filler<Link>();

            filler.Setup()
                .OnType<DateTimeOffset>().Use(dateTimeOffset)
                .OnType<DateTimeOffset?>().Use(dateTimeOffset);

            return filler;
        }
    }
}
