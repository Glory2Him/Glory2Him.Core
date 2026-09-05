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
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Brokers.DateTimes;
using Glory2Him.Core.Brokers.Events;
using Glory2Him.Core.Brokers.Identifiers;
using Glory2Him.Core.Brokers.Integrities;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Brokers.Storages.Sql;
using Glory2Him.Core.Brokers.EventEnvelopes;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Glory2Him.Core.Models.Foundations.BibleReferences.Exceptions;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Services.Foundations.BibleReferences;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Moq;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.BibleReferences
{
    public partial class BibleReferenceServiceTests
    {
        private readonly Mock<IStorageBroker> storageBrokerMock;
        private readonly Mock<IDateTimeBroker> dateTimeBrokerMock;
        private readonly Mock<IIdentifierBroker> identifierBrokerMock;
        private readonly Mock<IEventBroker> eventBrokerMock;
        private readonly Mock<IEventEnvelopeBroker> eventEnvelopeBrokerMock;
        private readonly Mock<ISecurityAuditBroker> securityAuditBrokerMock;
        private readonly Mock<IAccessBroker> accessBrokerMock;
        private readonly Mock<IEnvelopeIntegrityBroker> envelopeIntegrityBrokerMock;
        private readonly Mock<ILoggingBroker> loggingBrokerMock;
        private readonly IBibleReferenceService bibleReferenceService;
        private SecurityContext ambientSecurityContext;

        public BibleReferenceServiceTests()
        {
            this.storageBrokerMock = new Mock<IStorageBroker>();
            this.dateTimeBrokerMock = new Mock<IDateTimeBroker>();
            this.identifierBrokerMock = new Mock<IIdentifierBroker>();
            this.eventBrokerMock = new Mock<IEventBroker>();
            this.eventEnvelopeBrokerMock = new Mock<IEventEnvelopeBroker>();
            this.securityAuditBrokerMock = new Mock<ISecurityAuditBroker>();
            this.accessBrokerMock = new Mock<IAccessBroker>();

            // an approve decision is permitted by default so the non-approval tests that never
            // touch this broker are unaffected; approve tests override it
            this.accessBrokerMock.Setup(broker =>
                broker.MayDecideApprovalAsync(
                    It.IsAny<ApprovalDecisionQuery>(),
                    It.IsAny<System.Threading.CancellationToken>()))
                        .ReturnsAsync(PermittedVerdict());
            this.envelopeIntegrityBrokerMock = new Mock<IEnvelopeIntegrityBroker>();
            this.loggingBrokerMock = new Mock<ILoggingBroker>();

            // the ambient caller the envelope broker captures on the direct path — tests
            // override this field (before acting) to run as a different caller
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.IsAny<BibleReference>()))
                    .Returns((BibleReference content) =>
                        new ValueTask<EventEnvelope<BibleReference>>(
                            new EventEnvelope<BibleReference>
                            {
                                Content = content,
                                SecurityContext = this.ambientSecurityContext,
                                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
                            }));

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateNextAsync(
                    It.IsAny<EventEnvelope<BibleReference>>(),
                    It.IsAny<BibleReference>()))
                        .Returns((EventEnvelope<BibleReference> sourceEnvelope, BibleReference content) =>
                            new ValueTask<EventEnvelope<BibleReference>>(
                                new EventEnvelope<BibleReference>
                                {
                                    Content = content,
                                    SecurityContext = sourceEnvelope.SecurityContext,
                                    Metadata = new EventMetadata { EventId = Guid.NewGuid() }
                                }));

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    It.IsAny<EventEnvelope<BibleReference>>(),
                    It.IsAny<string>(),
                    It.IsAny<EnvelopeDirection>()))
                        .ReturnsAsync(true);

            this.bibleReferenceService = new BibleReferenceService(
                storageBroker: this.storageBrokerMock.Object,
                dateTimeBroker: this.dateTimeBrokerMock.Object,
                identifierBroker: this.identifierBrokerMock.Object,
                eventBroker: this.eventBrokerMock.Object,
                eventEnvelopeBroker: this.eventEnvelopeBrokerMock.Object,
                securityAuditBroker: this.securityAuditBrokerMock.Object,
                accessBroker: this.accessBrokerMock.Object,
                envelopeIntegrityBroker: this.envelopeIntegrityBrokerMock.Object,
                loggingBroker: this.loggingBrokerMock.Object);
        }

        private static Expression<Func<Xeption, bool>> SameExceptionAs(Xeption expectedException) =>
            actualException => actualException.SameExceptionAs(expectedException);

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
                new[] { Roles.Reviewers }
            };

        public static TheoryData<string> ReviewRoles() =>
            new TheoryData<string>
            {
                Roles.Reviewers,
                Roles.BibleReferenceReviewers,
                Roles.Publishers,
                Roles.BibleReferencePublishers,
                Roles.Administrators
            };

        public static TheoryData<Exception, Xeption> DependencyExceptions()
        {
            var operationCanceledException = new OperationCanceledException();
            var timeoutException = new TimeoutException("The dependency operation timed out.");
            var dbUpdateException = new DbUpdateException();

            return new TheoryData<Exception, Xeption>
            {
                {
                    operationCanceledException,
                    new TimeoutBibleReferenceException(
                        message: "Failed bible reference timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data)
                },
                {
                    dbUpdateException,
                    new FailedStorageBibleReferenceException(
                        message: "Failed bible reference storage error occurred, contact support.",
                        innerException: dbUpdateException,
                        data: dbUpdateException.Data)
                }
            };
        }

        public static TheoryData<Exception, Xeption> DependencyValidationExceptions()
        {
            string someMessage = GetRandomString();
            var duplicateKeyException = new DuplicateKeyException(someMessage);
            var foreignKeyConstraintConflictException = new ForeignKeyConstraintConflictException(someMessage);
            var duplicateKeyWithUniqueIndexException =
                new DuplicateKeyWithUniqueIndexException(someMessage);

            return new TheoryData<Exception, Xeption>
            {
                {
                    duplicateKeyException,
                    new AlreadyExistsBibleReferenceException(
                        message: "Bible reference already exists with the same Id.",
                        innerException: duplicateKeyException,
                        data: duplicateKeyException.Data)
                },
                {
                    foreignKeyConstraintConflictException,
                    new InvalidBibleReferenceReferenceException(
                        message: "Invalid bible reference reference error occurred.",
                        innerException: foreignKeyConstraintConflictException,
                        data: foreignKeyConstraintConflictException.Data)
                },
                {
                    duplicateKeyWithUniqueIndexException,
                    new AlreadyExistsBibleReferenceException(
                        message: "Bible reference already exists, " +
                            "a uniqueness rule rejected the write.",
                        innerException: duplicateKeyWithUniqueIndexException,
                        data: duplicateKeyWithUniqueIndexException.Data)
                }
            };
        }

        public static TheoryData<Exception, Xeption> ModifyDependencyValidationExceptions()
        {
            string someMessage = GetRandomString();
            var dbUpdateConcurrencyException = new DbUpdateConcurrencyException();
            var foreignKeyConstraintConflictException = new ForeignKeyConstraintConflictException(someMessage);
            var duplicateKeyWithUniqueIndexException =
                new DuplicateKeyWithUniqueIndexException(someMessage);

            return new TheoryData<Exception, Xeption>
            {
                {
                    dbUpdateConcurrencyException,
                    new LockedBibleReferenceException(
                        message: "Locked bible reference record, please try again later.",
                        innerException: dbUpdateConcurrencyException,
                        data: dbUpdateConcurrencyException.Data)
                },
                {
                    foreignKeyConstraintConflictException,
                    new InvalidBibleReferenceReferenceException(
                        message: "Invalid bible reference reference error occurred.",
                        innerException: foreignKeyConstraintConflictException,
                        data: foreignKeyConstraintConflictException.Data)
                },
                {
                    duplicateKeyWithUniqueIndexException,
                    new AlreadyExistsBibleReferenceException(
                        message: "Bible reference already exists, " +
                            "a uniqueness rule rejected the write.",
                        innerException: duplicateKeyWithUniqueIndexException,
                        data: duplicateKeyWithUniqueIndexException.Data)
                }
            };
        }

        private static BibleReference CreateRandomBibleReference() =>
            CreateBibleReferenceFiller(dateTimeOffset: GetRandomDateTimeOffset()).Create();

        private static EventEnvelope<BibleReference> CreateRandomBibleReferenceRequestEnvelope(
            SecurityContext? securityContext = null) =>
            new EventEnvelope<BibleReference>
            {
                Content = new BibleReference { Id = Guid.NewGuid() },
                SecurityContext = securityContext ?? CreateAuthenticatedSecurityContext(),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

        private static SecurityContext CreateAuthenticatedSecurityContext(params string[] roles) =>
            new SecurityContext
            {
                IsAuthenticated = true,
                Roles = roles
            };

        private static BibleReference CreateRandomModifyBibleReference(
            DateTimeOffset dateTimeOffset,
            string userId = "")
        {
            int randomDaysInPast = GetRandomNegativeNumber();
            BibleReference randomBibleReference = CreateBibleReferenceFiller(dateTimeOffset, userId).Create();
            randomBibleReference.CreatedWhen = randomBibleReference.CreatedWhen.AddDays(randomDaysInPast);

            return randomBibleReference;
        }

        private static IQueryable<BibleReference> CreateRandomBibleReferences()
        {
            return CreateBibleReferenceFiller(dateTimeOffset: GetRandomDateTimeOffset())
                .Create(count: GetRandomNumber())
                .AsQueryable();
        }

        // Bounded well above DateTime.MinValue on purpose: arrangements shift these dates
        // backwards - AddDays(-n) for a stored row, AddSeconds(-90) for the recency window -
        // and a draw near the minimum makes that arithmetic throw. An unbounded earliest date
        // is an intermittently red suite, on whichever test happened to draw it.
        private static DateTimeOffset GetRandomDateTimeOffset() =>
            new DateTimeRange(earliestDate: new DateTime(year: 2000, month: 1, day: 1)).GetValue();

        private static AccessVerdict PermittedVerdict() =>
            new AccessVerdict
            {
                IsPermitted = true,
                DenialReason = AccessDenialReason.None,
                IsBypassUsed = false,
                BypassedBlockReason = AccessDenialReason.None,
                Explanation = "permitted",
            };

        private static AccessVerdict PermittedBypassVerdict() =>
            new AccessVerdict
            {
                IsPermitted = true,
                DenialReason = AccessDenialReason.None,
                IsBypassUsed = true,
                BypassedBlockReason = AccessDenialReason.None,
                Explanation = "permitted with bypass",
            };

        private static AccessVerdict DeniedVerdict() =>
            new AccessVerdict
            {
                IsPermitted = false,
                DenialReason = AccessDenialReason.NotInPublisherTier,
                IsBypassUsed = false,
                BypassedBlockReason = AccessDenialReason.None,
                Explanation = "denied",
            };

        private static Filler<BibleReference> CreateBibleReferenceFiller(
            DateTimeOffset dateTimeOffset,
            string userId = "")
        {
            userId = string.IsNullOrEmpty(userId) ? Guid.NewGuid().ToString() : userId;
            var filler = new Filler<BibleReference>();

            filler.Setup()
                .OnType<DateTimeOffset>().Use(dateTimeOffset)
                .OnType<DateTimeOffset?>().Use(dateTimeOffset)
                // IsDeleted gates every read and remove path, so it is pinned here rather
                // than drawn: a posture-sensitive test must never depend on the draw. Tests
                // that want a soft-deleted row set it explicitly.
                .OnProperty(bibleReference => bibleReference.IsDeleted).Use(false)
                .OnProperty(bibleReference => bibleReference.CreatedBy).Use(userId)
                .OnProperty(bibleReference => bibleReference.UpdatedBy).Use(userId)

                // A contribution is unpublished and unapproved: add refuses a caller-supplied IsPublished,
                // PublishDate or verdict status, and modify pins all three against storage. Drawing them
                // would make every write test fail on the draw rather than on what it is testing.
                .OnProperty(bibleReference => bibleReference.ApprovalStatus).Use(ApprovalStatus.Draft)
                .OnProperty(bibleReference => bibleReference.IsPublished).Use(false)
                .OnProperty(bibleReference => bibleReference.PublishDate).IgnoreIt();

            return filler;
        }
    }
}
