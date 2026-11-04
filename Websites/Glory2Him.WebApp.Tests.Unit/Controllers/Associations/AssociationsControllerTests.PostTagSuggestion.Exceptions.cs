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
using Glory2Him.Core.Models.Foundations.Associations.Exceptions;
using Glory2Him.Core.Models.Orchestrations.Associations;
using Glory2Him.Core.Models.Orchestrations.Associations.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RESTFulSense.Clients.Extensions;
using RESTFulSense.Models;
using Xeptions;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.Associations
{
    public partial class AssociationsControllerTests
    {
        [Fact]
        public async Task ShouldReturnUnauthorizedOnPostTagSuggestionIfUnauthorizedErrorOccurredAsync()
        {
            // given
            TagSuggestion someTagSuggestion = CreateRandomTagSuggestion();
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            string someMessage = GetRandomString();

            var unauthorizedAssociationOrchestrationException =
                new UnauthorizedAssociationOrchestrationException(
                    message: someMessage);

            var associationOrchestrationValidationException =
                new AssociationOrchestrationValidationException(
                    message: someMessage,
                    innerException: unauthorizedAssociationOrchestrationException);

            UnauthorizedObjectResult expectedUnauthorizedObjectResult =
                Unauthorized(unauthorizedAssociationOrchestrationException);

            var expectedActionResult =
                new ActionResult<AssociationSubmissionResult>(expectedUnauthorizedObjectResult);

            this.associationOrchestrationServiceMock.Setup(service =>
                service.SuggestTagAsync(someTagSuggestion, cancellationToken))
                    .ThrowsAsync(associationOrchestrationValidationException);

            // when
            ActionResult<AssociationSubmissionResult> actualActionResult =
                await this.associationsController.PostTagSuggestionAsync(someTagSuggestion, cancellationToken);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.associationOrchestrationServiceMock.Verify(service =>
                service.SuggestTagAsync(someTagSuggestion, cancellationToken),
                    Times.Once);

            this.associationOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnNotFoundOnPostTagSuggestionIfThePostIsNotFoundAsync()
        {
            // given
            TagSuggestion someTagSuggestion = CreateRandomTagSuggestion();
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            string someMessage = GetRandomString();

            var notFoundAssociationOrchestrationException =
                new NotFoundAssociationOrchestrationException(
                    message: someMessage);

            var associationOrchestrationValidationException =
                new AssociationOrchestrationValidationException(
                    message: someMessage,
                    innerException: notFoundAssociationOrchestrationException);

            NotFoundObjectResult expectedNotFoundObjectResult =
                NotFound(notFoundAssociationOrchestrationException);

            var expectedActionResult =
                new ActionResult<AssociationSubmissionResult>(expectedNotFoundObjectResult);

            this.associationOrchestrationServiceMock.Setup(service =>
                service.SuggestTagAsync(someTagSuggestion, cancellationToken))
                    .ThrowsAsync(associationOrchestrationValidationException);

            // when
            ActionResult<AssociationSubmissionResult> actualActionResult =
                await this.associationsController.PostTagSuggestionAsync(someTagSuggestion, cancellationToken);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.associationOrchestrationServiceMock.Verify(service =>
                service.SuggestTagAsync(someTagSuggestion, cancellationToken),
                    Times.Once);

            this.associationOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnBadRequestOnPostTagSuggestionIfValidationErrorOccurredAsync()
        {
            // given
            TagSuggestion someTagSuggestion = CreateRandomTagSuggestion();
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            var someInnerException = new Xeption();

            var associationOrchestrationValidationException =
                new AssociationOrchestrationValidationException(
                    message: GetRandomString(),
                    innerException: someInnerException);

            BadRequestObjectResult expectedBadRequestObjectResult =
                BadRequest(someInnerException);

            var expectedActionResult =
                new ActionResult<AssociationSubmissionResult>(expectedBadRequestObjectResult);

            this.associationOrchestrationServiceMock.Setup(service =>
                service.SuggestTagAsync(someTagSuggestion, cancellationToken))
                    .ThrowsAsync(associationOrchestrationValidationException);

            // when
            ActionResult<AssociationSubmissionResult> actualActionResult =
                await this.associationsController.PostTagSuggestionAsync(someTagSuggestion, cancellationToken);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.associationOrchestrationServiceMock.Verify(service =>
                service.SuggestTagAsync(someTagSuggestion, cancellationToken),
                    Times.Once);

            this.associationOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnConflictOnPostTagSuggestionIfAlreadyExistsErrorOccurredAsync()
        {
            // given
            TagSuggestion someTagSuggestion = CreateRandomTagSuggestion();
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            var someInnerException = new Exception();
            string someMessage = GetRandomString();

            var alreadyExistsAssociationException =
                new AlreadyExistsAssociationException(
                    message: someMessage,
                    innerException: someInnerException,
                    data: someInnerException.Data);

            var associationOrchestrationDependencyValidationException =
                new AssociationOrchestrationDependencyValidationException(
                    message: someMessage,
                    innerException: alreadyExistsAssociationException);

            ConflictObjectResult expectedConflictObjectResult =
                Conflict(alreadyExistsAssociationException);

            var expectedActionResult =
                new ActionResult<AssociationSubmissionResult>(expectedConflictObjectResult);

            this.associationOrchestrationServiceMock.Setup(service =>
                service.SuggestTagAsync(someTagSuggestion, cancellationToken))
                    .ThrowsAsync(associationOrchestrationDependencyValidationException);

            // when
            ActionResult<AssociationSubmissionResult> actualActionResult =
                await this.associationsController.PostTagSuggestionAsync(someTagSuggestion, cancellationToken);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.associationOrchestrationServiceMock.Verify(service =>
                service.SuggestTagAsync(someTagSuggestion, cancellationToken),
                    Times.Once);

            this.associationOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnBadRequestOnPostTagSuggestionIfDependencyValidationErrorOccurredAsync()
        {
            // given
            TagSuggestion someTagSuggestion = CreateRandomTagSuggestion();
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            var someInnerException = new Xeption();

            var associationOrchestrationDependencyValidationException =
                new AssociationOrchestrationDependencyValidationException(
                    message: GetRandomString(),
                    innerException: someInnerException);

            BadRequestObjectResult expectedBadRequestObjectResult =
                BadRequest(someInnerException);

            var expectedActionResult =
                new ActionResult<AssociationSubmissionResult>(expectedBadRequestObjectResult);

            this.associationOrchestrationServiceMock.Setup(service =>
                service.SuggestTagAsync(someTagSuggestion, cancellationToken))
                    .ThrowsAsync(associationOrchestrationDependencyValidationException);

            // when
            ActionResult<AssociationSubmissionResult> actualActionResult =
                await this.associationsController.PostTagSuggestionAsync(someTagSuggestion, cancellationToken);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.associationOrchestrationServiceMock.Verify(service =>
                service.SuggestTagAsync(someTagSuggestion, cancellationToken),
                    Times.Once);

            this.associationOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnFailedDependencyOnPostTagSuggestionIfDependencyErrorOccurredAsync()
        {
            // given
            TagSuggestion someTagSuggestion = CreateRandomTagSuggestion();
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            var someInnerException = new Xeption();

            var associationOrchestrationDependencyException =
                new AssociationOrchestrationDependencyException(
                    message: GetRandomString(),
                    innerException: someInnerException);

            FailedDependencyObjectResult expectedFailedDependencyObjectResult =
                FailedDependency(someInnerException);

            var expectedActionResult =
                new ActionResult<AssociationSubmissionResult>(expectedFailedDependencyObjectResult);

            this.associationOrchestrationServiceMock.Setup(service =>
                service.SuggestTagAsync(someTagSuggestion, cancellationToken))
                    .ThrowsAsync(associationOrchestrationDependencyException);

            // when
            ActionResult<AssociationSubmissionResult> actualActionResult =
                await this.associationsController.PostTagSuggestionAsync(someTagSuggestion, cancellationToken);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.associationOrchestrationServiceMock.Verify(service =>
                service.SuggestTagAsync(someTagSuggestion, cancellationToken),
                    Times.Once);

            this.associationOrchestrationServiceMock.VerifyNoOtherCalls();
        }
    }
}
