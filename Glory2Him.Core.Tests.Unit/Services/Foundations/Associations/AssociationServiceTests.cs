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
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.Associations.Exceptions;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Services.Foundations.Associations;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Moq;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Associations
{
    public partial class AssociationServiceTests
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
        private readonly IAssociationService associationService;
        private SecurityContext ambientSecurityContext;

        public AssociationServiceTests()
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

            // the cross-entity approval decision defaults to permitted so a test about
            // something else exercises its own subject rather than failing on an
            // unstubbed verdict. Tests about the gate itself call
            // SetupAccessBrokerToRefuse to reverse it.
            SetupAccessBrokerToPermit();

            // the ambient caller the envelope broker captures on the direct path — tests
            // override this field (before acting) to run as a different caller
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.IsAny<Association>()))
                    .Returns((Association content) =>
                        new ValueTask<EventEnvelope<Association>>(
                            new EventEnvelope<Association>
                            {
                                Content = content,
                                SecurityContext = this.ambientSecurityContext,
                                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
                            }));

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateNextAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    It.IsAny<Association>()))
                        .Returns((
                            EventEnvelope<Association> sourceEnvelope,
                            Association content) =>
                            new ValueTask<EventEnvelope<Association>>(
                                new EventEnvelope<Association>
                                {
                                    Content = content,
                                    SecurityContext = sourceEnvelope.SecurityContext,
                                    Metadata = new EventMetadata { EventId = Guid.NewGuid() }
                                }));

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    It.IsAny<string>(),
                    It.IsAny<EnvelopeDirection>()))
                        .ReturnsAsync(true);

            this.associationService = new AssociationService(
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

        private void SetupAccessBrokerToPermit() =>
            this.accessBrokerMock.Setup(broker =>
                broker.MayDecideApprovalAsync(
                    It.IsAny<ApprovalDecisionQuery>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new AccessVerdict
                        {
                            IsPermitted = true,
                            DenialReason = AccessDenialReason.None,
                            IsBypassUsed = false,
                            BypassedBlockReason = AccessDenialReason.None,
                            Explanation = "permitted",
                        });

        // A permit reached by WAIVING the conditions rather than by meeting them. DenialReason
        // stays None on purpose: a bypass is a permission, and every gate in the service reads
        // `IsPermitted` and then the reason — a verdict that reported the waived block as a
        // denial reason would refuse the very approve it just permitted.
        private void SetupAccessBrokerToPermitByBypass(
            AccessDenialReason bypassedBlockReason) =>
            this.accessBrokerMock.Setup(broker =>
                broker.MayDecideApprovalAsync(
                    It.IsAny<ApprovalDecisionQuery>(),
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
                broker.MayDecideApprovalAsync(
                    It.IsAny<ApprovalDecisionQuery>(),
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

        // the score is decimal(4,2), so the draw exercises the fractional precision rather
        // than only the whole numbers the old int column could hold
        private static decimal GetRandomConfidenceScore() =>
            new IntRange(min: 0, max: 1000).GetValue() / 100.0m;

        public static TheoryData<decimal> OutOfRangeConfidenceScores() =>
            new TheoryData<decimal>
            {
                -0.01m,
                10.01m
            };

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

        // Association has no scoped roles of its own (design §14.7, §18.6) — only the
        // global review roles apply until endpoint-derived authorization lands.
        public static TheoryData<string> ReviewRoles() =>
            new TheoryData<string>
            {
                Roles.Reviewer,
                Roles.Publisher,
                Roles.Admin
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
                    new TimeoutAssociationException(
                        message: "Failed content item association timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data)
                },
                {
                    dbUpdateException,
                    new FailedStorageAssociationException(
                        message: "Failed content item association storage error occurred, contact support.",
                        innerException: dbUpdateException,
                        data: dbUpdateException.Data)
                }
            };
        }

        public static TheoryData<Exception, Xeption> DependencyValidationExceptions()
        {
            string someMessage = GetRandomString();
            var duplicateKeyException = new DuplicateKeyException(someMessage);

            // UX_Associations_Pair reports through this type, not DuplicateKeyException, and
            // the two are siblings rather than parent and child — both derive straight from
            // Exception. It therefore needs its own catch and its own row here; without them a
            // duplicate pairing surfaces as a service exception instead of this one.
            var duplicateKeyWithUniqueIndexException =
                new DuplicateKeyWithUniqueIndexException(someMessage);

            var foreignKeyConstraintConflictException = new ForeignKeyConstraintConflictException(someMessage);

            return new TheoryData<Exception, Xeption>
            {
                {
                    duplicateKeyException,
                    new AlreadyExistsAssociationException(
                        message: "Content item association already exists with the same Id.",
                        innerException: duplicateKeyException,
                        data: duplicateKeyException.Data)
                },
                {
                    duplicateKeyWithUniqueIndexException,
                    new AlreadyExistsAssociationException(
                        message: "Content item association already exists, "
                            + "a uniqueness rule rejected the write.",
                        innerException: duplicateKeyWithUniqueIndexException,
                        data: duplicateKeyWithUniqueIndexException.Data)
                },
                {
                    foreignKeyConstraintConflictException,
                    new InvalidAssociationReferenceException(
                        message: "Invalid content item association reference error occurred.",
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

            // modify can collide with the pair index too: repointing an endpoint moves the row
            // onto an effective id another row may already occupy
            var duplicateKeyWithUniqueIndexException =
                new DuplicateKeyWithUniqueIndexException(someMessage);

            return new TheoryData<Exception, Xeption>
            {
                {
                    dbUpdateConcurrencyException,
                    new LockedAssociationException(
                        message: "Locked content item association record, please try again later.",
                        innerException: dbUpdateConcurrencyException,
                        data: dbUpdateConcurrencyException.Data)
                },
                {
                    duplicateKeyWithUniqueIndexException,
                    new AlreadyExistsAssociationException(
                        message: "Content item association already exists, "
                            + "a uniqueness rule rejected the write.",
                        innerException: duplicateKeyWithUniqueIndexException,
                        data: duplicateKeyWithUniqueIndexException.Data)
                },
                {
                    foreignKeyConstraintConflictException,
                    new InvalidAssociationReferenceException(
                        message: "Invalid content item association reference error occurred.",
                        innerException: foreignKeyConstraintConflictException,
                        data: foreignKeyConstraintConflictException.Data)
                }
            };
        }

        private static Association CreateRandomAssociation() =>
            CreateAssociationFiller(dateTimeOffset: GetRandomDateTimeOffset()).Create();

        private static EventEnvelope<Association>
            CreateRandomAssociationRequestEnvelope(
                SecurityContext? securityContext = null) =>
            new EventEnvelope<Association>
            {
                Content = new Association { Id = Guid.NewGuid() },
                SecurityContext = securityContext ?? CreateAuthenticatedSecurityContext(),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

        private static SecurityContext CreateAuthenticatedSecurityContext(params string[] roles) =>
            new SecurityContext
            {
                IsAuthenticated = true,
                Roles = roles
            };

        private static Association CreateRandomModifyAssociation(
            DateTimeOffset dateTimeOffset,
            string userId = "")
        {
            int randomDaysInPast = GetRandomNegativeNumber();

            Association randomAssociation =
                CreateAssociationFiller(dateTimeOffset, userId).Create();

            randomAssociation.CreatedWhen =
                randomAssociation.CreatedWhen.AddDays(randomDaysInPast);

            return randomAssociation;
        }

        private static IQueryable<Association> CreateRandomAssociations()
        {
            return CreateAssociationFiller(dateTimeOffset: GetRandomDateTimeOffset())
                .Create(count: GetRandomNumber())
                .AsQueryable();
        }

        private static DateTimeOffset GetRandomDateTimeOffset() =>
            new DateTimeRange(earliestDate: new DateTime()).GetValue();

        private static Filler<Association> CreateAssociationFiller(
            DateTimeOffset dateTimeOffset,
            string userId = "")
        {
            userId = string.IsNullOrEmpty(userId) ? Guid.NewGuid().ToString() : userId;
            var filler = new Filler<Association>();

            filler.Setup()
                .OnType<DateTimeOffset>().Use(dateTimeOffset)
                .OnType<DateTimeOffset?>().Use(dateTimeOffset)
                // IsDeleted gates every read and remove path, so it is pinned here rather
                // than drawn: a posture-sensitive test must never depend on the draw. Tests
                // that want a soft-deleted row set it explicitly.
                .OnProperty(association => association.IsDeleted).Use(false)
                .OnProperty(association => association.CreatedBy).Use(userId)
                .OnProperty(association => association.UpdatedBy).Use(userId)

                // The endpoint pair is pinned so the add path's derivation and canonical
                // ordering are both no-ops on a drawn association, and a test that is about
                // something else cannot fail on the draw.
                //
                // Attachment before ContentItem is already canonical — string.CompareOrdinal
                // puts 'A' before 'C' — so the ordering never swaps and a DeepClone taken
                // before the call still matches after it. Both types are versioned (design
                // §7.5.1), so neither group id is rewritten to its key id and both scopes
                // derive to AllVersions, which is what is pinned here. Tests that exercise
                // ordering, derivation or a non-versioned endpoint set the endpoints
                // explicitly.
                .OnProperty(association => association.EntityAType).Use(EntityType.Attachment)
                .OnProperty(association => association.EntityBType).Use(EntityType.ContentItem)
                .OnProperty(association => association.EntityAScope).Use(Scope.AllVersions)
                .OnProperty(association => association.EntityBScope).Use(Scope.AllVersions)

                // only a ContentItem endpoint may carry a content type, and resolving it is
                // the orchestration's read — a drawn value would trip the structural rule
                .OnProperty(association => association.EntityAContentType).Use((ContentType?)null)
                .OnProperty(association => association.EntityBContentType).Use((ContentType?)null)

                // the score is range checked on write, so it is pinned to a valid draw here
                // rather than left to the filler's unbounded decimal
                .OnProperty(association => association.ConfidenceScore)
                    .Use(GetRandomConfidenceScore())

                // A contribution is unpublished and unapproved: add rejects a caller-supplied
                // IsPublished, PublishDate or verdict status, and modify pins all three
                // against storage. Drawing them would make every write test fail on the draw
                // rather than on what it is testing. Tests about read visibility set them
                // explicitly on the storage row.
                .OnProperty(association => association.ApprovalStatus).Use(ApprovalStatus.Draft)
                .OnProperty(association => association.IsPublished).Use(false)
                .OnProperty(association => association.PublishDate).IgnoreIt();

            return filler;
        }
    }
}
