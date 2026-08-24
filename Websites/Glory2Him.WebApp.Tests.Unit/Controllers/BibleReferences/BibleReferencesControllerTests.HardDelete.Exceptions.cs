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
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Glory2Him.Core.Models.Foundations.BibleReferences.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RESTFulSense.Clients.Extensions;
using RESTFulSense.Models;
using Xeptions;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.BibleReferences
{
    public partial class BibleReferencesControllerTests
    {
        [Theory]
        [MemberData(nameof(ValidationExceptions))]
        public async Task ShouldReturnBadRequestOnHardDeleteIfValidationErrorOccurredAsync(
            Xeption validationException)
        {
            // given
            Guid someId = Guid.NewGuid();

            BadRequestObjectResult expectedBadRequestObjectResult =
                BadRequest(validationException.InnerException);

            var expectedActionResult =
                new ActionResult<BibleReference>(expectedBadRequestObjectResult);

            this.bibleReferenceServiceMock.Setup(service =>
                service.HardRemoveBibleReferenceByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(validationException);

            // when
            ActionResult<BibleReference> actualActionResult =
                await this.bibleReferencesController.HardDeleteBibleReferenceByIdAsync(someId, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.bibleReferenceServiceMock.Verify(service =>
                service.HardRemoveBibleReferenceByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.bibleReferenceServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ServerExceptions))]
        public async Task ShouldReturnInternalServerErrorOnHardDeleteIfServerErrorOccurredAsync(
            Xeption validationException)
        {
            // given
            Guid someId = Guid.NewGuid();

            InternalServerErrorObjectResult expectedInternalServerErrorObjectResult =
                InternalServerError(validationException);

            var expectedActionResult =
                new ActionResult<BibleReference>(expectedInternalServerErrorObjectResult);

            this.bibleReferenceServiceMock.Setup(service =>
                service.HardRemoveBibleReferenceByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(validationException);

            // when
            ActionResult<BibleReference> actualActionResult =
                await this.bibleReferencesController.HardDeleteBibleReferenceByIdAsync(someId, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.bibleReferenceServiceMock.Verify(service =>
                service.HardRemoveBibleReferenceByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.bibleReferenceServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnNotFoundOnHardDeleteIfItemDoesNotExistAsync()
        {
            // given
            Guid someId = Guid.NewGuid();
            string someMessage = GetRandomString();

            var notFoundBibleReferenceException =
                new NotFoundBibleReferenceException(
                    message: someMessage);

            var bibleReferenceValidationException =
                new BibleReferenceValidationException(
                    message: someMessage,
                    innerException: notFoundBibleReferenceException);

            NotFoundObjectResult expectedNotFoundObjectResult =
                NotFound(notFoundBibleReferenceException);

            var expectedActionResult =
                new ActionResult<BibleReference>(expectedNotFoundObjectResult);

            this.bibleReferenceServiceMock.Setup(service =>
                service.HardRemoveBibleReferenceByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(bibleReferenceValidationException);

            // when
            ActionResult<BibleReference> actualActionResult =
                await this.bibleReferencesController.HardDeleteBibleReferenceByIdAsync(someId, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.bibleReferenceServiceMock.Verify(service =>
                service.HardRemoveBibleReferenceByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.bibleReferenceServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnUnauthorizedOnHardDeleteIfUnauthorizedErrorOccurredAsync()
        {
            // given
            Guid someId = Guid.NewGuid();
            string someMessage = GetRandomString();

            var unauthorizedBibleReferenceException =
                new UnauthorizedBibleReferenceException(
                    message: someMessage);

            var bibleReferenceValidationException =
                new BibleReferenceValidationException(
                    message: someMessage,
                    innerException: unauthorizedBibleReferenceException);

            UnauthorizedObjectResult expectedUnauthorizedObjectResult =
                Unauthorized(unauthorizedBibleReferenceException);

            var expectedActionResult =
                new ActionResult<BibleReference>(expectedUnauthorizedObjectResult);

            this.bibleReferenceServiceMock.Setup(service =>
                service.HardRemoveBibleReferenceByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(bibleReferenceValidationException);

            // when
            ActionResult<BibleReference> actualActionResult =
                await this.bibleReferencesController.HardDeleteBibleReferenceByIdAsync(someId, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.bibleReferenceServiceMock.Verify(service =>
                service.HardRemoveBibleReferenceByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.bibleReferenceServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnLockedOnHardDeleteIfRecordIsLockedAsync()
        {
            // given
            Guid someId = Guid.NewGuid();
            var someInnerException = new Exception();
            string someMessage = GetRandomString();

            var lockedBibleReferenceException =
                new LockedBibleReferenceException(
                    message: someMessage,
                    innerException: someInnerException,
                    data: someInnerException.Data);

            var bibleReferenceDependencyValidationException =
                new BibleReferenceDependencyValidationException(
                    message: someMessage,
                    innerException: lockedBibleReferenceException);

            LockedObjectResult expectedConflictObjectResult =
                Locked(lockedBibleReferenceException);

            var expectedActionResult =
                new ActionResult<BibleReference>(expectedConflictObjectResult);

            this.bibleReferenceServiceMock.Setup(service =>
                service.HardRemoveBibleReferenceByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(bibleReferenceDependencyValidationException);

            // when
            ActionResult<BibleReference> actualActionResult =
                await this.bibleReferencesController.HardDeleteBibleReferenceByIdAsync(someId, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.bibleReferenceServiceMock.Verify(service =>
                service.HardRemoveBibleReferenceByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.bibleReferenceServiceMock.VerifyNoOtherCalls();
        }
        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldReturnFailedDependencyOnHardDeleteIfDependencyErrorOccurredAsync(
            Xeption dependencyException)
        {
            // given
            Guid someId = Guid.NewGuid();

            FailedDependencyObjectResult expectedFailedDependencyObjectResult =
                FailedDependency(dependencyException.InnerException);

            var expectedActionResult =
                new ActionResult<BibleReference>(expectedFailedDependencyObjectResult);

            this.bibleReferenceServiceMock.Setup(service =>
                service.HardRemoveBibleReferenceByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(dependencyException);

            // when
            ActionResult<BibleReference> actualActionResult =
                await this.bibleReferencesController.HardDeleteBibleReferenceByIdAsync(someId, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.bibleReferenceServiceMock.Verify(service =>
                service.HardRemoveBibleReferenceByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.bibleReferenceServiceMock.VerifyNoOtherCalls();
        }
        [Fact]
        public async Task ShouldReturnConflictOnHardDeleteIfAlreadyExistsBibleReferenceErrorOccurredAsync()
        {
            // given
            Guid someId = Guid.NewGuid();
            var someInnerException = new Exception();
            string someMessage = GetRandomString();

            var alreadyExistsBibleReferenceException =
                new AlreadyExistsBibleReferenceException(
                    message: someMessage,
                    innerException: someInnerException,
                    data: someInnerException.Data);

            var bibleReferenceDependencyValidationException =
                new BibleReferenceDependencyValidationException(
                    message: someMessage,
                    innerException: alreadyExistsBibleReferenceException);

            ConflictObjectResult expectedConflictObjectResult =
                Conflict(alreadyExistsBibleReferenceException);

            var expectedActionResult =
                new ActionResult<BibleReference>(expectedConflictObjectResult);

            this.bibleReferenceServiceMock.Setup(service =>
                service.HardRemoveBibleReferenceByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(bibleReferenceDependencyValidationException);

            // when
            ActionResult<BibleReference> actualActionResult =
                await this.bibleReferencesController.HardDeleteBibleReferenceByIdAsync(someId, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.bibleReferenceServiceMock.Verify(service =>
                service.HardRemoveBibleReferenceByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.bibleReferenceServiceMock.VerifyNoOtherCalls();
        }
    }
}
