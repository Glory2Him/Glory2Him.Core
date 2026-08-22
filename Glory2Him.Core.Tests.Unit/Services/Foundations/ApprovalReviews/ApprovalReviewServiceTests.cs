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
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.ApprovalReviews.Exceptions;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Services.Foundations.ApprovalReviews;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Moq;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalReviews
{
    public partial class ApprovalReviewServiceTests
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
        private readonly IApprovalReviewService approvalReviewService;

        // The same instance through its workflow seam. Separate interfaces, one
        // implementation — the split exists to keep "act as the system" off the
        // public surface the controllers bind to, not to make two objects.
        private readonly IApprovalReviewWorkflowService approvalReviewWorkflowService;
        private SecurityContext ambientSecurityContext;

        public ApprovalReviewServiceTests()
        {
            this.storageBrokerMock = new Mock<IStorageBroker>();
            this.dateTimeBrokerMock = new Mock<IDateTimeBroker>();
            this.identifierBrokerMock = new Mock<IIdentifierBroker>();
            this.eventBrokerMock = new Mock<IEventBroker>();
            this.eventEnvelopeBrokerMock = new Mock<IEventEnvelopeBroker>();
            this.securityAuditBrokerMock = new Mock<ISecurityAuditBroker>();
            this.accessBrokerMock = new Mock<IAccessBroker>();
            this.envelopeIntegrityBrokerMock = new Mock<IEnvelopeIntegrityBroker>();
            this.loggingBrokerMock = new Mock<ILoggingBroker>();

            // The cross-entity decision defaults to permitted, so a test about something else
            // exercises its own subject rather than failing on an unstubbed verdict. Reverse it
            // with SetupAccessBrokerToRefuse, which covers MayRecordApprovalReviewAsync — add,
            // modify and remove.
            //
            // There was a second one, for dismissal, and it is gone with the routes it guarded
            // (#295): no caller decides who may dismiss, because nobody may.
            SetupAccessBrokerToPermit();

            // the ambient caller the envelope broker captures on the direct path — tests
            // override this field (before acting) to run as a different caller
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.IsAny<ApprovalReview>()))
                    .Returns((ApprovalReview content) =>
                        new ValueTask<EventEnvelope<ApprovalReview>>(
                            new EventEnvelope<ApprovalReview>
                            {
                                Content = content,
                                SecurityContext = this.ambientSecurityContext,
                                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
                            }));

            // The workflow's own path mints through here instead. Modelled the way the real
            // broker behaves: the caller's SubjectId is kept — the audit answer to "who caused
            // this" is a person — and the roles are dropped, so the system flag stands alone as
            // the authority. A test that left roles on would pass without the flag doing
            // anything.
            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateSystemAsync(It.IsAny<ApprovalReview>()))
                    .Returns((ApprovalReview content) =>
                        new ValueTask<EventEnvelope<ApprovalReview>>(
                            new EventEnvelope<ApprovalReview>
                            {
                                Content = content,

                                SecurityContext = new SecurityContext
                                {
                                    IsAuthenticated = true,
                                    SubjectId = this.ambientSecurityContext?.SubjectId,
                                    Username = this.ambientSecurityContext?.Username,
                                    Roles = [],
                                    IsSystemIdentity = true
                                },

                                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
                            }));

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateNextAsync(
                    It.IsAny<EventEnvelope<ApprovalReview>>(),
                    It.IsAny<ApprovalReview>()))
                        .Returns((EventEnvelope<ApprovalReview> sourceEnvelope, ApprovalReview content) =>
                            new ValueTask<EventEnvelope<ApprovalReview>>(
                                new EventEnvelope<ApprovalReview>
                                {
                                    Content = content,
                                    SecurityContext = sourceEnvelope.SecurityContext,
                                    Metadata = new EventMetadata { EventId = Guid.NewGuid() }
                                }));

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    It.IsAny<EventEnvelope<ApprovalReview>>(),
                    It.IsAny<string>(),
                    It.IsAny<EnvelopeDirection>()))
                        .ReturnsAsync(true);

            var approvalReviewServiceInstance = new ApprovalReviewService(
                storageBroker: this.storageBrokerMock.Object,
                dateTimeBroker: this.dateTimeBrokerMock.Object,
                identifierBroker: this.identifierBrokerMock.Object,
                eventBroker: this.eventBrokerMock.Object,
                eventEnvelopeBroker: this.eventEnvelopeBrokerMock.Object,
                securityAuditBroker: this.securityAuditBrokerMock.Object,
                accessBroker: this.accessBrokerMock.Object,
                envelopeIntegrityBroker: this.envelopeIntegrityBrokerMock.Object,
                loggingBroker: this.loggingBrokerMock.Object);

            this.approvalReviewService = approvalReviewServiceInstance;
            this.approvalReviewWorkflowService = approvalReviewServiceInstance;
        }

        private void SetupAccessBrokerToPermit()
        {
            this.accessBrokerMock.Setup(broker =>
                broker.MayRecordApprovalReviewAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<bool>(),
                    It.IsAny<SecurityContext>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(CreatePermittedVerdict());
        }

        private static AccessVerdict CreatePermittedVerdict() =>
            new AccessVerdict
            {
                IsPermitted = true,
                DenialReason = AccessDenialReason.None,
                IsBypassUsed = false,
                BypassedBlockReason = AccessDenialReason.None,
                Explanation = "permitted",
            };


        // A permit reached by WAIVING the conditions rather than by meeting them. Recording a
        // review never takes this route today — HR-1 has no bypass — but the verdict type is
        // shared, so the fixture can state the shape a bypassing verdict has.
        private void SetupAccessBrokerToPermitByBypass(
            AccessDenialReason bypassedBlockReason) =>
            this.accessBrokerMock.Setup(broker =>
                broker.MayRecordApprovalReviewAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<bool>(),
                    It.IsAny<SecurityContext>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new AccessVerdict
                        {
                            IsPermitted = true,
                            DenialReason = AccessDenialReason.None,
                            IsBypassUsed = true,
                            BypassedBlockReason = bypassedBlockReason,
                            Explanation = "permitted by bypass",
                        });

        // the reverse of the fixture default, for tests about what the service does when the
        // access decision refuses
        private void SetupAccessBrokerToRefuse(AccessDenialReason reason) =>
            this.accessBrokerMock.Setup(broker =>
                broker.MayRecordApprovalReviewAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<bool>(),
                    It.IsAny<SecurityContext>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new AccessVerdict
                        {
                            IsPermitted = false,
                            DenialReason = reason,
                            IsBypassUsed = false,
                            BypassedBlockReason = AccessDenialReason.None,
                            Explanation = "refused",
                        });

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

        // the global review roles plus two scoped ones standing in for the §16.6
        // "%EntityType%-Reviewer"/"%EntityType%-Publisher" convention the foundation
        // recognizes by suffix
        public static TheoryData<string> ReviewRoles() =>
            new TheoryData<string>
            {
                Roles.Reviewer,
                Roles.Publisher,
                Roles.Admin,
                Roles.ContentItemReviewer,
                Roles.TagPublisher
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
                    new TimeoutApprovalReviewException(
                        message: "Failed approval review timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data)
                },
                {
                    dbUpdateException,
                    new FailedStorageApprovalReviewException(
                        message: "Failed approval review storage error occurred, contact support.",
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
                    new AlreadyExistsApprovalReviewException(
                        message: "Approval review already exists with the same Id.",
                        innerException: duplicateKeyException,
                        data: duplicateKeyException.Data)
                },
                {
                    foreignKeyConstraintConflictException,
                    new InvalidApprovalReviewReferenceException(
                        message: "Invalid approval review reference error occurred.",
                        innerException: foreignKeyConstraintConflictException,
                        data: foreignKeyConstraintConflictException.Data)
                },
                {
                    duplicateKeyWithUniqueIndexException,
                    new AlreadyExistsApprovalReviewException(
                        message: "Approval review already exists, " +
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
                    new LockedApprovalReviewException(
                        message: "Locked approval review record, please try again later.",
                        innerException: dbUpdateConcurrencyException,
                        data: dbUpdateConcurrencyException.Data)
                },
                {
                    foreignKeyConstraintConflictException,
                    new InvalidApprovalReviewReferenceException(
                        message: "Invalid approval review reference error occurred.",
                        innerException: foreignKeyConstraintConflictException,
                        data: foreignKeyConstraintConflictException.Data)
                },
                {
                    duplicateKeyWithUniqueIndexException,
                    new AlreadyExistsApprovalReviewException(
                        message: "Approval review already exists, " +
                            "a uniqueness rule rejected the write.",
                        innerException: duplicateKeyWithUniqueIndexException,
                        data: duplicateKeyWithUniqueIndexException.Data)
                }
            };
        }

        private static ApprovalReview CreateRandomApprovalReview() =>
            CreateApprovalReviewFiller(dateTimeOffset: GetRandomDateTimeOffset()).Create();

        private static EventEnvelope<ApprovalReview> CreateRandomApprovalReviewRequestEnvelope(
            SecurityContext? securityContext = null) =>
            new EventEnvelope<ApprovalReview>
            {
                Content = new ApprovalReview { Id = Guid.NewGuid() },
                SecurityContext = securityContext ?? CreateAuthenticatedSecurityContext(),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

        private static SecurityContext CreateAuthenticatedSecurityContext(params string[] roles) =>
            new SecurityContext
            {
                IsAuthenticated = true,
                Roles = roles
            };

        private static ApprovalReview CreateRandomModifyApprovalReview(
            DateTimeOffset dateTimeOffset,
            string userId = "")
        {
            int randomDaysInPast = GetRandomNegativeNumber();
            ApprovalReview randomApprovalReview = CreateApprovalReviewFiller(dateTimeOffset, userId).Create();
            randomApprovalReview.CreatedWhen = randomApprovalReview.CreatedWhen.AddDays(randomDaysInPast);

            return randomApprovalReview;
        }

        private static IQueryable<ApprovalReview> CreateRandomApprovalReviews()
        {
            return CreateApprovalReviewFiller(dateTimeOffset: GetRandomDateTimeOffset())
                .Create(count: GetRandomNumber())
                .AsQueryable();
        }

        private static DateTimeOffset GetRandomDateTimeOffset() =>
            new DateTimeRange(earliestDate: new DateTime()).GetValue();

        private static Filler<ApprovalReview> CreateApprovalReviewFiller(
            DateTimeOffset dateTimeOffset,
            string userId = "")
        {
            userId = string.IsNullOrEmpty(userId) ? Guid.NewGuid().ToString() : userId;
            var filler = new Filler<ApprovalReview>();

            filler.Setup()
                .OnType<DateTimeOffset>().Use(dateTimeOffset)
                .OnType<DateTimeOffset?>().Use(dateTimeOffset)
                .OnProperty(approvalReview => approvalReview.Approval).IgnoreIt()
                // IsDeleted gates every read and remove path, so it is pinned here rather
                // than drawn: a posture-sensitive test must never depend on the draw. Tests
                // that want a soft-deleted row set it explicitly.
                .OnProperty(approvalReview => approvalReview.IsDeleted).Use(false)

                // A review carries a verdict and the service refuses anything else, so a
                // drawn ApprovalStatus would fail every write test on the draw rather than on
                // what it is testing. Tests about the closed set say the status explicitly.
                .OnProperty(approvalReview => approvalReview.StatusId).Use(ApprovalStatus.Approved)
                // CreatedBy IS the caller and, since ReviewerId/UserId were dropped, carries
                // the reviewer identity on its own — a drawn value would fail the actor binding
                // on every add test rather than on the one test that is about it
                .OnProperty(approvalReview => approvalReview.CreatedBy).Use(userId)
                .OnProperty(approvalReview => approvalReview.UpdatedBy).Use(userId);

            return filler;
        }
    }
}
