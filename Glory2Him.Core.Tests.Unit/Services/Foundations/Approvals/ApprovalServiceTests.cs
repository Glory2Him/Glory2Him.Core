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
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Foundations.Approvals.Exceptions;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Services.Foundations.Approvals;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Moq;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Approvals
{
    public partial class ApprovalServiceTests
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
        private readonly IApprovalService approvalService;

        // The SAME instance behind its narrower door, so a test can drive the workflow path
        // without a second object (#287).
        private readonly IApprovalWorkflowService approvalWorkflowService;
        private SecurityContext ambientSecurityContext;

        public ApprovalServiceTests()
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

            // the ambient caller the envelope broker captures on the direct path — tests
            // override this field (before acting) to run as a different caller
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.IsAny<Approval>()))
                    .Returns((Approval content) =>
                        new ValueTask<EventEnvelope<Approval>>(
                            new EventEnvelope<Approval>
                            {
                                Content = content,
                                SecurityContext = this.ambientSecurityContext,
                                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
                            }));

            // The workflow's own path mints through one of these two instead (#287). Modelled
            // the way the real broker behaves, and the difference between them is the whole
            // point: an act nobody asked for records SystemIdentity, an act carried out for a
            // person records the person. Both drop roles, so the system flag stands alone as the
            // authority — a test that left roles on would pass without the flag doing anything.
            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateSystemAsync(It.IsAny<Approval>()))
                    .Returns((Approval content) =>
                        new ValueTask<EventEnvelope<Approval>>(
                            new EventEnvelope<Approval>
                            {
                                Content = content,

                                SecurityContext = new SecurityContext
                                {
                                    IsAuthenticated = true,
                                    SubjectId = SystemIdentity.UserId,
                                    Username = SystemIdentity.Username,
                                    DelegatedBySubjectId = this.ambientSecurityContext?.SubjectId,
                                    Roles = [],
                                    IsSystemIdentity = true,
                                    AuthenticationType = AuthenticationType.System
                                },

                                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
                            }));

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateElevatedAsync(It.IsAny<Approval>()))
                    .Returns((Approval content) =>
                        new ValueTask<EventEnvelope<Approval>>(
                            new EventEnvelope<Approval>
                            {
                                Content = content,

                                SecurityContext = new SecurityContext
                                {
                                    IsAuthenticated = true,
                                    SubjectId = this.ambientSecurityContext?.SubjectId,
                                    Username = this.ambientSecurityContext?.Username,
                                    DelegatedBySubjectId = this.ambientSecurityContext?.SubjectId,
                                    Roles = [],
                                    IsSystemIdentity = true,
                                    AuthenticationType = AuthenticationType.Delegated
                                },

                                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
                            }));

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateNextAsync(
                    It.IsAny<EventEnvelope<Approval>>(),
                    It.IsAny<Approval>()))
                        .Returns((EventEnvelope<Approval> sourceEnvelope, Approval content) =>
                            new ValueTask<EventEnvelope<Approval>>(
                                new EventEnvelope<Approval>
                                {
                                    Content = content,
                                    SecurityContext = sourceEnvelope.SecurityContext,
                                    Metadata = new EventMetadata { EventId = Guid.NewGuid() }
                                }));

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    It.IsAny<EventEnvelope<Approval>>(),
                    It.IsAny<string>(),
                    It.IsAny<EnvelopeDirection>()))
                        .ReturnsAsync(true);

            // The cross-entity amendment decision defaults to permitted so a test about something
            // else exercises its own subject rather than failing on an unstubbed verdict. Tests
            // about the gate itself call SetupAccessBrokerToRefuseAmendment to reverse it.
            SetupAccessBrokerToPermitAmendment();

            var approvalServiceInstance = new ApprovalService(
                storageBroker: this.storageBrokerMock.Object,
                dateTimeBroker: this.dateTimeBrokerMock.Object,
                identifierBroker: this.identifierBrokerMock.Object,
                eventBroker: this.eventBrokerMock.Object,
                eventEnvelopeBroker: this.eventEnvelopeBrokerMock.Object,
                securityAuditBroker: this.securityAuditBrokerMock.Object,
                accessBroker: this.accessBrokerMock.Object,
                envelopeIntegrityBroker: this.envelopeIntegrityBrokerMock.Object,
                loggingBroker: this.loggingBrokerMock.Object);

            this.approvalService = approvalServiceInstance;
            this.approvalWorkflowService = approvalServiceInstance;
        }

        private void SetupAccessBrokerToPermitAmendment() =>
            this.accessBrokerMock.Setup(broker =>
                broker.MayAmendApprovalAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<SecurityContext>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new AccessVerdict
                        {
                            IsPermitted = true,
                            DenialReason = AccessDenialReason.None,
                            IsBypassUsed = false,
                            BypassedBlockReason = AccessDenialReason.None,
                            Explanation = "permitted",
                        });

        /// <summary>
        /// Mirrors the real decision instead of blanket-permitting: owner OR review tier. A
        /// default that permits everything cannot tell a gate that admits the submitter from one
        /// that does not, which is exactly the defect this suite failed to catch once already.
        /// </summary>
        private void SetupAccessBrokerToMirrorTheAmendmentDecision(
            string approvalCreatedBy,
            string actorUserId) =>
            this.accessBrokerMock.Setup(broker =>
                broker.MayAmendApprovalAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<SecurityContext>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Guid _, SecurityContext securityContext, CancellationToken _) =>
                        {
                            // the ACTOR id as the audit surface resolves it, which is what the
                            // real broker forwards — not SecurityContext.SubjectId
                            bool isOwner =
                                string.IsNullOrWhiteSpace(approvalCreatedBy) is false
                                    && approvalCreatedBy == actorUserId;

                            bool hasReviewTier = securityContext.Roles.Any(role =>
                                role == Roles.Reviewer
                                    || role == Roles.Publisher
                                    || role == Roles.Admin
                                    || role.EndsWith(Roles.ReviewerSuffix, StringComparison.Ordinal)
                                    || role.EndsWith(Roles.PublisherSuffix, StringComparison.Ordinal));

                            return isOwner || hasReviewTier
                                ? CreatePermittedAmendmentVerdict()
                                : new AccessVerdict
                                {
                                    IsPermitted = false,
                                    DenialReason = AccessDenialReason.NotInReviewTier,
                                    IsBypassUsed = false,
                                    BypassedBlockReason = AccessDenialReason.None,
                                    Explanation = "neither the submitter nor in the review tier",
                                };
                        });

        private static AccessVerdict CreatePermittedAmendmentVerdict() =>
            new AccessVerdict
            {
                IsPermitted = true,
                DenialReason = AccessDenialReason.None,
                IsBypassUsed = false,
                BypassedBlockReason = AccessDenialReason.None,
                Explanation = "permitted",
            };

        private void SetupAccessBrokerToRefuseAmendment(AccessDenialReason denialReason) =>
            this.accessBrokerMock.Setup(broker =>
                broker.MayAmendApprovalAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<SecurityContext>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new AccessVerdict
                        {
                            IsPermitted = false,
                            DenialReason = denialReason,
                            IsBypassUsed = false,
                            BypassedBlockReason = AccessDenialReason.None,
                            Explanation = "the actor is not in the review tier for this entity",
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

        // the global review roles plus two §16.6 scoped roles, which qualify by their
        // "-Reviewer"/"-Publisher" suffix whatever entity type they are scoped to
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
                    new TimeoutApprovalException(
                        message: "Failed approval timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data)
                },
                {
                    dbUpdateException,
                    new FailedStorageApprovalException(
                        message: "Failed approval storage error occurred, contact support.",
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
                    new AlreadyExistsApprovalException(
                        message: "Approval already exists with the same Id.",
                        innerException: duplicateKeyException,
                        data: duplicateKeyException.Data)
                },
                {
                    foreignKeyConstraintConflictException,
                    new InvalidApprovalReferenceException(
                        message: "Invalid approval reference error occurred.",
                        innerException: foreignKeyConstraintConflictException,
                        data: foreignKeyConstraintConflictException.Data)
                },
                {
                    duplicateKeyWithUniqueIndexException,
                    new AlreadyExistsApprovalException(
                        message: "Approval already exists, " +
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
                    new LockedApprovalException(
                        message: "Locked approval record, please try again later.",
                        innerException: dbUpdateConcurrencyException,
                        data: dbUpdateConcurrencyException.Data)
                },
                {
                    foreignKeyConstraintConflictException,
                    new InvalidApprovalReferenceException(
                        message: "Invalid approval reference error occurred.",
                        innerException: foreignKeyConstraintConflictException,
                        data: foreignKeyConstraintConflictException.Data)
                },
                {
                    duplicateKeyWithUniqueIndexException,
                    new AlreadyExistsApprovalException(
                        message: "Approval already exists, " +
                            "a uniqueness rule rejected the write.",
                        innerException: duplicateKeyWithUniqueIndexException,
                        data: duplicateKeyWithUniqueIndexException.Data)
                }
            };
        }

        private static Approval CreateRandomApproval() =>
            CreateApprovalFiller(dateTimeOffset: GetRandomDateTimeOffset()).Create();

        private static EventEnvelope<Approval> CreateRandomApprovalRequestEnvelope(
            SecurityContext? securityContext = null) =>
            new EventEnvelope<Approval>
            {
                Content = new Approval { Id = Guid.NewGuid() },
                SecurityContext = securityContext ?? CreateAuthenticatedSecurityContext(),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

        private static SecurityContext CreateAuthenticatedSecurityContext(params string[] roles) =>
            new SecurityContext
            {
                IsAuthenticated = true,
                Roles = roles
            };

        private static Approval CreateRandomModifyApproval(
            DateTimeOffset dateTimeOffset,
            string userId = "")
        {
            int randomDaysInPast = GetRandomNegativeNumber();
            Approval randomApproval = CreateApprovalFiller(dateTimeOffset, userId).Create();
            randomApproval.CreatedWhen = randomApproval.CreatedWhen.AddDays(randomDaysInPast);

            return randomApproval;
        }

        private static IQueryable<Approval> CreateRandomApprovals()
        {
            return CreateApprovalFiller(dateTimeOffset: GetRandomDateTimeOffset())
                .Create(count: GetRandomNumber())
                .AsQueryable();
        }

        private static DateTimeOffset GetRandomDateTimeOffset() =>
            new DateTimeRange(earliestDate: new DateTime()).GetValue();

        private static Filler<Approval> CreateApprovalFiller(
            DateTimeOffset dateTimeOffset,
            string userId = "")
        {
            userId = string.IsNullOrEmpty(userId) ? Guid.NewGuid().ToString() : userId;
            var filler = new Filler<Approval>();

            filler.Setup()
                .OnType<DateTimeOffset>().Use(dateTimeOffset)
                .OnType<DateTimeOffset?>().Use(dateTimeOffset)
                .OnProperty(approval => approval.ApprovalComments).IgnoreIt()
                .OnProperty(approval => approval.ApprovalReviews).IgnoreIt()
                // IsDeleted gates every read and remove path, so it is pinned here rather
                // than drawn: a posture-sensitive test must never depend on the draw. Tests
                // that want a soft-deleted row set it explicitly.
                .OnProperty(approval => approval.IsDeleted).Use(false)

                // An approval is born undecided, and add now refuses anything else: outcome
                // statuses arrive only through the modify-side decision gate, and the bypass
                // pair only as that gate's derived verdict. Pinned rather than drawn for the
                // same reason as IsDeleted — a random Approved or a random true flag would make
                // every add test's outcome depend on the draw. Tests that want a decided row or
                // a recorded waiver set them explicitly.
                .OnProperty(approval => approval.ApprovalStatus).Use(ApprovalStatus.Submitted)
                .OnProperty(approval => approval.IsApprovedByBypass).Use(false)
                .OnProperty(approval => approval.ApprovedByBypassReason).Use((string)null)

                .OnProperty(approval => approval.CreatedBy).Use(userId)
                .OnProperty(approval => approval.UpdatedBy).Use(userId);

            return filler;
        }
    }
}
