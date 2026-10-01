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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.Associations.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Associations
{
    public partial class AssociationServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnRemovingAssociationByIdEventWhenEnvelopeIsInvalidAsync()
        {
            // given
            EventEnvelope<Association>? nullEnvelope = null;

            var invalidAssociationEventException =
                new InvalidAssociationEventException(
                    message: "Invalid content item association event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: invalidAssociationEventException);

            // when
            ValueTask<EventEnvelope<Association>?> onRemovingTask =
                this.associationService.OnRemovingAssociationByIdAsync(
                    nullEnvelope!,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    onRemovingTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRemovingAssociationByIdEventWhenIdIsInvalidAsync()
        {
            // given
            var requestEnvelope = new EventEnvelope<Association>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(),
                Content = new Association { Id = Guid.Empty },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            var invalidAssociationException = new InvalidAssociationException(
                message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationException.UpsertDataList(
                key: nameof(Association.Id),
                value: "Id is required");

            var expectedAssociationValidationException = new AssociationValidationException(
                message: "Content item association validation error occurred, fix the errors and try again.",
                innerException: invalidAssociationException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.AssociationOnRemovingAssociationByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            // when
            ValueTask<EventEnvelope<Association>?> onRemovingTask =
                this.associationService.OnRemovingAssociationByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    onRemovingTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.AssociationOnRemovingAssociationByIdSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRemovingAssociationByIdEventWhenAssociationNotFoundAsync()
        {
            // given
            Guid someAssociationId = Guid.NewGuid();
            Association noAssociation = null!;

            var requestEnvelope = new EventEnvelope<Association>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(),
                Content = new Association { Id = someAssociationId },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            var notFoundAssociationException = new NotFoundAssociationException(
                message: $"Content item association not found with id: {someAssociationId}.");

            var expectedAssociationValidationException = new AssociationValidationException(
                message: "Content item association validation error occurred, fix the errors and try again.",
                innerException: notFoundAssociationException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.AssociationOnRemovingAssociationByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(
                    someAssociationId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(noAssociation);

            // when
            ValueTask<EventEnvelope<Association>?> onRemovingTask =
                this.associationService.OnRemovingAssociationByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    onRemovingTask.AsTask);

            // then: the raw not-found from the shared do-work is categorized the same way
            // the non-event path categorizes it — the event path must not degrade it to a
            // service exception.
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAssociationByIdAsync(
                    someAssociationId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowValidationExceptionOnRemovingAssociationByIdEventIfUserIsNotAuthenticatedAndLogItAsync(
            SecurityContext invalidSecurityContext)
        {
            // given: the ambient caller stays authenticated, so only the inbound envelope's
            // caller explains the refusal (§6 rule 1)
            var requestEnvelope = new EventEnvelope<Association>
            {
                SecurityContext = invalidSecurityContext,
                Content = new Association { Id = Guid.NewGuid() },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            var unauthorizedAssociationException = new UnauthorizedAssociationException(
                message: "The current user is not authenticated.");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedAssociationException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.AssociationOnRemovingAssociationByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            // when
            ValueTask<EventEnvelope<Association>?> onRemovingTask =
                this.associationService.OnRemovingAssociationByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    onRemovingTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.AssociationOnRemovingAssociationByIdSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAssociationByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Never);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(It.IsAny<Association>()),
                Times.Never);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldBlockRemovingAnEditorialRowWhenTheCallerIsReadOnlyOnRemovingAssociationByIdEventAndLogItAsync()
        {
            // given: the signed caller created the editorial row, so the owner test admits them
            // and the veto is the only refusal. The ambient caller holds no read-only role, so
            // only the inbound envelope's caller explains it (§6 rule 1)
            string actorUserId = GetRandomString();
            Association storageAssociation = CreateRandomAssociation();
            storageAssociation.IsDeleted = false;
            storageAssociation.UserId = null;
            storageAssociation.CreatedBy = actorUserId;

            var requestEnvelope = new EventEnvelope<Association>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(Roles.ReadOnly),
                Content = new Association { Id = storageAssociation.Id },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            var unauthorizedAssociationException = new UnauthorizedAssociationException(
                message: "The current user is blocked from contributing content item associations.");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedAssociationException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.AssociationOnRemovingAssociationByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(
                    storageAssociation.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(requestEnvelope.SecurityContext))
                    .ReturnsAsync(actorUserId);

            // when
            ValueTask<EventEnvelope<Association>?> onRemovingTask =
                this.associationService.OnRemovingAssociationByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    onRemovingTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.AssociationOnRemovingAssociationByIdSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAssociationByIdAsync(
                    storageAssociation.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(requestEnvelope.SecurityContext),
                Times.Once);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(It.IsAny<Association>()),
                Times.Never);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ReadOnlyRolesOverAReaction))]
        public async Task ShouldBlockAnAdministratorWithAReadOnlyRoleFromRemovingAnotherReadersReactionOnRemovingAssociationByIdEventAndLogItAsync(
            string readOnlyRole)
        {
            // given: the owner test admits Administrators, and the exemption belongs only to the
            // reader whose UserId the row carries, so the veto is the only refusal (§5 rule 4)
            string actorUserId = GetRandomString();
            string anotherReaderUserId = GetRandomString();
            Association storageAssociation = CreateRandomReaction(anotherReaderUserId);
            storageAssociation.IsDeleted = false;

            var requestEnvelope = new EventEnvelope<Association>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(
                    Roles.Administrators,
                    readOnlyRole),
                Content = new Association { Id = storageAssociation.Id },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            var unauthorizedAssociationException = new UnauthorizedAssociationException(
                message: "The current user is blocked from contributing content item associations.");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedAssociationException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.AssociationOnRemovingAssociationByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(
                    storageAssociation.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(requestEnvelope.SecurityContext))
                    .ReturnsAsync(actorUserId);

            // when
            ValueTask<EventEnvelope<Association>?> onRemovingTask =
                this.associationService.OnRemovingAssociationByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    onRemovingTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.AssociationOnRemovingAssociationByIdSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAssociationByIdAsync(
                    storageAssociation.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(requestEnvelope.SecurityContext),
                Times.Once);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(It.IsAny<Association>()),
                Times.Never);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRemovingAssociationByIdEventIfUserIsNotOwnerAndNotAdminAndLogItAsync()
        {
            // given: the signed caller holds no role, so the veto admits them, and neither
            // created the row nor is in Administrators, so the owner test refuses them (§5 rule 5)
            string actorUserId = GetRandomString();
            string anotherUserId = GetRandomString();
            Association storageAssociation = CreateRandomAssociation();
            storageAssociation.IsDeleted = false;
            storageAssociation.UserId = anotherUserId;
            storageAssociation.CreatedBy = anotherUserId;

            var requestEnvelope = new EventEnvelope<Association>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(),
                Content = new Association { Id = storageAssociation.Id },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            var unauthorizedAssociationException = new UnauthorizedAssociationException(
                message: "The current user is not allowed to remove this content item association.");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedAssociationException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.AssociationOnRemovingAssociationByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(
                    storageAssociation.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(requestEnvelope.SecurityContext))
                    .ReturnsAsync(actorUserId);

            // when
            ValueTask<EventEnvelope<Association>?> onRemovingTask =
                this.associationService.OnRemovingAssociationByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    onRemovingTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.AssociationOnRemovingAssociationByIdSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAssociationByIdAsync(
                    storageAssociation.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(requestEnvelope.SecurityContext),
                Times.Once);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(It.IsAny<Association>()),
                Times.Never);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
