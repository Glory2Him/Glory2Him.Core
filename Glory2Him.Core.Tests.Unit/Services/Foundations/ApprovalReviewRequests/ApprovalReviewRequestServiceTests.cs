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
using System.Threading;
using System.Threading.Tasks;
using EFxceptions.Models.Exceptions;
using Glory2Him.Core.Brokers.DateTimes;
using Glory2Him.Core.Brokers.EventEnvelopes;
using Glory2Him.Core.Brokers.Events;
using Glory2Him.Core.Brokers.Identifiers;
using Glory2Him.Core.Brokers.Integrities;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Brokers.Storages.Sql;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests.Exceptions;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Services.Foundations.ApprovalReviewRequests;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Moq;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalReviewRequests
{
    public partial class ApprovalReviewRequestServiceTests
    {
        private readonly Mock<IStorageBroker> storageBrokerMock;
        private readonly Mock<IDateTimeBroker> dateTimeBrokerMock;
        private readonly Mock<IIdentifierBroker> identifierBrokerMock;
        private readonly Mock<IEventBroker> eventBrokerMock;
        private readonly Mock<IEventEnvelopeBroker> eventEnvelopeBrokerMock;
        private readonly Mock<ISecurityAuditBroker> securityAuditBrokerMock;
        private readonly Mock<IEnvelopeIntegrityBroker> envelopeIntegrityBrokerMock;
        private readonly Mock<ILoggingBroker> loggingBrokerMock;
        private readonly IApprovalReviewRequestService approvalReviewRequestService;

        // The same instance through its workflow seam. Separate interfaces, one implementation —
        // the split exists to keep "act as the system" off the public surface the exposers bind
        // to, not to make two objects.
        private readonly IApprovalReviewRequestWorkflowService approvalReviewRequestWorkflowService;

        // the ambient caller the envelope broker captures on the direct path — tests
        // override this field (before acting) to run as a different caller
        private SecurityContext ambientSecurityContext;

        // Whether CreateSystemAsync hands back a genuine system context. Always true in the real
        // broker; a test flips it to false to reach the system-identity guard, which the public
        // seam otherwise makes unreachable by minting the context itself.
        private bool systemContextIsGenuine;

        // There is deliberately no IAccessBroker here, and its absence is the fixture stating a
        // design fact rather than an omission: an invitation grants nothing and enters no §8.5
        // condition, so this service has no cross-entity invariant to defend. The rules that DO
        // need the parent approval — the target's eligibility, the Submitted window, the
        // idempotent dismiss of a duplicate — belong to ApprovalOrchestrationService (§16.7.4)
        // and are tested there.
        public ApprovalReviewRequestServiceTests()
        {
            this.storageBrokerMock = new Mock<IStorageBroker>();
            this.dateTimeBrokerMock = new Mock<IDateTimeBroker>();
            this.identifierBrokerMock = new Mock<IIdentifierBroker>();
            this.eventBrokerMock = new Mock<IEventBroker>();
            this.eventEnvelopeBrokerMock = new Mock<IEventEnvelopeBroker>();
            this.securityAuditBrokerMock = new Mock<ISecurityAuditBroker>();
            this.envelopeIntegrityBrokerMock = new Mock<IEnvelopeIntegrityBroker>();
            this.loggingBrokerMock = new Mock<ILoggingBroker>();

            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            this.systemContextIsGenuine = true;

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.IsAny<ApprovalReviewRequest>()))
                    .Returns((ApprovalReviewRequest content) =>
                        new ValueTask<EventEnvelope<ApprovalReviewRequest>>(
                            new EventEnvelope<ApprovalReviewRequest>
                            {
                                Content = content,
                                SecurityContext = this.ambientSecurityContext,
                                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
                            }));

            // The workflow's own path mints through here instead. Modelled the way the real
            // broker behaves: the caller's SubjectId is kept — the audit answer to "who caused
            // this" is a person — and the roles are DROPPED, so the system flag stands alone as
            // the authority. That dropping is the whole reason the retirement needs its own seam:
            // a role-less context cannot pass the withdraw gate.
            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateSystemAsync(It.IsAny<ApprovalReviewRequest>()))
                    .Returns((ApprovalReviewRequest content) =>
                        new ValueTask<EventEnvelope<ApprovalReviewRequest>>(
                            new EventEnvelope<ApprovalReviewRequest>
                            {
                                Content = content,

                                SecurityContext = new SecurityContext
                                {
                                    IsAuthenticated = true,
                                    SubjectId = this.ambientSecurityContext?.SubjectId,
                                    Username = this.ambientSecurityContext?.Username,
                                    Roles = [],
                                    IsSystemIdentity = this.systemContextIsGenuine
                                },

                                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
                            }));

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateNextAsync(
                    It.IsAny<EventEnvelope<ApprovalReviewRequest>>(),
                    It.IsAny<ApprovalReviewRequest>()))
                        .Returns((
                            EventEnvelope<ApprovalReviewRequest> sourceEnvelope,
                            ApprovalReviewRequest content) =>
                                new ValueTask<EventEnvelope<ApprovalReviewRequest>>(
                                    new EventEnvelope<ApprovalReviewRequest>
                                    {
                                        Content = content,
                                        SecurityContext = sourceEnvelope.SecurityContext,
                                        Metadata = new EventMetadata { EventId = Guid.NewGuid() }
                                    }));

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    It.IsAny<EventEnvelope<ApprovalReviewRequest>>(),
                    It.IsAny<string>(),
                    It.IsAny<EnvelopeDirection>()))
                        .ReturnsAsync(true);

            var approvalReviewRequestServiceInstance = new ApprovalReviewRequestService(
                storageBroker: this.storageBrokerMock.Object,
                dateTimeBroker: this.dateTimeBrokerMock.Object,
                identifierBroker: this.identifierBrokerMock.Object,
                eventBroker: this.eventBrokerMock.Object,
                eventEnvelopeBroker: this.eventEnvelopeBrokerMock.Object,
                securityAuditBroker: this.securityAuditBrokerMock.Object,
                envelopeIntegrityBroker: this.envelopeIntegrityBrokerMock.Object,
                loggingBroker: this.loggingBrokerMock.Object);

            this.approvalReviewRequestService = approvalReviewRequestServiceInstance;
            this.approvalReviewRequestWorkflowService = approvalReviewRequestServiceInstance;
        }

        private static Expression<Func<Xeption, bool>> SameExceptionAs(Xeption expectedException) =>
            actualException => actualException.SameExceptionAs(expectedException);

        private static SqlException GetSqlException() =>
            (SqlException)RuntimeHelpers.GetUninitializedObject(typeof(SqlException));

        private static string GetRandomString() =>
            new MnemonicString(wordCount: GetRandomNumber()).GetValue();

        private static string GetRandomStringWithLengthOf(int length)
        {
            string result =
                new MnemonicString(wordCount: 1, wordMinLength: length, wordMaxLength: length).GetValue();

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

        // The global review roles plus two scoped ones standing in for the §16.6
        // "%EntityType%-Reviewers"/"%EntityType%-Publishers" convention the foundation recognizes
        // by suffix. All of them may issue AND withdraw an invitation (§7.9 rules 2 and 5).
        public static TheoryData<string> ReviewRoles() =>
            new TheoryData<string>
            {
                Roles.Reviewers,
                Roles.Publishers,
                Roles.Administrators,
                Roles.ContentItemReviewers,
                Roles.TagPublishers
            };

        public static TheoryData<string[]> NonReviewRoleSets() =>
            new TheoryData<string[]>
            {
                new string[0],
                new[] { Roles.ContentItemReadOnly }
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
                    new TimeoutApprovalReviewRequestException(
                        message: "Failed approval review request timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data)
                },
                {
                    dbUpdateException,
                    new FailedStorageApprovalReviewRequestException(
                        message: "Failed approval review request storage error occurred, contact support.",
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
                    new AlreadyExistsApprovalReviewRequestException(
                        message: "Approval review request already exists with the same Id.",
                        innerException: duplicateKeyException,
                        data: duplicateKeyException.Data)
                },
                {
                    foreignKeyConstraintConflictException,
                    new InvalidApprovalReviewRequestReferenceException(
                        message: "Invalid approval review request reference error occurred.",
                        innerException: foreignKeyConstraintConflictException,
                        data: foreignKeyConstraintConflictException.Data)
                },

                // The route §7.9 rule 1 travels: a second ACTIVE invitation for the same person
                // on the same approval trips UX_ApprovalReviewRequests_ApprovalId_RequestedUserId,
                // which arrives as a unique-index violation rather than a duplicate key.
                {
                    duplicateKeyWithUniqueIndexException,
                    new AlreadyExistsApprovalReviewRequestException(
                        message: "Approval review request already exists, " +
                            "a uniqueness rule rejected the write.",
                        innerException: duplicateKeyWithUniqueIndexException,
                        data: duplicateKeyWithUniqueIndexException.Data)
                }
            };
        }

        public static TheoryData<Exception, Xeption> RemoveDependencyValidationExceptions()
        {
            string someMessage = GetRandomString();
            var dbUpdateConcurrencyException = new DbUpdateConcurrencyException();
            var foreignKeyConstraintConflictException = new ForeignKeyConstraintConflictException(someMessage);

            return new TheoryData<Exception, Xeption>
            {
                {
                    dbUpdateConcurrencyException,
                    new LockedApprovalReviewRequestException(
                        message: "Locked approval review request record, please try again later.",
                        innerException: dbUpdateConcurrencyException,
                        data: dbUpdateConcurrencyException.Data)
                },
                {
                    foreignKeyConstraintConflictException,
                    new InvalidApprovalReviewRequestReferenceException(
                        message: "Invalid approval review request reference error occurred.",
                        innerException: foreignKeyConstraintConflictException,
                        data: foreignKeyConstraintConflictException.Data)
                }
            };
        }

        private static ApprovalReviewRequest CreateRandomApprovalReviewRequest() =>
            CreateApprovalReviewRequestFiller(dateTimeOffset: GetRandomDateTimeOffset()).Create();

        private static EventEnvelope<ApprovalReviewRequest> CreateRandomApprovalReviewRequestEnvelope(
            SecurityContext? securityContext = null) =>
            new EventEnvelope<ApprovalReviewRequest>
            {
                Content = new ApprovalReviewRequest { Id = Guid.NewGuid() },
                SecurityContext = securityContext ?? CreateAuthenticatedSecurityContext(Roles.Reviewers),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

        private static SecurityContext CreateAuthenticatedSecurityContext(params string[] roles) =>
            new SecurityContext
            {
                IsAuthenticated = true,
                Roles = roles
            };

        private static IQueryable<ApprovalReviewRequest> CreateRandomApprovalReviewRequests() =>
            CreateApprovalReviewRequestFiller(dateTimeOffset: GetRandomDateTimeOffset())
                .Create(count: GetRandomNumber())
                .AsQueryable();

        // Bounded well above DateTime.MinValue on purpose: arrangements shift these dates
        // backwards - AddDays(-n) for a stored row, AddSeconds(-90) for the recency window -
        // and a draw near the minimum makes that arithmetic throw. An unbounded earliest date
        // is an intermittently red suite, on whichever test happened to draw it.
        private static DateTimeOffset GetRandomDateTimeOffset() =>
            new DateTimeRange(earliestDate: new DateTime(year: 2000, month: 1, day: 1)).GetValue();

        private static Filler<ApprovalReviewRequest> CreateApprovalReviewRequestFiller(
            DateTimeOffset dateTimeOffset,
            string userId = "")
        {
            userId = string.IsNullOrEmpty(userId) ? Guid.NewGuid().ToString() : userId;
            var filler = new Filler<ApprovalReviewRequest>();

            filler.Setup()
                .OnType<DateTimeOffset>().Use(dateTimeOffset)
                .OnType<DateTimeOffset?>().Use(dateTimeOffset)
                .OnProperty(approvalReviewRequest => approvalReviewRequest.Approval).IgnoreIt()

                // IsDeleted gates every read and withdraw path, so it is pinned here rather than
                // drawn: a posture-sensitive test must never depend on the draw. Tests that want
                // a withdrawn row set it explicitly.
                .OnProperty(approvalReviewRequest => approvalReviewRequest.IsDeleted).Use(false)

                // CreatedBy IS the requester and must equal the acting user, so a drawn value
                // would fail the actor binding on every add test rather than on the one test
                // that is about it.
                .OnProperty(approvalReviewRequest => approvalReviewRequest.CreatedBy).Use(userId)
                .OnProperty(approvalReviewRequest => approvalReviewRequest.UpdatedBy).Use(userId);

            return filler;
        }
    }
}
