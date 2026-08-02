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
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using EFxceptions.Models.Exceptions;
using Glory2Him.Core.Brokers.DateTimes;
using Glory2Him.Core.Brokers.Events;
using Glory2Him.Core.Brokers.Identifiers;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Brokers.Storages.Sql;
using Glory2Him.Core.Brokers.EventEnvelopes;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Services.Foundations.ContentItems;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Moq;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItems
{
    public partial class ContentItemServiceTests
    {
        private readonly Mock<IStorageBroker> storageBrokerMock;
        private readonly Mock<IDateTimeBroker> dateTimeBrokerMock;
        private readonly Mock<IIdentifierBroker> identifierBrokerMock;
        private readonly Mock<IEventBroker> eventBrokerMock;
        private readonly Mock<IEventEnvelopeBroker> eventEnvelopeBrokerMock;
        private readonly Mock<ISecurityAuditBroker> securityAuditBrokerMock;
        private readonly Mock<ILoggingBroker> loggingBrokerMock;
        private readonly IContentItemService contentItemService;
        private SecurityContext ambientSecurityContext;

        public ContentItemServiceTests()
        {
            this.storageBrokerMock = new Mock<IStorageBroker>();
            this.dateTimeBrokerMock = new Mock<IDateTimeBroker>();
            this.identifierBrokerMock = new Mock<IIdentifierBroker>();
            this.eventBrokerMock = new Mock<IEventBroker>();
            this.eventEnvelopeBrokerMock = new Mock<IEventEnvelopeBroker>();
            this.securityAuditBrokerMock = new Mock<ISecurityAuditBroker>();
            this.loggingBrokerMock = new Mock<ILoggingBroker>();

            // the ambient caller the envelope broker captures on the direct path — tests
            // override this field (before acting) to run as a different caller
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.IsAny<ContentItem>()))
                    .Returns((ContentItem content) =>
                        new ValueTask<EventEnvelope<ContentItem>>(
                            new EventEnvelope<ContentItem>
                            {
                                Content = content,
                                SecurityContext = this.ambientSecurityContext,
                                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
                            }));

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateNextAsync(
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    It.IsAny<ContentItem>()))
                        .Returns((EventEnvelope<ContentItem> sourceEnvelope, ContentItem content) =>
                            new ValueTask<EventEnvelope<ContentItem>>(
                                new EventEnvelope<ContentItem>
                                {
                                    Content = content,
                                    SecurityContext = sourceEnvelope.SecurityContext,
                                    Metadata = new EventMetadata { EventId = Guid.NewGuid() }
                                }));

            this.contentItemService = new ContentItemService(
                storageBroker: this.storageBrokerMock.Object,
                dateTimeBroker: this.dateTimeBrokerMock.Object,
                identifierBroker: this.identifierBrokerMock.Object,
                eventBroker: this.eventBrokerMock.Object,
                eventEnvelopeBroker: this.eventEnvelopeBrokerMock.Object,
                securityAuditBroker: this.securityAuditBrokerMock.Object,
                loggingBroker: this.loggingBrokerMock.Object);
        }

        private static IQueryable<ContentItem> CreateRandomContentItems()
        {
            return CreateContentItemFiller(dateTimeOffset: GetRandomDateTimeOffset())
                .Create(count: GetRandomNumber())
                .AsQueryable();
        }

        private static ContentItem CreateRandomModifyContentItem(
            DateTimeOffset dateTimeOffset,
            string userId = "")
        {
            int randomDaysInPast = GetRandomNegativeNumber();
            ContentItem randomContentItem = CreateContentItemFiller(dateTimeOffset, userId).Create();
            randomContentItem.CreatedWhen = randomContentItem.CreatedWhen.AddDays(randomDaysInPast);

            return randomContentItem;
        }

        public static TheoryData<int> MinutesBeforeOrAfter()
        {
            int randomTimeInFuture = GetRandomNumber();
            int randomTimeInPast = GetRandomNegativeNumber();

            return new TheoryData<int>
            {
                randomTimeInFuture,
                randomTimeInPast
            };
        }

        public static TheoryData<SecurityContext> UnauthenticatedSecurityContexts() =>
            new TheoryData<SecurityContext>
            {
                null,
                new SecurityContext { IsAuthenticated = false }
            };

        public static TheoryData<string[]> NonAdminRoleSets() =>
            new TheoryData<string[]>
            {
                new string[0],
                new[] { Roles.Reviewer }
            };

        public static TheoryData<string> ReviewRoles() =>
            new TheoryData<string>
            {
                Roles.Reviewer,
                Roles.ContentItemReviewer,
                Roles.Publisher,
                Roles.ContentItemPublisher,
                Roles.Admin
            };

        public static TheoryData<Exception, Xeption> DependencyValidationExceptions()
        {
            string someMessage = GetRandomString();
            var duplicateKeyException = new DuplicateKeyException(someMessage);
            var foreignKeyConstraintConflictException = new ForeignKeyConstraintConflictException(someMessage);

            return new TheoryData<Exception, Xeption>
            {
                {
                    duplicateKeyException,
                    new AlreadyExistsContentItemException(
                        message: "Content item already exists with the same Id.",
                        innerException: duplicateKeyException,
                        data: duplicateKeyException.Data)
                },
                {
                    foreignKeyConstraintConflictException,
                    new InvalidContentItemReferenceException(
                        message: "Invalid content item reference error occurred.",
                        innerException: foreignKeyConstraintConflictException,
                        data: foreignKeyConstraintConflictException.Data)
                }
            };
        }

        public static TheoryData<Exception, Xeption> ModifyDependencyValidationExceptions()
        {
            string someMessage = GetRandomString();
            var dbUpdateConcurrencyException = new DbUpdateConcurrencyException();
            var foreignKeyConstraintConflictException = new ForeignKeyConstraintConflictException(someMessage);

            return new TheoryData<Exception, Xeption>
            {
                {
                    dbUpdateConcurrencyException,
                    new LockedContentItemException(
                        message: "Locked content item record, please try again later.",
                        innerException: dbUpdateConcurrencyException,
                        data: dbUpdateConcurrencyException.Data)
                },
                {
                    foreignKeyConstraintConflictException,
                    new InvalidContentItemReferenceException(
                        message: "Invalid content item reference error occurred.",
                        innerException: foreignKeyConstraintConflictException,
                        data: foreignKeyConstraintConflictException.Data)
                }
            };
        }

        public static TheoryData<Exception, Xeption> DependencyExceptions()
        {
            var operationCanceledException = new OperationCanceledException();
            var timeoutException = new TimeoutException("The dependency operation timed out.");
            var dbUpdateException = new DbUpdateException();

            return new TheoryData<Exception, Xeption>
            {
                {
                    operationCanceledException,
                    new TimeoutContentItemException(
                        message: "Failed content item timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data)
                },
                {
                    dbUpdateException,
                    new FailedStorageContentItemException(
                        message: "Failed content item storage error occurred, contact support.",
                        innerException: dbUpdateException,
                        data: dbUpdateException.Data)
                }
            };
        }

        private static Expression<Func<Xeption, bool>> SameExceptionAs(Xeption expectedException) =>
            actualException => actualException.SameExceptionAs(expectedException);

        // the duplicate probe's request payload carries only the content type and hash
        // being checked — the envelope exists to capture the ambient security context
        private static Expression<Func<ContentItem, bool>> SameCheckRequestAs(
            Guid expectedContentTypeId,
            string expectedContentHash) =>
            actualContentItem => actualContentItem.ContentTypeId == expectedContentTypeId
                && actualContentItem.ContentHash == expectedContentHash;

        private static SqlException GetSqlException() =>
            (SqlException)RuntimeHelpers.GetUninitializedObject(typeof(SqlException));

        private static string GetRandomString() =>
            new MnemonicString(wordCount: GetRandomNumber()).GetValue();

        private static string GetRandomStringWithLengthOf(int length)
        {
            string result = new MnemonicString(wordCount: 1, wordMinLength: length, wordMaxLength: length).GetValue();

            return result.Length > length ? result.Substring(0, length) : result;
        }

        private static int GetRandomNumber() =>
            new IntRange(min: 2, max: 10).GetValue();

        private static int GetRandomNegativeNumber() =>
            -1 * new IntRange(min: 2, max: 10).GetValue();

        private static DateTimeOffset GetRandomDateTimeOffset() =>
            new DateTimeRange(earliestDate: new DateTime()).GetValue();

        private static ContentItem CreateRandomContentItem() =>
            CreateContentItemFiller(dateTimeOffset: GetRandomDateTimeOffset()).Create();

        private static EventEnvelope<ContentItem> CreateRandomContentItemRequestEnvelope(
            SecurityContext? securityContext = null) =>
            new EventEnvelope<ContentItem>
            {
                Content = new ContentItem { Id = Guid.NewGuid() },
                SecurityContext = securityContext ?? CreateAuthenticatedSecurityContext(),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

        private static SecurityContext CreateAuthenticatedSecurityContext(params string[] roles) =>
            new SecurityContext
            {
                IsAuthenticated = true,
                Roles = roles
            };

        private static ContentItem CreateRandomContentItem(DateTimeOffset dateTimeOffset, string userId = "") =>
            CreateContentItemFiller(dateTimeOffset, userId).Create();

        private static Filler<ContentItem> CreateContentItemFiller(
            DateTimeOffset dateTimeOffset,
            string userId = "")
        {
            userId = string.IsNullOrEmpty(userId) ? Guid.NewGuid().ToString() : userId;
            var filler = new Filler<ContentItem>();

            filler.Setup()
                .OnType<DateTimeOffset>().Use(dateTimeOffset)
                .OnType<DateTimeOffset?>().Use(dateTimeOffset)
                .OnProperty(contentItem => contentItem.ContentType).IgnoreIt()
                .OnProperty(contentItem => contentItem.CreatedBy).Use(userId)
                .OnProperty(contentItem => contentItem.UpdatedBy).Use(userId);

            return filler;
        }
    }
}
