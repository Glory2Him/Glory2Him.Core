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
using Glory2Him.Core.Brokers.EventEnvelopes;
using Glory2Him.Core.Brokers.Events;
using Glory2Him.Core.Brokers.Identifiers;
using Glory2Him.Core.Brokers.Integrities;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Brokers.Storages.Sql;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments;
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments.Exceptions;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Services.Foundations.AIReviewerAssignments;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Moq;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.AIReviewerAssignments
{
    public partial class AIReviewerAssignmentServiceTests
    {
        private readonly Mock<IStorageBroker> storageBrokerMock;
        private readonly Mock<IDateTimeBroker> dateTimeBrokerMock;
        private readonly Mock<IIdentifierBroker> identifierBrokerMock;
        private readonly Mock<IEventBroker> eventBrokerMock;
        private readonly Mock<IEventEnvelopeBroker> eventEnvelopeBrokerMock;
        private readonly Mock<ISecurityAuditBroker> securityAuditBrokerMock;
        private readonly Mock<IEnvelopeIntegrityBroker> envelopeIntegrityBrokerMock;
        private readonly Mock<ILoggingBroker> loggingBrokerMock;
        private readonly IAIReviewerAssignmentService aiReviewerAssignmentService;

        // The same instance through its workflow seam. Separate interfaces, one implementation —
        // the split exists to keep "act as the system" off the public surface the exposers bind
        // to, not to make two objects.
        private readonly IAIReviewerAssignmentWorkflowService aiReviewerAssignmentWorkflowService;

        // the ambient caller the envelope broker captures on the direct path — tests
        // override this field (before acting) to run as a different caller
        private SecurityContext ambientSecurityContext;

        // Whether CreateSystemAsync hands back a genuine system context. Always true in the real
        // broker; a test flips it to false to reach the system-identity guard, which the public
        // seam otherwise makes unreachable by minting the context itself.
        private bool systemContextIsGenuine;

        public AIReviewerAssignmentServiceTests()
        {
            this.storageBrokerMock = new Mock<IStorageBroker>();
            this.dateTimeBrokerMock = new Mock<IDateTimeBroker>();
            this.identifierBrokerMock = new Mock<IIdentifierBroker>();
            this.eventBrokerMock = new Mock<IEventBroker>();
            this.eventEnvelopeBrokerMock = new Mock<IEventEnvelopeBroker>();
            this.securityAuditBrokerMock = new Mock<ISecurityAuditBroker>();
            this.envelopeIntegrityBrokerMock = new Mock<IEnvelopeIntegrityBroker>();
            this.loggingBrokerMock = new Mock<ILoggingBroker>();

            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            this.systemContextIsGenuine = true;

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.IsAny<AIReviewerAssignment>()))
                    .Returns((AIReviewerAssignment content) =>
                        new ValueTask<EventEnvelope<AIReviewerAssignment>>(
                            new EventEnvelope<AIReviewerAssignment>
                            {
                                Content = content,
                                SecurityContext = this.ambientSecurityContext,
                                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
                            }));

            // The workflow's own path mints through here instead. Modelled the way the real
            // broker behaves: the caller's SubjectId is kept — the audit answer to "who caused
            // this" is a person — and the roles are DROPPED, so the system flag stands alone as
            // the authority. That dropping is the whole reason the return-to-pending transition
            // needs its own seam: a role-less context cannot pass the manage gate.
            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateSystemAsync(It.IsAny<AIReviewerAssignment>()))
                    .Returns((AIReviewerAssignment content) =>
                        new ValueTask<EventEnvelope<AIReviewerAssignment>>(
                            new EventEnvelope<AIReviewerAssignment>
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
                    It.IsAny<EventEnvelope<AIReviewerAssignment>>(),
                    It.IsAny<AIReviewerAssignment>()))
                        .Returns((
                            EventEnvelope<AIReviewerAssignment> sourceEnvelope,
                            AIReviewerAssignment content) =>
                                new ValueTask<EventEnvelope<AIReviewerAssignment>>(
                                    new EventEnvelope<AIReviewerAssignment>
                                    {
                                        Content = content,
                                        SecurityContext = sourceEnvelope.SecurityContext,
                                        Metadata = new EventMetadata { EventId = Guid.NewGuid() }
                                    }));

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    It.IsAny<EventEnvelope<AIReviewerAssignment>>(),
                    It.IsAny<string>(),
                    It.IsAny<EnvelopeDirection>()))
                        .ReturnsAsync(true);

            var aiReviewerAssignmentServiceInstance = new AIReviewerAssignmentService(
                storageBroker: this.storageBrokerMock.Object,
                dateTimeBroker: this.dateTimeBrokerMock.Object,
                identifierBroker: this.identifierBrokerMock.Object,
                eventBroker: this.eventBrokerMock.Object,
                eventEnvelopeBroker: this.eventEnvelopeBrokerMock.Object,
                securityAuditBroker: this.securityAuditBrokerMock.Object,
                envelopeIntegrityBroker: this.envelopeIntegrityBrokerMock.Object,
                loggingBroker: this.loggingBrokerMock.Object);

            this.aiReviewerAssignmentService = aiReviewerAssignmentServiceInstance;
            this.aiReviewerAssignmentWorkflowService = aiReviewerAssignmentServiceInstance;
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

        // The global review roles plus two scoped ones standing in for the §16.6
        // "%EntityType%-Reviewers"/"%EntityType%-Publishers" convention the foundation
        // recognizes by suffix. All of them may manage (add/modify/remove) and read an
        // assignment — there is no narrower nuance the way ApprovalReviewRequest's withdrawal
        // gate has, because there is no "requester" here for a different rule to ever apply to.
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
                    new TimeoutAIReviewerAssignmentException(
                        message: "Failed AI reviewer assignment timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data)
                },
                {
                    dbUpdateException,
                    new FailedStorageAIReviewerAssignmentException(
                        message: "Failed AI reviewer assignment storage error occurred, contact support.",
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
                    new AlreadyExistsAIReviewerAssignmentException(
                        message: "AI reviewer assignment already exists with the same Id.",
                        innerException: duplicateKeyException,
                        data: duplicateKeyException.Data)
                },
                {
                    foreignKeyConstraintConflictException,
                    new InvalidAIReviewerAssignmentReferenceException(
                        message: "Invalid AI reviewer assignment reference error occurred.",
                        innerException: foreignKeyConstraintConflictException,
                        data: foreignKeyConstraintConflictException.Data)
                },

                // The route UX_AIReviewerAssignments_ApprovalId travels: a second LIVE assignment
                // on the same approval trips it, which arrives as a unique-index violation rather
                // than a duplicate key.
                {
                    duplicateKeyWithUniqueIndexException,
                    new AlreadyExistsAIReviewerAssignmentException(
                        message: "AI reviewer assignment already exists, " +
                            "a uniqueness rule rejected the write.",
                        innerException: duplicateKeyWithUniqueIndexException,
                        data: duplicateKeyWithUniqueIndexException.Data)
                }
            };
        }

        // Modify routes through UpdateAIReviewerAssignmentAsync, so it can additionally raise a
        // concurrency conflict the Add-side DependencyValidationExceptions has no reason to cover.
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
                    new LockedAIReviewerAssignmentException(
                        message: "Locked AI reviewer assignment record, please try again later.",
                        innerException: dbUpdateConcurrencyException,
                        data: dbUpdateConcurrencyException.Data)
                },
                {
                    foreignKeyConstraintConflictException,
                    new InvalidAIReviewerAssignmentReferenceException(
                        message: "Invalid AI reviewer assignment reference error occurred.",
                        innerException: foreignKeyConstraintConflictException,
                        data: foreignKeyConstraintConflictException.Data)
                },
                {
                    duplicateKeyWithUniqueIndexException,
                    new AlreadyExistsAIReviewerAssignmentException(
                        message: "AI reviewer assignment already exists, " +
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
                    new LockedAIReviewerAssignmentException(
                        message: "Locked AI reviewer assignment record, please try again later.",
                        innerException: dbUpdateConcurrencyException,
                        data: dbUpdateConcurrencyException.Data)
                },
                {
                    foreignKeyConstraintConflictException,
                    new InvalidAIReviewerAssignmentReferenceException(
                        message: "Invalid AI reviewer assignment reference error occurred.",
                        innerException: foreignKeyConstraintConflictException,
                        data: foreignKeyConstraintConflictException.Data)
                }
            };
        }

        private static AIReviewerAssignment CreateRandomAIReviewerAssignment() =>
            CreateAIReviewerAssignmentFiller(dateTimeOffset: GetRandomDateTimeOffset()).Create();

        private static EventEnvelope<AIReviewerAssignment> CreateRandomAIReviewerAssignmentEnvelope(
            SecurityContext? securityContext = null) =>
            new EventEnvelope<AIReviewerAssignment>
            {
                Content = new AIReviewerAssignment { Id = Guid.NewGuid() },
                SecurityContext = securityContext ?? CreateAuthenticatedSecurityContext(Roles.Reviewers),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

        private static SecurityContext CreateAuthenticatedSecurityContext(params string[] roles) =>
            new SecurityContext
            {
                IsAuthenticated = true,
                Roles = roles
            };

        private static AIReviewerAssignment CreateRandomModifyAIReviewerAssignment(
            DateTimeOffset dateTimeOffset,
            string userId = "")
        {
            int randomDaysInPast = GetRandomNegativeNumber();

            AIReviewerAssignment randomAIReviewerAssignment =
                CreateAIReviewerAssignmentFiller(dateTimeOffset, userId).Create();

            randomAIReviewerAssignment.CreatedWhen =
                randomAIReviewerAssignment.CreatedWhen.AddDays(randomDaysInPast);

            return randomAIReviewerAssignment;
        }

        // Bounded well above DateTime.MinValue on purpose: arrangements shift these dates
        // backwards - AddDays(-n) for a stored row, AddSeconds(-90) for the recency window -
        // and a draw near the minimum makes that arithmetic throw. An unbounded earliest date
        // is an intermittently red suite, on whichever test happened to draw it.
        private static DateTimeOffset GetRandomDateTimeOffset() =>
            new DateTimeRange(earliestDate: new DateTime(year: 2000, month: 1, day: 1)).GetValue();

        private static Filler<AIReviewerAssignment> CreateAIReviewerAssignmentFiller(
            DateTimeOffset dateTimeOffset,
            string userId = "")
        {
            userId = string.IsNullOrEmpty(userId) ? Guid.NewGuid().ToString() : userId;
            var filler = new Filler<AIReviewerAssignment>();

            filler.Setup()
                .OnType<DateTimeOffset>().Use(dateTimeOffset)
                .OnType<DateTimeOffset?>().Use(dateTimeOffset)

                // IsDeleted gates every read and remove path, so it is pinned here rather than
                // drawn: a posture-sensitive test must never depend on the draw. Tests that want
                // a removed row set it explicitly.
                .OnProperty(aiReviewerAssignment => aiReviewerAssignment.IsDeleted).Use(false)

                // The two system-facing flags are pinned for the same reason and a sharper one:
                // an add refuses either of them already set, and add and modify alike refuse
                // comments-present on a row that says the pass never finished. A drawn pair would
                // red arrangements that are not about the flags at all, on whichever run happened
                // to draw it. Tests about the flags set them explicitly.
                .OnProperty(aiReviewerAssignment => aiReviewerAssignment.IsAIReviewCompleted).Use(false)
                .OnProperty(aiReviewerAssignment =>
                    aiReviewerAssignment.IsAIReviewCommentsPresent).Use(false)

                .OnProperty(aiReviewerAssignment => aiReviewerAssignment.CreatedBy).Use(userId)
                .OnProperty(aiReviewerAssignment => aiReviewerAssignment.UpdatedBy).Use(userId);

            return filler;
        }
    }
}
