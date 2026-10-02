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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EFxceptions.Models.Exceptions;
using Force.DeepCloner;
using FluentAssertions;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.Associations.Exceptions;
using Glory2Him.Core.Models.Securities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Associations
{
    public partial class AssociationServiceTests
    {
        [Fact]
        public async Task ShouldThrowDependencyValidationExceptionOnUpsertPersonalIfTheRowAlreadyExistsAndLogItAsync()
        {
            // given: the reader had no row when it was looked up, and a second first reaction of
            // theirs landed before this one was inserted, so the personal index refuses it
            // (§DOM4.6 rule 2)
            Association upsertRequest = CreateAllowedPersonalUpsertRequest();
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();
            string someMessage = GetRandomString();

            var duplicateKeyWithUniqueIndexException =
                new DuplicateKeyWithUniqueIndexException(someMessage);

            var alreadyExistsAssociationException =
                new AlreadyExistsAssociationException(
                    message: "Content item association already exists, "
                        + "a uniqueness rule rejected the write.",
                    innerException: duplicateKeyWithUniqueIndexException,
                    data: duplicateKeyWithUniqueIndexException.Data);

            var expectedAssociationDependencyValidationException =
                new AssociationDependencyValidationException(
                    message: "Content item association dependency validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: alreadyExistsAssociationException);

            SetupPersonalUpsertLookupOver(
                CreateRandomAssociations(),
                TestContext.Current.CancellationToken);

            SetupPersonalUpsertNewRowStamp(upsertRequest.UserId, currentDateTime);

            this.storageBrokerMock.Setup(broker =>
                broker.InsertAssociationAsync(It.IsAny<Association>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(duplicateKeyWithUniqueIndexException);

            // when
            ValueTask<PersonalAssociationUpsert> upsertTask =
                this.associationService.UpsertPersonalAssociationAsync(
                    upsertRequest,
                    TestContext.Current.CancellationToken);

            AssociationDependencyValidationException actualAssociationDependencyValidationException =
                await Assert.ThrowsAsync<AssociationDependencyValidationException>(upsertTask.AsTask);

            // then: refused as the add's duplicate is, and nothing announced
            actualAssociationDependencyValidationException.Should().BeEquivalentTo(
                expectedAssociationDependencyValidationException);

            VerifyPersonalUpsertLookupAsked(TestContext.Current.CancellationToken, Times.Once());

            this.storageBrokerMock.Verify(broker =>
                broker.InsertAssociationAsync(It.IsAny<Association>(), TestContext.Current.CancellationToken),
                    Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationDependencyValidationException))),
                Times.Once);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateNextAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    It.IsAny<Association>()),
                Times.Never);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                    Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowCriticalDependencyExceptionOnUpsertPersonalIfSqlErrorOccursAndLogItAsync()
        {
            // given
            Association upsertRequest = CreateAllowedPersonalUpsertRequest();
            SqlException sqlException = GetSqlException();

            var failedStorageAssociationException = new FailedStorageAssociationException(
                message: "Failed content item association storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedAssociationDependencyException = new AssociationDependencyException(
                message: "Content item association dependency error occurred, contact support.",
                innerException: failedStorageAssociationException);

            SetupPersonalUpsertLookupToThrow(sqlException);

            // when
            ValueTask<PersonalAssociationUpsert> upsertTask =
                this.associationService.UpsertPersonalAssociationAsync(
                    upsertRequest,
                    TestContext.Current.CancellationToken);

            AssociationDependencyException actualAssociationDependencyException =
                await Assert.ThrowsAsync<AssociationDependencyException>(upsertTask.AsTask);

            // then
            actualAssociationDependencyException.Should().BeEquivalentTo(
                expectedAssociationDependencyException);

            VerifyPersonalUpsertLookupAsked(TestContext.Current.CancellationToken, Times.Once());

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedAssociationDependencyException))),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnUpsertPersonalIfCancellationRequestedAsync()
        {
            // given: storage would answer if it were asked
            Association upsertRequest = CreateAllowedPersonalUpsertRequest();
            var cancelledToken = new CancellationToken(canceled: true);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationsAsync(
                    It.IsAny<Func<IQueryable<Association>, IQueryable<Association>>>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(Array.Empty<Association>());

            // when
            ValueTask<PersonalAssociationUpsert> upsertTask =
                this.associationService.UpsertPersonalAssociationAsync(
                    upsertRequest,
                    cancelledToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(upsertTask.AsTask);

            // the guard sits above the envelope, so nothing at all is read or written
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnUpsertPersonalIfServiceErrorOccursAndLogItAsync()
        {
            // given
            Association upsertRequest = CreateAllowedPersonalUpsertRequest();
            var serviceException = new Exception();

            var failedAssociationServiceException = new FailedAssociationServiceException(
                message: "Failed content item association service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedAssociationServiceException = new AssociationServiceException(
                message: "Content item association service error occurred, contact support.",
                innerException: failedAssociationServiceException);

            SetupPersonalUpsertLookupToThrow(serviceException);

            // when
            ValueTask<PersonalAssociationUpsert> upsertTask =
                this.associationService.UpsertPersonalAssociationAsync(
                    upsertRequest,
                    TestContext.Current.CancellationToken);

            AssociationServiceException actualAssociationServiceException =
                await Assert.ThrowsAsync<AssociationServiceException>(upsertTask.AsTask);

            // then
            actualAssociationServiceException.Should().BeEquivalentTo(
                expectedAssociationServiceException);

            VerifyPersonalUpsertLookupAsked(TestContext.Current.CancellationToken, Times.Once());

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationServiceException))),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnUpsertPersonalIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given: the storage call is cancelled though the caller's token was not — a timeout
            Association upsertRequest = CreateAllowedPersonalUpsertRequest();
            var operationCanceledException = new OperationCanceledException();

            var timeoutException =
                new TimeoutException("The dependency operation timed out.");

            var timeoutAssociationException =
                new TimeoutAssociationException(
                    message: "Failed content item association timeout error occurred, contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            var expectedAssociationDependencyException = new AssociationDependencyException(
                message: "Content item association dependency error occurred, contact support.",
                innerException: timeoutAssociationException);

            SetupPersonalUpsertLookupToThrow(operationCanceledException);

            // when
            ValueTask<PersonalAssociationUpsert> upsertTask =
                this.associationService.UpsertPersonalAssociationAsync(
                    upsertRequest,
                    TestContext.Current.CancellationToken);

            AssociationDependencyException actualAssociationDependencyException =
                await Assert.ThrowsAsync<AssociationDependencyException>(upsertTask.AsTask);

            // then: reported as a timeout, and nothing written or announced
            actualAssociationDependencyException.Should().BeEquivalentTo(
                expectedAssociationDependencyException);

            VerifyPersonalUpsertLookupAsked(TestContext.Current.CancellationToken, Times.Once());

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationDependencyException))),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyValidationExceptionOnUpsertPersonalIfTheIdAlreadyExistsAndLogItAsync()
        {
            // given: the new row's minted Id is already taken, so the insert is refused
            string someMessage = GetRandomString();
            var duplicateKeyException = new DuplicateKeyException(someMessage);

            Association upsertRequest =
                ArrangePersonalUpsertWriteToThrow(isUpdate: false, duplicateKeyException);

            var alreadyExistsAssociationException =
                new AlreadyExistsAssociationException(
                    message: "Content item association already exists with the same Id.",
                    innerException: duplicateKeyException,
                    data: duplicateKeyException.Data);

            var expectedAssociationDependencyValidationException =
                new AssociationDependencyValidationException(
                    message: "Content item association dependency validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: alreadyExistsAssociationException);

            // when
            ValueTask<PersonalAssociationUpsert> upsertTask =
                this.associationService.UpsertPersonalAssociationAsync(
                    upsertRequest,
                    TestContext.Current.CancellationToken);

            AssociationDependencyValidationException actualAssociationDependencyValidationException =
                await Assert.ThrowsAsync<AssociationDependencyValidationException>(upsertTask.AsTask);

            // then: refused as the add refuses it, and nothing announced
            actualAssociationDependencyValidationException.Should().BeEquivalentTo(
                expectedAssociationDependencyValidationException);

            VerifyPersonalUpsertWriteAsked(isUpdate: false);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationDependencyValidationException))),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyValidationExceptionOnUpsertPersonalIfAReferenceIsInvalidAndLogItAsync()
        {
            // given: the new row's insert is refused by a foreign key conflict
            string someMessage = GetRandomString();

            var foreignKeyConstraintConflictException =
                new ForeignKeyConstraintConflictException(someMessage);

            Association upsertRequest =
                ArrangePersonalUpsertWriteToThrow(isUpdate: false, foreignKeyConstraintConflictException);

            var invalidAssociationReferenceException =
                new InvalidAssociationReferenceException(
                    message: "Invalid content item association reference error occurred.",
                    innerException: foreignKeyConstraintConflictException,
                    data: foreignKeyConstraintConflictException.Data);

            var expectedAssociationDependencyValidationException =
                new AssociationDependencyValidationException(
                    message: "Content item association dependency validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidAssociationReferenceException);

            // when
            ValueTask<PersonalAssociationUpsert> upsertTask =
                this.associationService.UpsertPersonalAssociationAsync(
                    upsertRequest,
                    TestContext.Current.CancellationToken);

            AssociationDependencyValidationException actualAssociationDependencyValidationException =
                await Assert.ThrowsAsync<AssociationDependencyValidationException>(upsertTask.AsTask);

            // then
            actualAssociationDependencyValidationException.Should().BeEquivalentTo(
                expectedAssociationDependencyValidationException);

            VerifyPersonalUpsertWriteAsked(isUpdate: false);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationDependencyValidationException))),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyValidationExceptionOnUpsertPersonalIfDatabaseUpdateConcurrencyErrorOccursAndLogItAsync()
        {
            // given: another write changed or removed the reader's row between the lookup and this
            // repoint's update
            var dbUpdateConcurrencyException = new DbUpdateConcurrencyException();

            Association upsertRequest =
                ArrangePersonalUpsertWriteToThrow(isUpdate: true, dbUpdateConcurrencyException);

            var lockedAssociationException = new LockedAssociationException(
                message: "Locked content item association record, please try again later.",
                innerException: dbUpdateConcurrencyException,
                data: dbUpdateConcurrencyException.Data);

            var expectedAssociationDependencyValidationException =
                new AssociationDependencyValidationException(
                    message: "Content item association dependency validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: lockedAssociationException);

            // when
            ValueTask<PersonalAssociationUpsert> upsertTask =
                this.associationService.UpsertPersonalAssociationAsync(
                    upsertRequest,
                    TestContext.Current.CancellationToken);

            AssociationDependencyValidationException actualAssociationDependencyValidationException =
                await Assert.ThrowsAsync<AssociationDependencyValidationException>(upsertTask.AsTask);

            // then
            actualAssociationDependencyValidationException.Should().BeEquivalentTo(
                expectedAssociationDependencyValidationException);

            VerifyPersonalUpsertWriteAsked(isUpdate: true);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationDependencyValidationException))),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ShouldThrowDependencyExceptionOnUpsertPersonalIfDatabaseUpdateErrorOccursAndLogItAsync(
            bool isUpdate)
        {
            // given: the write fails with a database update error, on the create arm's insert or
            // on an existing row's update
            var dbUpdateException = new DbUpdateException();

            Association upsertRequest =
                ArrangePersonalUpsertWriteToThrow(isUpdate, dbUpdateException);

            var failedStorageAssociationException =
                new FailedStorageAssociationException(
                    message: "Failed content item association storage error occurred, contact support.",
                    innerException: dbUpdateException,
                    data: dbUpdateException.Data);

            var expectedAssociationDependencyException = new AssociationDependencyException(
                message: "Content item association dependency error occurred, contact support.",
                innerException: failedStorageAssociationException);

            // when
            ValueTask<PersonalAssociationUpsert> upsertTask =
                this.associationService.UpsertPersonalAssociationAsync(
                    upsertRequest,
                    TestContext.Current.CancellationToken);

            AssociationDependencyException actualAssociationDependencyException =
                await Assert.ThrowsAsync<AssociationDependencyException>(upsertTask.AsTask);

            // then
            actualAssociationDependencyException.Should().BeEquivalentTo(
                expectedAssociationDependencyException);

            VerifyPersonalUpsertWriteAsked(isUpdate);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationDependencyException))),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // A reader's request that reaches a write: the create arm's insert, or, with the reader's
        // live row on another reaction stored, an existing row's update. The write throws.
        private Association ArrangePersonalUpsertWriteToThrow(bool isUpdate, Exception exception)
        {
            Association upsertRequest = CreateAllowedPersonalUpsertRequest();
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();
            List<Association> storageAssociations = CreateRandomAssociations().ToList();

            if (isUpdate)
            {
                storageAssociations.Add(CreateStoredPersonalRow(upsertRequest, isDeleted: false));
            }

            SetupPersonalUpsertLookupOver(
                storageAssociations,
                TestContext.Current.CancellationToken);

            SetupPersonalUpsertNewRowStamp(upsertRequest.UserId, currentDateTime);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(It.IsAny<Association>(), It.IsAny<SecurityContext>()))
                    .ReturnsAsync((Association entity, SecurityContext _) =>
                        StampModifyAudit(entity.DeepClone(), upsertRequest.UserId, currentDateTime));

            this.storageBrokerMock.Setup(broker =>
                broker.InsertAssociationAsync(It.IsAny<Association>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(exception);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateAssociationAsync(It.IsAny<Association>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(exception);

            return upsertRequest;
        }

        // the reader's row was resolved and the one write the arm makes was asked, with the
        // caller's token
        private void VerifyPersonalUpsertWriteAsked(bool isUpdate)
        {
            VerifyPersonalUpsertLookupAsked(TestContext.Current.CancellationToken, Times.Once());

            this.storageBrokerMock.Verify(broker =>
                broker.InsertAssociationAsync(It.IsAny<Association>(), TestContext.Current.CancellationToken),
                    isUpdate ? Times.Never() : Times.Once());

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateAssociationAsync(It.IsAny<Association>(), TestContext.Current.CancellationToken),
                    isUpdate ? Times.Once() : Times.Never());
        }

        // a signed-in reader giving their own reaction, so the upsert reaches storage
        private Association CreateAllowedPersonalUpsertRequest()
        {
            string readerUserId = GetRandomString();
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext))
                    .ReturnsAsync(readerUserId);

            return CreatePersonalUpsertRequest(readerUserId);
        }

        // the reader's add stamp on the new row, at a moment the clock calls recent, so the create
        // arm reaches its insert
        private void SetupPersonalUpsertNewRowStamp(string readerUserId, DateTimeOffset currentDateTime)
        {
            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(It.IsAny<Association>(), It.IsAny<SecurityContext>()))
                    .ReturnsAsync((Association entity, SecurityContext _) =>
                        StampAddAudit(entity.DeepClone(), readerUserId, currentDateTime));

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);
        }

        private void SetupPersonalUpsertLookupToThrow(Exception exception) =>
            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationsAsync(
                    It.IsAny<Func<IQueryable<Association>, IQueryable<Association>>>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(exception);
    }
}
