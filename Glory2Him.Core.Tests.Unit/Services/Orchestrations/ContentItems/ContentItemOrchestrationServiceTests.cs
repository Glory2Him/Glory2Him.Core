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
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Glory2Him.Core.Brokers.Hashes;
using Glory2Him.Core.Brokers.Identifiers;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.EventEnvelopes;
using Glory2Him.Core.Brokers.Events;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Orchestrations;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
using Glory2Him.Core.Services.Foundations.ContentItems;
using Glory2Him.Core.Services.Orchestrations.ContentItems;
using Moq;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.ContentItems
{
    public partial class ContentItemOrchestrationServiceTests
    {
        private readonly Mock<IContentItemService> contentItemServiceMock;
        private readonly Mock<IHashBroker> hashBrokerMock;
        private readonly Mock<IIdentifierBroker> identifierBrokerMock;
        private readonly Mock<IEventEnvelopeBroker> eventEnvelopeBrokerMock;
        private readonly Mock<IEventBroker> eventBrokerMock;
        private readonly Mock<ISecurityAuditBroker> securityAuditBrokerMock;
        private readonly Mock<ILoggingBroker> loggingBrokerMock;
        private readonly IContentItemOrchestrationService contentItemOrchestrationService;

        public ContentItemOrchestrationServiceTests()
        {
            this.contentItemServiceMock = new Mock<IContentItemService>();
            this.hashBrokerMock = new Mock<IHashBroker>();
            this.identifierBrokerMock = new Mock<IIdentifierBroker>();
            this.eventEnvelopeBrokerMock = new Mock<IEventEnvelopeBroker>();
            this.eventBrokerMock = new Mock<IEventBroker>();
            this.securityAuditBrokerMock = new Mock<ISecurityAuditBroker>();
            this.loggingBrokerMock = new Mock<ILoggingBroker>();

            this.contentItemOrchestrationService = new ContentItemOrchestrationService(
                contentItemService: this.contentItemServiceMock.Object,
                hashBroker: this.hashBrokerMock.Object,
                identifierBroker: this.identifierBrokerMock.Object,
                eventEnvelopeBroker: this.eventEnvelopeBrokerMock.Object,
                eventBroker: this.eventBrokerMock.Object,
                securityAuditBroker: this.securityAuditBrokerMock.Object,
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
        // every completed do-work publishes the orchestration's own fact on the next
        // envelope in the causation chain; the helpers keep that setup out of each test
        private EventEnvelope<ContentItem> SetupCompletionFactPublish(
            EventEnvelope<ContentItem> inboundEnvelope,
            ContentItem resultContentItem,
            ContentItemOrchestrationEventOperation operation)
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
                broker.PublishContentItemOrchestrationAsync(outboundEnvelope, operation))
                    .ReturnsAsync(new EventPublishResult<ContentItem>());

            return outboundEnvelope;
        }

        private void VerifyCompletionFactPublished(
            EventEnvelope<ContentItem> outboundEnvelope,
            ContentItemOrchestrationEventOperation operation)
        {
            this.eventBrokerMock.Verify(broker =>
                broker.PublishContentItemOrchestrationAsync(outboundEnvelope, operation),
                Times.Once);

            this.eventBrokerMock.VerifyNoOtherCalls();
        }

        private static Expression<Func<ContentItem, bool>> SameRemoveRequestAs(
            Guid expectedContentItemId,
            string? expectedDeletionReason) =>
            actualContentItem => actualContentItem.Id == expectedContentItemId
                && actualContentItem.DeletionReason == expectedDeletionReason;

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

        // the "current" row the modify flow loads from storage: the modifiable tip of its group
        private static ContentItem CreateRandomStorageContentItem(
            Guid contentItemId,
            ApprovalStatus approvalStatus,
            string createdBy)
        {
            ContentItem storageContentItem = CreateRandomContentItem();
            storageContentItem.Id = contentItemId;
            storageContentItem.ApprovalStatus = approvalStatus;
            storageContentItem.CreatedBy = createdBy;
            storageContentItem.IsLatestVersion = true;
            storageContentItem.IsDeleted = false;

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
