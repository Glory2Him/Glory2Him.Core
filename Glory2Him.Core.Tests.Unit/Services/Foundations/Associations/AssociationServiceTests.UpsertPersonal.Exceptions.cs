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
using System.Threading;
using System.Threading.Tasks;
using EFxceptions.Models.Exceptions;
using FluentAssertions;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.Associations.Exceptions;
using Glory2Him.Core.Models.Securities;
using Microsoft.Data.SqlClient;
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

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(It.IsAny<Association>(), It.IsAny<SecurityContext>()))
                    .ReturnsAsync((Association entity, SecurityContext _) => entity);

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

        private void SetupPersonalUpsertLookupToThrow(Exception exception) =>
            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationsAsync(
                    It.IsAny<Func<IQueryable<Association>, IQueryable<Association>>>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(exception);
    }
}
