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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Glory2Him.Core.Models.Foundations.BibleReferences.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.BibleReferences
{
    public partial class BibleReferenceServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnHardRemoveByIdIfIdIsInvalidAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            var invalidBibleReferenceId = Guid.Empty;

            var invalidBibleReferenceException = new InvalidBibleReferenceException(
                message: "Bible reference is invalid, fix the errors and try again.");

            invalidBibleReferenceException.UpsertDataList(
                key: nameof(BibleReference.Id),
                value: "Id is required");

            var expectedBibleReferenceValidationException = new BibleReferenceValidationException(
                message: "Bible reference validation error occurred, fix the errors and try again.",
                innerException: invalidBibleReferenceException);

            // when
            ValueTask<BibleReference> hardRemoveBibleReferenceByIdTask =
                this.bibleReferenceService.HardRemoveBibleReferenceByIdAsync(
                    invalidBibleReferenceId,
                    TestContext.Current.CancellationToken);

            BibleReferenceValidationException actualBibleReferenceValidationException =
                await Assert.ThrowsAsync<BibleReferenceValidationException>(
                    hardRemoveBibleReferenceByIdTask.AsTask);

            // then
            actualBibleReferenceValidationException.Should().BeEquivalentTo(
                expectedBibleReferenceValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedBibleReferenceValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnHardRemoveByIdIfBibleReferenceNotFoundAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            Guid someBibleReferenceId = Guid.NewGuid();
            BibleReference noBibleReference = null;

            var notFoundBibleReferenceException = new NotFoundBibleReferenceException(
                message: $"Bible reference not found with id: {someBibleReferenceId}.");

            var expectedBibleReferenceValidationException = new BibleReferenceValidationException(
                message: "Bible reference validation error occurred, fix the errors and try again.",
                innerException: notFoundBibleReferenceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectBibleReferenceByIdAsync(
                    someBibleReferenceId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(noBibleReference);

            // when
            ValueTask<BibleReference> hardRemoveBibleReferenceByIdTask =
                this.bibleReferenceService.HardRemoveBibleReferenceByIdAsync(
                    someBibleReferenceId,
                    TestContext.Current.CancellationToken);

            BibleReferenceValidationException actualBibleReferenceValidationException =
                await Assert.ThrowsAsync<BibleReferenceValidationException>(
                    hardRemoveBibleReferenceByIdTask.AsTask);

            // then
            actualBibleReferenceValidationException.Should().BeEquivalentTo(
                expectedBibleReferenceValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectBibleReferenceByIdAsync(
                    someBibleReferenceId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedBibleReferenceValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowValidationExceptionOnHardRemoveByIdIfUserIsNotAuthenticatedAndLogItAsync(
            SecurityContext invalidSecurityContext)
        {
            // given
            this.ambientSecurityContext = invalidSecurityContext;
            Guid someBibleReferenceId = Guid.NewGuid();

            var unauthorizedBibleReferenceException = new UnauthorizedBibleReferenceException(
                message: "The current user is not authenticated.");

            var expectedBibleReferenceValidationException = new BibleReferenceValidationException(
                message: "Bible reference validation error occurred, fix the errors and try again.",
                innerException: unauthorizedBibleReferenceException);

            // when
            ValueTask<BibleReference> hardRemoveBibleReferenceByIdTask =
                this.bibleReferenceService.HardRemoveBibleReferenceByIdAsync(
                    someBibleReferenceId,
                    TestContext.Current.CancellationToken);

            BibleReferenceValidationException actualBibleReferenceValidationException =
                await Assert.ThrowsAsync<BibleReferenceValidationException>(
                    hardRemoveBibleReferenceByIdTask.AsTask);

            // then
            actualBibleReferenceValidationException.Should().BeEquivalentTo(
                expectedBibleReferenceValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedBibleReferenceValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.DeleteBibleReferenceAsync(
                    It.IsAny<BibleReference>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(NonAdminRoleSets))]
        public async Task ShouldThrowValidationExceptionOnHardRemoveByIdIfUserIsNotAdminAndLogItAsync(
            string[] nonAdminRoles)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(nonAdminRoles);
            Guid someBibleReferenceId = Guid.NewGuid();

            var unauthorizedBibleReferenceException = new UnauthorizedBibleReferenceException(
                message: "The current user is not allowed to permanently remove this bible reference.");

            var expectedBibleReferenceValidationException = new BibleReferenceValidationException(
                message: "Bible reference validation error occurred, fix the errors and try again.",
                innerException: unauthorizedBibleReferenceException);

            // when
            ValueTask<BibleReference> hardRemoveBibleReferenceByIdTask =
                this.bibleReferenceService.HardRemoveBibleReferenceByIdAsync(
                    someBibleReferenceId,
                    TestContext.Current.CancellationToken);

            BibleReferenceValidationException actualBibleReferenceValidationException =
                await Assert.ThrowsAsync<BibleReferenceValidationException>(
                    hardRemoveBibleReferenceByIdTask.AsTask);

            // then
            actualBibleReferenceValidationException.Should().BeEquivalentTo(
                expectedBibleReferenceValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedBibleReferenceValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.DeleteBibleReferenceAsync(
                    It.IsAny<BibleReference>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
