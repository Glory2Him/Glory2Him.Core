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
using FluentAssertions;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.Associations.Exceptions;
using Microsoft.Data.SqlClient;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Associations
{
    public partial class AssociationServiceTests
    {
        [Fact]
        public async Task ShouldThrowCriticalDependencyExceptionOnFindPersonalIfSqlErrorOccursAndLogItAsync()
        {
            // given
            Association lookupRequest = CreateAllowedPersonalLookupRequest();
            SqlException sqlException = GetSqlException();

            var failedStorageAssociationException = new FailedStorageAssociationException(
                message: "Failed content item association storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedAssociationDependencyException = new AssociationDependencyException(
                message: "Content item association dependency error occurred, contact support.",
                innerException: failedStorageAssociationException);

            SetupPersonalLookupToThrow(sqlException);

            // when
            ValueTask<PersonalAssociationMatch?> findTask =
                this.associationService.FindPersonalAssociationAsync(
                    lookupRequest,
                    TestContext.Current.CancellationToken);

            AssociationDependencyException actualAssociationDependencyException =
                await Assert.ThrowsAsync<AssociationDependencyException>(findTask.AsTask);

            // then
            actualAssociationDependencyException.Should().BeEquivalentTo(
                expectedAssociationDependencyException);

            VerifyPersonalLookupAsked(TestContext.Current.CancellationToken, Times.Once());

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
        public async Task ShouldThrowOperationCanceledExceptionOnFindPersonalIfCancellationRequestedAsync()
        {
            // given
            Association lookupRequest = CreateAllowedPersonalLookupRequest();
            var cancelledToken = new CancellationToken(canceled: true);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationsAsync(
                    It.IsAny<Func<IQueryable<Association>, IQueryable<PersonalAssociationMatch>>>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(Array.Empty<PersonalAssociationMatch>());

            // when
            ValueTask<PersonalAssociationMatch?> findTask =
                this.associationService.FindPersonalAssociationAsync(
                    lookupRequest,
                    cancelledToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(findTask.AsTask);

            // the guard sits above the envelope, so nothing at all is asked
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // a signed-in reader asking for their own row, so the lookup reaches storage
        private Association CreateAllowedPersonalLookupRequest()
        {
            string readerUserId = GetRandomString();
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext))
                    .ReturnsAsync(readerUserId);

            return CreatePersonalLookupRequest(readerUserId);
        }

        private void SetupPersonalLookupToThrow(Exception exception) =>
            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationsAsync(
                    It.IsAny<Func<IQueryable<Association>, IQueryable<PersonalAssociationMatch>>>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(exception);
    }
}
