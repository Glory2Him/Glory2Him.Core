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
using Glory2Him.Core.Models.Foundations.ApprovalComments;
using Glory2Him.Core.Models.Foundations.ApprovalComments.Exceptions;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Services.Foundations.ApprovalComments;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Moq;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalComments
{
    public partial class ApprovalCommentServiceTests
    {
        private readonly Mock<IStorageBroker> storageBrokerMock;
        private readonly Mock<IDateTimeBroker> dateTimeBrokerMock;
        private readonly Mock<IIdentifierBroker> identifierBrokerMock;
        private readonly Mock<IEventBroker> eventBrokerMock;
        private readonly Mock<IEventEnvelopeBroker> eventEnvelopeBrokerMock;
        private readonly Mock<ISecurityAuditBroker> securityAuditBrokerMock;
        private readonly Mock<IEnvelopeIntegrityBroker> envelopeIntegrityBrokerMock;
        private readonly Mock<ILoggingBroker> loggingBrokerMock;
        private readonly IApprovalCommentService approvalCommentService;
        private SecurityContext ambientSecurityContext;

        public ApprovalCommentServiceTests()
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
                broker.CreateAsync(It.IsAny<ApprovalComment>()))
                    .Returns((ApprovalComment content) =>
                        new ValueTask<EventEnvelope<ApprovalComment>>(
                            new EventEnvelope<ApprovalComment>
                            {
                                Content = content,
                                SecurityContext = this.ambientSecurityContext,
                                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
                            }));

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateNextAsync(
                    It.IsAny<EventEnvelope<ApprovalComment>>(),
                    It.IsAny<ApprovalComment>()))
                        .Returns((EventEnvelope<ApprovalComment> sourceEnvelope, ApprovalComment content) =>
                            new ValueTask<EventEnvelope<ApprovalComment>>(
                                new EventEnvelope<ApprovalComment>
                                {
                                    Content = content,
                                    SecurityContext = sourceEnvelope.SecurityContext,
                                    Metadata = new EventMetadata { EventId = Guid.NewGuid() }
                                }));

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    It.IsAny<EventEnvelope<ApprovalComment>>(),
                    It.IsAny<string>(),
                    It.IsAny<EnvelopeDirection>()))
                        .ReturnsAsync(true);

            this.approvalCommentService = new ApprovalCommentService(
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
                new[] { Roles.Reviewer }
            };

        // the review roles a workflow record honors: the global three, plus — by the §16.6
        // suffix convention — any entity-scoped "{Entity}-Reviewer"/"{Entity}-Publisher"
        public static TheoryData<string> ReviewRoles() =>
            new TheoryData<string>
            {
                Roles.Reviewer,
                Roles.Publisher,
                Roles.Admin,
                Roles.ContentItemReviewer,
                Roles.ContentItemPublisher
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
                    new TimeoutApprovalCommentException(
                        message: "Failed approval comment timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data)
                },
                {
                    dbUpdateException,
                    new FailedStorageApprovalCommentException(
                        message: "Failed approval comment storage error occurred, contact support.",
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

            return new TheoryData<Exception, Xeption>
            {
                {
                    duplicateKeyException,
                    new AlreadyExistsApprovalCommentException(
                        message: "Approval comment already exists with the same Id.",
                        innerException: duplicateKeyException,
                        data: duplicateKeyException.Data)
                },
                {
                    foreignKeyConstraintConflictException,
                    new InvalidApprovalCommentReferenceException(
                        message: "Invalid approval comment reference error occurred.",
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
                    new LockedApprovalCommentException(
                        message: "Locked approval comment record, please try again later.",
                        innerException: dbUpdateConcurrencyException,
                        data: dbUpdateConcurrencyException.Data)
                },
                {
                    foreignKeyConstraintConflictException,
                    new InvalidApprovalCommentReferenceException(
                        message: "Invalid approval comment reference error occurred.",
                        innerException: foreignKeyConstraintConflictException,
                        data: foreignKeyConstraintConflictException.Data)
                }
            };
        }

        private static ApprovalComment CreateRandomApprovalComment() =>
            CreateApprovalCommentFiller(dateTimeOffset: GetRandomDateTimeOffset()).Create();

        private static EventEnvelope<ApprovalComment> CreateRandomApprovalCommentRequestEnvelope(
            SecurityContext? securityContext = null) =>
            new EventEnvelope<ApprovalComment>
            {
                Content = new ApprovalComment { Id = Guid.NewGuid() },
                SecurityContext = securityContext ?? CreateAuthenticatedSecurityContext(),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

        private static SecurityContext CreateAuthenticatedSecurityContext(params string[] roles) =>
            new SecurityContext
            {
                IsAuthenticated = true,
                Roles = roles
            };

        private static ApprovalComment CreateRandomModifyApprovalComment(
            DateTimeOffset dateTimeOffset,
            string userId = "")
        {
            int randomDaysInPast = GetRandomNegativeNumber();
            ApprovalComment randomApprovalComment = CreateApprovalCommentFiller(dateTimeOffset, userId).Create();
            randomApprovalComment.CreatedWhen = randomApprovalComment.CreatedWhen.AddDays(randomDaysInPast);

            return randomApprovalComment;
        }

        private static IQueryable<ApprovalComment> CreateRandomApprovalComments()
        {
            return CreateApprovalCommentFiller(dateTimeOffset: GetRandomDateTimeOffset())
                .Create(count: GetRandomNumber())
                .AsQueryable();
        }

        private static DateTimeOffset GetRandomDateTimeOffset() =>
            new DateTimeRange(earliestDate: new DateTime()).GetValue();

        private static Filler<ApprovalComment> CreateApprovalCommentFiller(
            DateTimeOffset dateTimeOffset,
            string userId = "")
        {
            userId = string.IsNullOrEmpty(userId) ? Guid.NewGuid().ToString() : userId;
            var filler = new Filler<ApprovalComment>();

            filler.Setup()
                .OnType<DateTimeOffset>().Use(dateTimeOffset)
                .OnType<DateTimeOffset?>().Use(dateTimeOffset)
                .OnProperty(approvalComment => approvalComment.Approval).IgnoreIt()
                // IsDeleted gates every read and remove path, so it is pinned here rather
                // than drawn: a posture-sensitive test must never depend on the draw. Tests
                // that want a soft-deleted row set it explicitly.
                .OnProperty(approvalComment => approvalComment.IsDeleted).Use(false)
                .OnProperty(approvalComment => approvalComment.CreatedBy).Use(userId)
                .OnProperty(approvalComment => approvalComment.UpdatedBy).Use(userId)

                // the commenter IS the caller, so a drawn value would fail the actor binding
                // on every add test rather than on the one test that is about it
                .OnProperty(approvalComment => approvalComment.UserId).Use(userId);

            return filler;
        }
    }
}
