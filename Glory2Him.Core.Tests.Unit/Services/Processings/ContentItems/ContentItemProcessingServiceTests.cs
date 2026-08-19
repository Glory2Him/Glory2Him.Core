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
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Glory2Him.Core.Brokers.DateTimes;
using Glory2Him.Core.Brokers.Hashes;
using Glory2Him.Core.Brokers.Identifiers;
using Glory2Him.Core.Brokers.Integrities;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.EventEnvelopes;
using Glory2Him.Core.Brokers.Events;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Processings;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
using Glory2Him.Core.Services.Foundations.ContentItems;
using Glory2Him.Core.Services.Processings.ContentItems;
using Moq;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Processings.ContentItems
{
    public partial class ContentItemProcessingServiceTests
    {
        private readonly Mock<IContentItemService> contentItemServiceMock;
        private readonly Mock<IDateTimeBroker> dateTimeBrokerMock;
        private readonly Mock<IHashBroker> hashBrokerMock;
        private readonly Mock<IIdentifierBroker> identifierBrokerMock;
        private readonly Mock<IEventEnvelopeBroker> eventEnvelopeBrokerMock;
        private readonly Mock<IEventBroker> eventBrokerMock;
        private readonly Mock<ISecurityAuditBroker> securityAuditBrokerMock;
        private readonly Mock<IEnvelopeIntegrityBroker> envelopeIntegrityBrokerMock;
        private readonly Mock<ILoggingBroker> loggingBrokerMock;
        private readonly IContentItemProcessingService contentItemProcessingService;

        public ContentItemProcessingServiceTests()
        {
            this.contentItemServiceMock = new Mock<IContentItemService>();
            this.dateTimeBrokerMock = new Mock<IDateTimeBroker>();
            this.hashBrokerMock = new Mock<IHashBroker>();
            this.identifierBrokerMock = new Mock<IIdentifierBroker>();
            this.eventEnvelopeBrokerMock = new Mock<IEventEnvelopeBroker>();
            this.eventBrokerMock = new Mock<IEventBroker>();
            this.securityAuditBrokerMock = new Mock<ISecurityAuditBroker>();
            this.envelopeIntegrityBrokerMock = new Mock<IEnvelopeIntegrityBroker>();
            this.loggingBrokerMock = new Mock<ILoggingBroker>();

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    It.IsAny<string>(),
                    It.IsAny<EnvelopeDirection>()))
                        .ReturnsAsync(true);

            this.contentItemProcessingService = new ContentItemProcessingService(
                contentItemService: this.contentItemServiceMock.Object,
                dateTimeBroker: this.dateTimeBrokerMock.Object,
                hashBroker: this.hashBrokerMock.Object,
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
                new ContentItemValidationException(
                    message: randomMessage,
                    innerException: innerException),

                new ContentItemDependencyValidationException(
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
                new ContentItemDependencyException(
                    message: randomMessage,
                    innerException: innerException),

                new ContentItemServiceException(
                    message: randomMessage,
                    innerException: innerException)
            };
        }

        // Test-side twins of the frozen normalization + hashing contract (design §3.4.2):
        // any drift in the production implementation fails these tests.
        private static string NormalizeContent(string content) =>
            Regex.Replace(content.Trim(), pattern: @"\s+", replacement: " ")
                .ToLowerInvariant();

        private static string ComputeContentHash(string content)
        {
            byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(NormalizeContent(content)));

            return Convert.ToHexStringLower(hashBytes);
        }

        private static Expression<Func<Xeption, bool>> SameExceptionAs(Xeption expectedException) =>
            actualException => actualException.SameExceptionAs(expectedException);

        // the remove request payload is minted inside the service, so it is matched on the
        // instruction it carries: the id of the row to remove and the optional reason
        // every completed do-work publishes the processing service's own fact on the next
        // envelope in the causation chain; the helpers keep that setup out of each test
        private EventEnvelope<ContentItem> SetupCompletionFactPublish(
            EventEnvelope<ContentItem> inboundEnvelope,
            ContentItem resultContentItem,
            ContentItemProcessingEventOperation operation)
        {
            var outboundEnvelope = new EventEnvelope<ContentItem>
            {
                Content = resultContentItem,
                SecurityContext = inboundEnvelope.SecurityContext,

                Metadata = new EventMetadata
                {
                    EventId = Guid.NewGuid(),
                    CausationId = inboundEnvelope.Metadata.EventId.ToString()
                }
            };

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateNextAsync(inboundEnvelope, resultContentItem))
                    .ReturnsAsync(outboundEnvelope);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishContentItemProcessingAsync(outboundEnvelope, operation))
                    .ReturnsAsync(new EventPublishResult<ContentItem>());

            return outboundEnvelope;
        }

        private void VerifyCompletionFactPublished(
            EventEnvelope<ContentItem> outboundEnvelope,
            ContentItemProcessingEventOperation operation)
        {
            this.eventBrokerMock.Verify(broker =>
                broker.PublishContentItemProcessingAsync(outboundEnvelope, operation),
                Times.Once);

            this.eventBrokerMock.VerifyNoOtherCalls();
        }

        private static Expression<Func<ContentItem, bool>> SameRemoveRequestAs(
            Guid expectedContentItemId,
            string? expectedDeletionReason) =>
            actualContentItem => actualContentItem.Id == expectedContentItemId
                && actualContentItem.DeletionReason == expectedDeletionReason;

        // the retrieve request payload carries only the id of the version to read
        private static Expression<Func<ContentItem, bool>> SameRetrieveRequestAs(
            Guid expectedContentItemId) =>
            actualContentItem => actualContentItem.Id == expectedContentItemId;

        // a group read's request payload carries only the group whose versions to read
        private static Expression<Func<ContentItem, bool>> SameGroupRetrieveRequestAs(
            Guid expectedGroupId) =>
            actualContentItem => actualContentItem.GroupId == expectedGroupId;

        // an unfiltered collection read carries no instruction at all — the request payload
        // is an empty content item minted only to capture the ambient security context
        private static Expression<Func<ContentItem, bool>> SameRetrieveAllRequest() =>
            actualContentItem => actualContentItem.Id == Guid.Empty
                && actualContentItem.GroupId == Guid.Empty;

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

        private static EventEnvelope<ContentItem> CreateEventEnvelope(
            ContentItem contentItem,
            SecurityContext securityContext) =>
            new EventEnvelope<ContentItem>
            {
                Content = contentItem,
                SecurityContext = securityContext,
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

        private static IQueryable<ContentItem> CreateRandomContentItems() =>
            CreateContentItemFiller(dateTimeOffset: GetRandomDateTimeOffset())
                .Create(count: GetRandomNumber())
                .AsQueryable();

        private static ContentItem CreateRandomContentItem() =>
            CreateContentItemFiller(dateTimeOffset: GetRandomDateTimeOffset()).Create();

        // The "current" row the modify flow loads from storage. It can no longer declare
        // itself the tip: the tip is DERIVED — the highest Version among the group's
        // non-deleted rows — so the Version is pinned to a known number here and
        // SetupGroupTip seeds the GROUP that decides the answer.
        private static ContentItem CreateRandomStorageContentItem(
            Guid contentItemId,
            ApprovalStatus approvalStatus,
            string createdBy)
        {
            ContentItem storageContentItem = CreateRandomContentItem();
            storageContentItem.Id = contentItemId;
            storageContentItem.ApprovalStatus = approvalStatus;
            storageContentItem.CreatedBy = createdBy;
            storageContentItem.Version = 2;
            storageContentItem.IsDeleted = false;

            return storageContentItem;
        }

        // Seeds the group the derivation reads, so that storageContentItem genuinely is — or
        // genuinely is not — its tip. A test cannot state the answer on the row any more; the
        // only thing that can take the tip away from a row is a live sibling at a higher
        // Version, so that is what "not the tip" is made of here.
        private void SetupGroupTip(ContentItem storageContentItem, bool isTheGroupTip)
        {
            var groupContentItems = new List<ContentItem> { storageContentItem };

            if (isTheGroupTip is false)
            {
                ContentItem newerVersionContentItem = CreateRandomContentItem();
                newerVersionContentItem.GroupId = storageContentItem.GroupId;
                newerVersionContentItem.Version = storageContentItem.Version + 1;
                newerVersionContentItem.IsDeleted = false;
                groupContentItems.Add(newerVersionContentItem);
            }

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(groupContentItems.AsQueryable());
        
            // The fork numbers from the group high-water mark, so the seeded group has to
            // report one. Kept in the same helper as the tip so a test cannot describe a
            // group whose tip and highest version disagree by accident (#271).
            this.contentItemServiceMock.Setup(service =>
                service.FindHighestVersionInGroupAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(groupContentItems.Max(contentItem => contentItem.Version));
}

        // the derivation costs one read of the group, which VerifyNoOtherCalls would
        // otherwise flag
        private void VerifyGroupTipResolved() =>
            this.contentItemServiceMock.Verify(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

        // a row that satisfies canonical content visibility (§14.1) as of currentDateTime
        private static ContentItem CreateRandomPubliclyVisibleContentItem(
            Guid contentItemId,
            DateTimeOffset currentDateTime,
            bool hasPublishDate)
        {
            ContentItem storageContentItem = CreateRandomContentItem();
            storageContentItem.Id = contentItemId;
            storageContentItem.ApprovalStatus = ApprovalStatus.Approved;
            storageContentItem.IsPublished = true;
            storageContentItem.IsDeleted = false;

            storageContentItem.PublishDate = hasPublishDate
                ? currentDateTime.AddDays(-1)
                : null;

            return storageContentItem;
        }

        // a row that misses canonical visibility (§14.1): unpublished and not approved —
        // visible only to its owner and the review roles on collection reads
        private static ContentItem CreateRandomNonPublicContentItem(string createdBy)
        {
            ContentItem storageContentItem = CreateRandomContentItem();
            storageContentItem.ApprovalStatus = ApprovalStatus.Draft;
            storageContentItem.IsPublished = false;
            storageContentItem.IsDeleted = false;
            storageContentItem.CreatedBy = createdBy;

            return storageContentItem;
        }

        private static ContentItem CreateRandomDeletedContentItem(DateTimeOffset currentDateTime)
        {
            ContentItem storageContentItem = CreateRandomPubliclyVisibleContentItem(
                contentItemId: Guid.NewGuid(),
                currentDateTime: currentDateTime,
                hasPublishDate: true);

            storageContentItem.IsDeleted = true;

            return storageContentItem;
        }

        private static Filler<ContentItem> CreateContentItemFiller(DateTimeOffset dateTimeOffset)
        {
            var filler = new Filler<ContentItem>();

            filler.Setup()
                .OnType<DateTimeOffset>().Use(dateTimeOffset)
                .OnType<DateTimeOffset?>().Use(dateTimeOffset)
                .OnProperty(contentItem => contentItem.ContentType).IgnoreIt();

            return filler;
        }
    }
}
