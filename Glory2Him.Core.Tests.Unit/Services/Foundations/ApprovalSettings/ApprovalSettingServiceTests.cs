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
using Glory2Him.Core.Brokers.Integrities;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Brokers.Storages.Sql;
using Glory2Him.Core.Brokers.EventEnvelopes;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ApprovalSettings;
using Glory2Him.Core.Models.Foundations.ApprovalSettings.Exceptions;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Services.Foundations.ApprovalSettings;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Moq;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettings
{
    public partial class ApprovalSettingServiceTests
    {
        private readonly Mock<IStorageBroker> storageBrokerMock;
        private readonly Mock<IDateTimeBroker> dateTimeBrokerMock;
        private readonly Mock<IIdentifierBroker> identifierBrokerMock;
        private readonly Mock<IEventBroker> eventBrokerMock;
        private readonly Mock<IEventEnvelopeBroker> eventEnvelopeBrokerMock;
        private readonly Mock<ISecurityAuditBroker> securityAuditBrokerMock;
        private readonly Mock<IEnvelopeIntegrityBroker> envelopeIntegrityBrokerMock;
        private readonly Mock<ILoggingBroker> loggingBrokerMock;
        private readonly IApprovalSettingService approvalSettingService;
        private SecurityContext ambientSecurityContext;

        public ApprovalSettingServiceTests()
        {
            this.storageBrokerMock = new Mock<IStorageBroker>();
            this.dateTimeBrokerMock = new Mock<IDateTimeBroker>();
            this.identifierBrokerMock = new Mock<IIdentifierBroker>();
            this.eventBrokerMock = new Mock<IEventBroker>();
            this.eventEnvelopeBrokerMock = new Mock<IEventEnvelopeBroker>();
            this.securityAuditBrokerMock = new Mock<ISecurityAuditBroker>();
            this.envelopeIntegrityBrokerMock = new Mock<IEnvelopeIntegrityBroker>();
            this.loggingBrokerMock = new Mock<ILoggingBroker>();

            // the ambient caller the envelope broker captures on the direct path — tests
            // override this field (before acting) to run as a different caller
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.IsAny<ApprovalSetting>()))
                    .Returns((ApprovalSetting content) =>
                        new ValueTask<EventEnvelope<ApprovalSetting>>(
                            new EventEnvelope<ApprovalSetting>
                            {
                                Content = content,
                                SecurityContext = this.ambientSecurityContext,
                                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
                            }));

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateNextAsync(
                    It.IsAny<EventEnvelope<ApprovalSetting>>(),
                    It.IsAny<ApprovalSetting>()))
                        .Returns((EventEnvelope<ApprovalSetting> sourceEnvelope, ApprovalSetting content) =>
                            new ValueTask<EventEnvelope<ApprovalSetting>>(
                                new EventEnvelope<ApprovalSetting>
                                {
                                    Content = content,
                                    SecurityContext = sourceEnvelope.SecurityContext,
                                    Metadata = new EventMetadata { EventId = Guid.NewGuid() }
                                }));

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    It.IsAny<EventEnvelope<ApprovalSetting>>(),
                    It.IsAny<string>(),
                    It.IsAny<EnvelopeDirection>()))
                        .ReturnsAsync(true);

            this.approvalSettingService = new ApprovalSettingService(
                storageBroker: this.storageBrokerMock.Object,
                dateTimeBroker: this.dateTimeBrokerMock.Object,
                identifierBroker: this.identifierBrokerMock.Object,
                eventBroker: this.eventBrokerMock.Object,
                eventEnvelopeBroker: this.eventEnvelopeBrokerMock.Object,
                securityAuditBroker: this.securityAuditBrokerMock.Object,
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

        // reads only ask for a signed-in caller, so every role set — including none at
        // all and the blocked contribution role — reads the policy
        public static TheoryData<string[]> AuthenticatedRoleSets() =>
            new TheoryData<string[]>
            {
                new string[0],
                new[] { Roles.ReadOnly },
                new[] { Roles.Reviewers },
                new[] { Roles.Administrators }
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
                    new TimeoutApprovalSettingException(
                        message: "Failed approval setting timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data)
                },
                {
                    dbUpdateException,
                    new FailedStorageApprovalSettingException(
                        message: "Failed approval setting storage error occurred, contact support.",
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
                    new AlreadyExistsApprovalSettingException(
                        message: "Approval setting already exists with the same Id.",
                        innerException: duplicateKeyException,
                        data: duplicateKeyException.Data)
                },
                {
                    foreignKeyConstraintConflictException,
                    new InvalidApprovalSettingReferenceException(
                        message: "Invalid approval setting reference error occurred.",
                        innerException: foreignKeyConstraintConflictException,
                        data: foreignKeyConstraintConflictException.Data)
                },
                {
                    duplicateKeyWithUniqueIndexException,
                    new AlreadyExistsApprovalSettingException(
                        message: "Approval setting already exists, " +
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
                    new LockedApprovalSettingException(
                        message: "Locked approval setting record, please try again later.",
                        innerException: dbUpdateConcurrencyException,
                        data: dbUpdateConcurrencyException.Data)
                },
                {
                    foreignKeyConstraintConflictException,
                    new InvalidApprovalSettingReferenceException(
                        message: "Invalid approval setting reference error occurred.",
                        innerException: foreignKeyConstraintConflictException,
                        data: foreignKeyConstraintConflictException.Data)
                },
                {
                    duplicateKeyWithUniqueIndexException,
                    new AlreadyExistsApprovalSettingException(
                        message: "Approval setting already exists, " +
                            "a uniqueness rule rejected the write.",
                        innerException: duplicateKeyWithUniqueIndexException,
                        data: duplicateKeyWithUniqueIndexException.Data)
                }
            };
        }

        private static ApprovalSetting CreateRandomApprovalSetting() =>
            CreateApprovalSettingFiller(dateTimeOffset: GetRandomDateTimeOffset()).Create();

        private static EventEnvelope<ApprovalSetting> CreateRandomApprovalSettingRequestEnvelope(
            SecurityContext? securityContext = null) =>
            new EventEnvelope<ApprovalSetting>
            {
                Content = new ApprovalSetting { Id = Guid.NewGuid() },
                SecurityContext = securityContext ?? CreateAuthenticatedSecurityContext(Roles.Administrators),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

        private static SecurityContext CreateAuthenticatedSecurityContext(params string[] roles) =>
            new SecurityContext
            {
                IsAuthenticated = true,
                Roles = roles
            };

        private static ApprovalSetting CreateRandomModifyApprovalSetting(
            DateTimeOffset dateTimeOffset,
            string userId = "")
        {
            int randomDaysInPast = GetRandomNegativeNumber();
            ApprovalSetting randomApprovalSetting = CreateApprovalSettingFiller(dateTimeOffset, userId).Create();
            randomApprovalSetting.CreatedWhen = randomApprovalSetting.CreatedWhen.AddDays(randomDaysInPast);

            return randomApprovalSetting;
        }

        private static IQueryable<ApprovalSetting> CreateRandomApprovalSettings()
        {
            return CreateApprovalSettingFiller(dateTimeOffset: GetRandomDateTimeOffset())
                .Create(count: GetRandomNumber())
                .AsQueryable();
        }

        // Bounded well above DateTime.MinValue on purpose: arrangements shift these dates
        // backwards - AddDays(-n) for a stored row, AddSeconds(-90) for the recency window -
        // and a draw near the minimum makes that arithmetic throw. An unbounded earliest date
        // is an intermittently red suite, on whichever test happened to draw it.
        private static DateTimeOffset GetRandomDateTimeOffset() =>
            new DateTimeRange(earliestDate: new DateTime(year: 2000, month: 1, day: 1)).GetValue();

        private static Filler<ApprovalSetting> CreateApprovalSettingFiller(
            DateTimeOffset dateTimeOffset,
            string userId = "")
        {
            userId = string.IsNullOrEmpty(userId) ? Guid.NewGuid().ToString() : userId;
            var filler = new Filler<ApprovalSetting>();

            filler.Setup()
                .OnType<DateTimeOffset>().Use(dateTimeOffset)
                .OnType<DateTimeOffset?>().Use(dateTimeOffset)
                // IsDeleted gates every read and remove path, so it is pinned here rather
                // than drawn: a posture-sensitive test must never depend on the draw. Tests
                // that want a soft-deleted row set it explicitly.
                .OnProperty(approvalSetting => approvalSetting.IsDeleted).Use(false)

                // THE SCOPE IS PINNED TO A LEGAL ONE, and drawn scopes are why. §8.4 permits a
                // content type only on ContentItem and a personality only on Association, and
                // the filler fills the three fields independently — so a draw of
                // (Tag, Story) is a shape the service now refuses, and every test that merely
                // wanted a setting would fail on its fixture rather than on its subject.
                // EntityType is left drawn: null (the global tier) and any member are all legal
                // while nothing narrows them. A scope test sets what it means to exercise.
                .OnProperty(approvalSetting => approvalSetting.ContentType).Use((ContentType?)null)
                .OnProperty(approvalSetting => approvalSetting.IsPersonal).Use((bool?)null)
                .OnProperty(approvalSetting => approvalSetting.CreatedBy).Use(userId)
                .OnProperty(approvalSetting => approvalSetting.UpdatedBy).Use(userId);

            return filler;
        }
    }
}
