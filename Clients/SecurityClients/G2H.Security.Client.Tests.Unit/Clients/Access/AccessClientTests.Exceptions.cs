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
using System.Threading.Tasks;
using FluentAssertions;
using G2H.Security.Client.Models.Clients.Access.Exceptions;
using G2H.Security.Client.Models.Foundations.Access;
using Moq;
using Xeptions;

namespace G2H.Security.Client.Tests.Unit.Clients.Access
{
    public partial class AccessClientTests
    {
        [Theory]
        [MemberData(nameof(ValidationExceptions))]
        public async Task ShouldThrowValidationExceptionOnEvaluateApprovalConditionsIfValidationErrorOccursAsync(
            Xeption validationException)
        {
            // given
            ApprovalConditionsRequest someApprovalConditionsRequest =
                CreateRandomApprovalConditionsRequest();

            var expectedAccessClientValidationException =
                new AccessClientValidationException(
                    message: "Access client validation error occurred, fix the error and try again.",
                    innerException: (validationException.InnerException as Xeption)!,
                    data: validationException.InnerException?.Data!);

            this.accessServiceMock.Setup(service =>
                service.EvaluateApprovalConditionsAsync(It.IsAny<ApprovalConditionsRequest>()))
                    .Throws(validationException);

            // when
            ValueTask<ApprovalConditionsVerdict> evaluateApprovalConditionsTask =
                this.accessClient.EvaluateApprovalConditionsAsync(
                    someApprovalConditionsRequest);

            AccessClientValidationException actualAccessClientValidationException =
                await Assert.ThrowsAsync<AccessClientValidationException>(
                    evaluateApprovalConditionsTask.AsTask);

            // then
            actualAccessClientValidationException.Should()
                .BeEquivalentTo(expectedAccessClientValidationException);

            this.accessServiceMock.Verify(service =>
                service.EvaluateApprovalConditionsAsync(It.IsAny<ApprovalConditionsRequest>()),
                    Times.Once);

            this.accessServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnEvaluateApprovalConditionsIfDependencyErrorOccursAsync(
            Xeption dependencyException)
        {
            // given
            ApprovalConditionsRequest someApprovalConditionsRequest =
                CreateRandomApprovalConditionsRequest();

            var expectedAccessClientDependencyException =
                new AccessClientDependencyException(
                    message: "Access client dependency error occurred, please contact support.",
                    innerException: (dependencyException.InnerException as Xeption)!,
                    data: dependencyException.InnerException?.Data!);

            this.accessServiceMock.Setup(service =>
                service.EvaluateApprovalConditionsAsync(It.IsAny<ApprovalConditionsRequest>()))
                    .Throws(dependencyException);

            // when
            ValueTask<ApprovalConditionsVerdict> evaluateApprovalConditionsTask =
                this.accessClient.EvaluateApprovalConditionsAsync(
                    someApprovalConditionsRequest);

            AccessClientDependencyException actualAccessClientDependencyException =
                await Assert.ThrowsAsync<AccessClientDependencyException>(
                    evaluateApprovalConditionsTask.AsTask);

            // then
            actualAccessClientDependencyException.Should()
                .BeEquivalentTo(expectedAccessClientDependencyException);

            this.accessServiceMock.Verify(service =>
                service.EvaluateApprovalConditionsAsync(It.IsAny<ApprovalConditionsRequest>()),
                    Times.Once);

            this.accessServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnEvaluateApprovalConditionsIfServiceErrorOccursAsync()
        {
            // given
            ApprovalConditionsRequest someApprovalConditionsRequest =
                CreateRandomApprovalConditionsRequest();

            var serviceException = new Exception(message: GetRandomString());

            var expectedAccessClientServiceException =
                new AccessClientServiceException(
                    message: "Access client service error occurred, please contact support.",
                    innerException: serviceException,
                    data: serviceException.Data);

            this.accessServiceMock.Setup(service =>
                service.EvaluateApprovalConditionsAsync(It.IsAny<ApprovalConditionsRequest>()))
                    .Throws(serviceException);

            // when
            ValueTask<ApprovalConditionsVerdict> evaluateApprovalConditionsTask =
                this.accessClient.EvaluateApprovalConditionsAsync(
                    someApprovalConditionsRequest);

            AccessClientServiceException actualAccessClientServiceException =
                await Assert.ThrowsAsync<AccessClientServiceException>(
                    evaluateApprovalConditionsTask.AsTask);

            // then
            actualAccessClientServiceException.Should()
                .BeEquivalentTo(expectedAccessClientServiceException);

            this.accessServiceMock.Verify(service =>
                service.EvaluateApprovalConditionsAsync(It.IsAny<ApprovalConditionsRequest>()),
                    Times.Once);

            this.accessServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ValidationExceptions))]
        public async Task ShouldThrowValidationExceptionOnMayRecordApprovalReviewIfValidationErrorOccursAsync(
            Xeption validationException)
        {
            // given
            RecordReviewRequest someRecordReviewRequest = CreateRandomRecordReviewRequest();

            var expectedAccessClientValidationException =
                new AccessClientValidationException(
                    message: "Access client validation error occurred, fix the error and try again.",
                    innerException: (validationException.InnerException as Xeption)!,
                    data: validationException.InnerException?.Data!);

            this.accessServiceMock.Setup(service =>
                service.MayRecordApprovalReviewAsync(It.IsAny<RecordReviewRequest>()))
                    .Throws(validationException);

            // when
            ValueTask<AccessVerdict> mayRecordApprovalReviewTask =
                this.accessClient.MayRecordApprovalReviewAsync(someRecordReviewRequest);

            AccessClientValidationException actualAccessClientValidationException =
                await Assert.ThrowsAsync<AccessClientValidationException>(
                    mayRecordApprovalReviewTask.AsTask);

            // then
            actualAccessClientValidationException.Should()
                .BeEquivalentTo(expectedAccessClientValidationException);

            this.accessServiceMock.Verify(service =>
                service.MayRecordApprovalReviewAsync(It.IsAny<RecordReviewRequest>()),
                    Times.Once);

            this.accessServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnMayRecordApprovalReviewIfDependencyErrorOccursAsync(
            Xeption dependencyException)
        {
            // given
            RecordReviewRequest someRecordReviewRequest = CreateRandomRecordReviewRequest();

            var expectedAccessClientDependencyException =
                new AccessClientDependencyException(
                    message: "Access client dependency error occurred, please contact support.",
                    innerException: (dependencyException.InnerException as Xeption)!,
                    data: dependencyException.InnerException?.Data!);

            this.accessServiceMock.Setup(service =>
                service.MayRecordApprovalReviewAsync(It.IsAny<RecordReviewRequest>()))
                    .Throws(dependencyException);

            // when
            ValueTask<AccessVerdict> mayRecordApprovalReviewTask =
                this.accessClient.MayRecordApprovalReviewAsync(someRecordReviewRequest);

            AccessClientDependencyException actualAccessClientDependencyException =
                await Assert.ThrowsAsync<AccessClientDependencyException>(
                    mayRecordApprovalReviewTask.AsTask);

            // then
            actualAccessClientDependencyException.Should()
                .BeEquivalentTo(expectedAccessClientDependencyException);

            this.accessServiceMock.Verify(service =>
                service.MayRecordApprovalReviewAsync(It.IsAny<RecordReviewRequest>()),
                    Times.Once);

            this.accessServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnMayRecordApprovalReviewIfServiceErrorOccursAsync()
        {
            // given
            RecordReviewRequest someRecordReviewRequest = CreateRandomRecordReviewRequest();
            var serviceException = new Exception(message: GetRandomString());

            var expectedAccessClientServiceException =
                new AccessClientServiceException(
                    message: "Access client service error occurred, please contact support.",
                    innerException: serviceException,
                    data: serviceException.Data);

            this.accessServiceMock.Setup(service =>
                service.MayRecordApprovalReviewAsync(It.IsAny<RecordReviewRequest>()))
                    .Throws(serviceException);

            // when
            ValueTask<AccessVerdict> mayRecordApprovalReviewTask =
                this.accessClient.MayRecordApprovalReviewAsync(someRecordReviewRequest);

            AccessClientServiceException actualAccessClientServiceException =
                await Assert.ThrowsAsync<AccessClientServiceException>(
                    mayRecordApprovalReviewTask.AsTask);

            // then
            actualAccessClientServiceException.Should()
                .BeEquivalentTo(expectedAccessClientServiceException);

            this.accessServiceMock.Verify(service =>
                service.MayRecordApprovalReviewAsync(It.IsAny<RecordReviewRequest>()),
                    Times.Once);

            this.accessServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ValidationExceptions))]
        public async Task ShouldThrowValidationExceptionOnMayDecideApprovalIfValidationErrorOccursAsync(
            Xeption validationException)
        {
            // given
            DecideApprovalRequest someDecideApprovalRequest = CreateRandomDecideApprovalRequest();

            var expectedAccessClientValidationException =
                new AccessClientValidationException(
                    message: "Access client validation error occurred, fix the error and try again.",
                    innerException: (validationException.InnerException as Xeption)!,
                    data: validationException.InnerException?.Data!);

            this.accessServiceMock.Setup(service =>
                service.MayDecideApprovalAsync(It.IsAny<DecideApprovalRequest>()))
                    .Throws(validationException);

            // when
            ValueTask<AccessVerdict> mayDecideApprovalTask =
                this.accessClient.MayDecideApprovalAsync(someDecideApprovalRequest);

            AccessClientValidationException actualAccessClientValidationException =
                await Assert.ThrowsAsync<AccessClientValidationException>(
                    mayDecideApprovalTask.AsTask);

            // then
            actualAccessClientValidationException.Should()
                .BeEquivalentTo(expectedAccessClientValidationException);

            this.accessServiceMock.Verify(service =>
                service.MayDecideApprovalAsync(It.IsAny<DecideApprovalRequest>()),
                    Times.Once);

            this.accessServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnMayDecideApprovalIfDependencyErrorOccursAsync(
            Xeption dependencyException)
        {
            // given
            DecideApprovalRequest someDecideApprovalRequest = CreateRandomDecideApprovalRequest();

            var expectedAccessClientDependencyException =
                new AccessClientDependencyException(
                    message: "Access client dependency error occurred, please contact support.",
                    innerException: (dependencyException.InnerException as Xeption)!,
                    data: dependencyException.InnerException?.Data!);

            this.accessServiceMock.Setup(service =>
                service.MayDecideApprovalAsync(It.IsAny<DecideApprovalRequest>()))
                    .Throws(dependencyException);

            // when
            ValueTask<AccessVerdict> mayDecideApprovalTask =
                this.accessClient.MayDecideApprovalAsync(someDecideApprovalRequest);

            AccessClientDependencyException actualAccessClientDependencyException =
                await Assert.ThrowsAsync<AccessClientDependencyException>(
                    mayDecideApprovalTask.AsTask);

            // then
            actualAccessClientDependencyException.Should()
                .BeEquivalentTo(expectedAccessClientDependencyException);

            this.accessServiceMock.Verify(service =>
                service.MayDecideApprovalAsync(It.IsAny<DecideApprovalRequest>()),
                    Times.Once);

            this.accessServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnMayDecideApprovalIfServiceErrorOccursAsync()
        {
            // given
            DecideApprovalRequest someDecideApprovalRequest = CreateRandomDecideApprovalRequest();
            var serviceException = new Exception(message: GetRandomString());

            var expectedAccessClientServiceException =
                new AccessClientServiceException(
                    message: "Access client service error occurred, please contact support.",
                    innerException: serviceException,
                    data: serviceException.Data);

            this.accessServiceMock.Setup(service =>
                service.MayDecideApprovalAsync(It.IsAny<DecideApprovalRequest>()))
                    .Throws(serviceException);

            // when
            ValueTask<AccessVerdict> mayDecideApprovalTask =
                this.accessClient.MayDecideApprovalAsync(someDecideApprovalRequest);

            AccessClientServiceException actualAccessClientServiceException =
                await Assert.ThrowsAsync<AccessClientServiceException>(
                    mayDecideApprovalTask.AsTask);

            // then
            actualAccessClientServiceException.Should()
                .BeEquivalentTo(expectedAccessClientServiceException);

            this.accessServiceMock.Verify(service =>
                service.MayDecideApprovalAsync(It.IsAny<DecideApprovalRequest>()),
                    Times.Once);

            this.accessServiceMock.VerifyNoOtherCalls();
        }
    }
}
