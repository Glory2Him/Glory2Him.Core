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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments;
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments.Exceptions;
using Glory2Him.Core.Models.Orchestrations.AIReviewers;
using Glory2Him.Core.Models.Orchestrations.AIReviewers.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RESTFulSense.Clients.Extensions;
using RESTFulSense.Models;
using Xeptions;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.AIReviewers
{
    public partial class AIReviewersControllerTests
    {
        [Fact]
        public async Task ShouldReturnStatusOnGetAIReviewerAsync()
        {
            // given
            EntityType randomEntityType = GetRandomEntityType();
            Guid randomEntityId = Guid.NewGuid();
            AIReviewerStatus randomAIReviewerStatus = CreateRandomAIReviewerStatus();

            this.aiReviewerOrchestrationServiceMock.Setup(service =>
                service.RetrieveAIReviewerStatusAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(randomAIReviewerStatus);

            // when
            ActionResult<AIReviewerStatus> actualActionResult =
                await this.aiReviewersController.GetAIReviewerAsync(
                    randomEntityType,
                    randomEntityId,
                    default);

            // then
            actualActionResult.Result.Should().BeOfType<OkObjectResult>();

            ((OkObjectResult)actualActionResult.Result).Value
                .Should().BeSameAs(randomAIReviewerStatus);

            // and the route values reached the service unaltered — the exposer maps, it does not
            // interpret
            this.aiReviewerOrchestrationServiceMock.Verify(service =>
                service.RetrieveAIReviewerStatusAsync(
                    randomEntityType,
                    randomEntityId,
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.aiReviewerOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ValidationExceptions))]
        public async Task ShouldReturnBadRequestOnGetAIReviewerIfValidationErrorAsync(
            Xeption validationException)
        {
            // given
            BadRequestObjectResult expectedBadRequestObjectResult =
                BadRequest(validationException.InnerException);

            var expectedActionResult =
                new ActionResult<AIReviewerStatus>(expectedBadRequestObjectResult);

            this.aiReviewerOrchestrationServiceMock.Setup(service =>
                service.RetrieveAIReviewerStatusAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(validationException);

            // when
            ActionResult<AIReviewerStatus> actualActionResult =
                await this.aiReviewersController.GetAIReviewerAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.aiReviewerOrchestrationServiceMock.Verify(service =>
                service.RetrieveAIReviewerStatusAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.aiReviewerOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnUnauthorizedOnGetAIReviewerIfRefusedAsync()
        {
            // given
            var unauthorizedException = new UnauthorizedAIReviewerOrchestrationException(
                message: GetRandomString());

            var validationException = new AIReviewerOrchestrationValidationException(
                message: GetRandomString(),
                innerException: unauthorizedException);

            UnauthorizedObjectResult expectedUnauthorizedObjectResult =
                Unauthorized(unauthorizedException);

            var expectedActionResult =
                new ActionResult<AIReviewerStatus>(expectedUnauthorizedObjectResult);

            this.aiReviewerOrchestrationServiceMock.Setup(service =>
                service.RetrieveAIReviewerStatusAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(validationException);

            // when
            ActionResult<AIReviewerStatus> actualActionResult =
                await this.aiReviewersController.GetAIReviewerAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.aiReviewerOrchestrationServiceMock.Verify(service =>
                service.RetrieveAIReviewerStatusAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.aiReviewerOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnNotFoundOnGetAIReviewerIfApprovalDoesNotExistAsync()
        {
            // given
            string someMessage = GetRandomString();

            var notFoundAIReviewerOrchestrationException =
                new NotFoundAIReviewerOrchestrationException(message: someMessage);

            var aiReviewerOrchestrationValidationException =
                new AIReviewerOrchestrationValidationException(
                    message: someMessage,
                    innerException: notFoundAIReviewerOrchestrationException);

            NotFoundObjectResult expectedNotFoundObjectResult =
                NotFound(notFoundAIReviewerOrchestrationException);

            var expectedActionResult =
                new ActionResult<AIReviewerStatus>(expectedNotFoundObjectResult);

            this.aiReviewerOrchestrationServiceMock.Setup(service =>
                service.RetrieveAIReviewerStatusAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(aiReviewerOrchestrationValidationException);

            // when
            ActionResult<AIReviewerStatus> actualActionResult =
                await this.aiReviewersController.GetAIReviewerAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.aiReviewerOrchestrationServiceMock.Verify(service =>
                service.RetrieveAIReviewerStatusAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.aiReviewerOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldReturnFailedDependencyOnGetAIReviewerIfDependencyErrorAsync(
            Xeption dependencyException)
        {
            // given
            FailedDependencyObjectResult expectedFailedDependencyObjectResult =
                FailedDependency(dependencyException.InnerException);

            var expectedActionResult =
                new ActionResult<AIReviewerStatus>(expectedFailedDependencyObjectResult);

            this.aiReviewerOrchestrationServiceMock.Setup(service =>
                service.RetrieveAIReviewerStatusAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(dependencyException);

            // when
            ActionResult<AIReviewerStatus> actualActionResult =
                await this.aiReviewersController.GetAIReviewerAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.aiReviewerOrchestrationServiceMock.Verify(service =>
                service.RetrieveAIReviewerStatusAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.aiReviewerOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ServerExceptions))]
        public async Task ShouldReturnInternalServerErrorOnGetAIReviewerIfServerErrorAsync(
            Xeption serverException)
        {
            // given
            InternalServerErrorObjectResult expectedInternalServerErrorObjectResult =
                InternalServerError(serverException);

            var expectedActionResult =
                new ActionResult<AIReviewerStatus>(expectedInternalServerErrorObjectResult);

            this.aiReviewerOrchestrationServiceMock.Setup(service =>
                service.RetrieveAIReviewerStatusAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(serverException);

            // when
            ActionResult<AIReviewerStatus> actualActionResult =
                await this.aiReviewersController.GetAIReviewerAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.aiReviewerOrchestrationServiceMock.Verify(service =>
                service.RetrieveAIReviewerStatusAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.aiReviewerOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// The upsert always has something real to hand back — create, reset or no-op all end
        /// with a live row — so this is <c>200</c>, never the <c>204</c> the human invitation
        /// answers with.
        /// </summary>
        [Fact]
        public async Task ShouldReturnAssignmentOnPostAIReviewerAsync()
        {
            // given
            EntityType randomEntityType = GetRandomEntityType();
            Guid randomEntityId = Guid.NewGuid();
            var randomAssignment = new AIReviewerAssignment { Id = Guid.NewGuid() };

            this.aiReviewerOrchestrationServiceMock.Setup(service =>
                service.RequestAIReviewerAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(randomAssignment);

            // when
            ActionResult<AIReviewerAssignment> actualActionResult =
                await this.aiReviewersController.PostAIReviewerAsync(
                    randomEntityType,
                    randomEntityId,
                    default);

            // then
            actualActionResult.Result.Should().BeOfType<OkObjectResult>();

            ((OkObjectResult)actualActionResult.Result).Value
                .Should().BeSameAs(randomAssignment);

            this.aiReviewerOrchestrationServiceMock.Verify(service =>
                service.RequestAIReviewerAsync(
                    randomEntityType,
                    randomEntityId,
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.aiReviewerOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ValidationExceptions))]
        public async Task ShouldReturnBadRequestOnPostAIReviewerIfValidationErrorAsync(
            Xeption validationException)
        {
            // given
            BadRequestObjectResult expectedBadRequestObjectResult =
                BadRequest(validationException.InnerException);

            var expectedActionResult =
                new ActionResult<AIReviewerAssignment>(expectedBadRequestObjectResult);

            this.aiReviewerOrchestrationServiceMock.Setup(service =>
                service.RequestAIReviewerAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(validationException);

            // when
            ActionResult<AIReviewerAssignment> actualActionResult =
                await this.aiReviewersController.PostAIReviewerAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.aiReviewerOrchestrationServiceMock.Verify(service =>
                service.RequestAIReviewerAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.aiReviewerOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// The one collision the orchestration's re-read cannot dissolve — the winning row
        /// withdrawn in the sliver between the collision and the re-read — is a <c>409</c>, not
        /// the <c>400</c> every other dependency-validation refusal on this action gets. The
        /// caller's view of the round is stale rather than wrong, which is what 409 says.
        ///
        /// <para>Ordered ABOVE the plain BadRequest arm in the controller, so this test also pins
        /// that ordering: the two arms catch the SAME exception type and differ only by filter, so
        /// swapping them makes this one unreachable and turns every 409 into a 400. The
        /// <c>ValidationExceptions</c> theory above feeds a plain dependency-validation exception
        /// through the same action, so the pair together prove the filter discriminates rather
        /// than swallows.</para>
        /// </summary>
        [Fact]
        public async Task ShouldReturnConflictOnPostAIReviewerIfAlreadyExistsErrorOccurredAsync()
        {
            // given
            var someInnerException = new Exception();
            string someMessage = GetRandomString();

            var alreadyExistsAIReviewerAssignmentException =
                new AlreadyExistsAIReviewerAssignmentException(
                    message: someMessage,
                    innerException: someInnerException,
                    data: someInnerException.Data);

            var aiReviewerOrchestrationDependencyValidationException =
                new AIReviewerOrchestrationDependencyValidationException(
                    message: someMessage,
                    innerException: alreadyExistsAIReviewerAssignmentException);

            ConflictObjectResult expectedConflictObjectResult =
                Conflict(alreadyExistsAIReviewerAssignmentException);

            var expectedActionResult =
                new ActionResult<AIReviewerAssignment>(expectedConflictObjectResult);

            this.aiReviewerOrchestrationServiceMock.Setup(service =>
                service.RequestAIReviewerAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(aiReviewerOrchestrationDependencyValidationException);

            // when
            ActionResult<AIReviewerAssignment> actualActionResult =
                await this.aiReviewersController.PostAIReviewerAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.aiReviewerOrchestrationServiceMock.Verify(service =>
                service.RequestAIReviewerAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.aiReviewerOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// And the collision arm is the POST'S ALONE. The status read cannot produce it — nothing
        /// there writes an assignment — and the withdrawal answers a vanished row with a
        /// <c>204</c> rather than a conflict, so the same exception reaching either of those
        /// actions is an ordinary <c>400</c>.
        /// </summary>
        [Fact]
        public async Task ShouldReturnBadRequestOnDeleteAIReviewerIfAlreadyExistsErrorOccurredAsync()
        {
            // given
            var someInnerException = new Exception();
            string someMessage = GetRandomString();

            var alreadyExistsAIReviewerAssignmentException =
                new AlreadyExistsAIReviewerAssignmentException(
                    message: someMessage,
                    innerException: someInnerException,
                    data: someInnerException.Data);

            var aiReviewerOrchestrationDependencyValidationException =
                new AIReviewerOrchestrationDependencyValidationException(
                    message: someMessage,
                    innerException: alreadyExistsAIReviewerAssignmentException);

            BadRequestObjectResult expectedBadRequestObjectResult =
                BadRequest(alreadyExistsAIReviewerAssignmentException);

            var expectedActionResult =
                new ActionResult<AIReviewerAssignment>(expectedBadRequestObjectResult);

            this.aiReviewerOrchestrationServiceMock.Setup(service =>
                service.WithdrawAIReviewerAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(aiReviewerOrchestrationDependencyValidationException);

            // when
            ActionResult<AIReviewerAssignment> actualActionResult =
                await this.aiReviewersController.DeleteAIReviewerAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.aiReviewerOrchestrationServiceMock.Verify(service =>
                service.WithdrawAIReviewerAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.aiReviewerOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnUnauthorizedOnPostAIReviewerIfRefusedAsync()
        {
            // given
            var unauthorizedException = new UnauthorizedAIReviewerOrchestrationException(
                message: GetRandomString());

            var validationException = new AIReviewerOrchestrationValidationException(
                message: GetRandomString(),
                innerException: unauthorizedException);

            UnauthorizedObjectResult expectedUnauthorizedObjectResult =
                Unauthorized(unauthorizedException);

            var expectedActionResult =
                new ActionResult<AIReviewerAssignment>(expectedUnauthorizedObjectResult);

            this.aiReviewerOrchestrationServiceMock.Setup(service =>
                service.RequestAIReviewerAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(validationException);

            // when
            ActionResult<AIReviewerAssignment> actualActionResult =
                await this.aiReviewersController.PostAIReviewerAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.aiReviewerOrchestrationServiceMock.Verify(service =>
                service.RequestAIReviewerAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.aiReviewerOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnNotFoundOnPostAIReviewerIfApprovalDoesNotExistAsync()
        {
            // given
            string someMessage = GetRandomString();

            var notFoundAIReviewerOrchestrationException =
                new NotFoundAIReviewerOrchestrationException(message: someMessage);

            var aiReviewerOrchestrationValidationException =
                new AIReviewerOrchestrationValidationException(
                    message: someMessage,
                    innerException: notFoundAIReviewerOrchestrationException);

            NotFoundObjectResult expectedNotFoundObjectResult =
                NotFound(notFoundAIReviewerOrchestrationException);

            var expectedActionResult =
                new ActionResult<AIReviewerAssignment>(expectedNotFoundObjectResult);

            this.aiReviewerOrchestrationServiceMock.Setup(service =>
                service.RequestAIReviewerAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(aiReviewerOrchestrationValidationException);

            // when
            ActionResult<AIReviewerAssignment> actualActionResult =
                await this.aiReviewersController.PostAIReviewerAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.aiReviewerOrchestrationServiceMock.Verify(service =>
                service.RequestAIReviewerAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.aiReviewerOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldReturnFailedDependencyOnPostAIReviewerIfDependencyErrorAsync(
            Xeption dependencyException)
        {
            // given
            FailedDependencyObjectResult expectedFailedDependencyObjectResult =
                FailedDependency(dependencyException.InnerException);

            var expectedActionResult =
                new ActionResult<AIReviewerAssignment>(expectedFailedDependencyObjectResult);

            this.aiReviewerOrchestrationServiceMock.Setup(service =>
                service.RequestAIReviewerAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(dependencyException);

            // when
            ActionResult<AIReviewerAssignment> actualActionResult =
                await this.aiReviewersController.PostAIReviewerAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.aiReviewerOrchestrationServiceMock.Verify(service =>
                service.RequestAIReviewerAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.aiReviewerOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ServerExceptions))]
        public async Task ShouldReturnInternalServerErrorOnPostAIReviewerIfServerErrorAsync(
            Xeption serverException)
        {
            // given
            InternalServerErrorObjectResult expectedInternalServerErrorObjectResult =
                InternalServerError(serverException);

            var expectedActionResult =
                new ActionResult<AIReviewerAssignment>(expectedInternalServerErrorObjectResult);

            this.aiReviewerOrchestrationServiceMock.Setup(service =>
                service.RequestAIReviewerAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(serverException);

            // when
            ActionResult<AIReviewerAssignment> actualActionResult =
                await this.aiReviewersController.PostAIReviewerAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.aiReviewerOrchestrationServiceMock.Verify(service =>
                service.RequestAIReviewerAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.aiReviewerOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnRemovedAssignmentOnDeleteAIReviewerAsync()
        {
            // given
            EntityType randomEntityType = GetRandomEntityType();
            Guid randomEntityId = Guid.NewGuid();
            var randomAssignment = new AIReviewerAssignment { Id = Guid.NewGuid() };

            this.aiReviewerOrchestrationServiceMock.Setup(service =>
                service.WithdrawAIReviewerAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(randomAssignment);

            // when
            ActionResult<AIReviewerAssignment> actualActionResult =
                await this.aiReviewersController.DeleteAIReviewerAsync(
                    randomEntityType,
                    randomEntityId,
                    default);

            // then
            actualActionResult.Result.Should().BeOfType<OkObjectResult>();

            ((OkObjectResult)actualActionResult.Result).Value
                .Should().BeSameAs(randomAssignment);

            this.aiReviewerOrchestrationServiceMock.Verify(service =>
                service.WithdrawAIReviewerAsync(
                    randomEntityType,
                    randomEntityId,
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.aiReviewerOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Idempotent — nothing assigned is a <c>204</c>, not a <c>404</c>: withdrawing twice, or
        /// withdrawing what was never there, is a stale panel rather than a mistake.
        /// </summary>
        [Fact]
        public async Task ShouldReturnNoContentOnDeleteAIReviewerWhenNothingWasAssignedAsync()
        {
            // given
            this.aiReviewerOrchestrationServiceMock.Setup(service =>
                service.WithdrawAIReviewerAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((AIReviewerAssignment)null);

            // when
            ActionResult<AIReviewerAssignment> actualActionResult =
                await this.aiReviewersController.DeleteAIReviewerAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    default);

            // then
            actualActionResult.Result.Should().BeOfType<NoContentResult>();
        }

        [Theory]
        [MemberData(nameof(ValidationExceptions))]
        public async Task ShouldReturnBadRequestOnDeleteAIReviewerIfValidationErrorAsync(
            Xeption validationException)
        {
            // given
            BadRequestObjectResult expectedBadRequestObjectResult =
                BadRequest(validationException.InnerException);

            var expectedActionResult =
                new ActionResult<AIReviewerAssignment>(expectedBadRequestObjectResult);

            this.aiReviewerOrchestrationServiceMock.Setup(service =>
                service.WithdrawAIReviewerAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(validationException);

            // when
            ActionResult<AIReviewerAssignment> actualActionResult =
                await this.aiReviewersController.DeleteAIReviewerAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.aiReviewerOrchestrationServiceMock.Verify(service =>
                service.WithdrawAIReviewerAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.aiReviewerOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnUnauthorizedOnDeleteAIReviewerIfRefusedAsync()
        {
            // given
            var unauthorizedException = new UnauthorizedAIReviewerOrchestrationException(
                message: GetRandomString());

            var validationException = new AIReviewerOrchestrationValidationException(
                message: GetRandomString(),
                innerException: unauthorizedException);

            UnauthorizedObjectResult expectedUnauthorizedObjectResult =
                Unauthorized(unauthorizedException);

            var expectedActionResult =
                new ActionResult<AIReviewerAssignment>(expectedUnauthorizedObjectResult);

            this.aiReviewerOrchestrationServiceMock.Setup(service =>
                service.WithdrawAIReviewerAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(validationException);

            // when
            ActionResult<AIReviewerAssignment> actualActionResult =
                await this.aiReviewersController.DeleteAIReviewerAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.aiReviewerOrchestrationServiceMock.Verify(service =>
                service.WithdrawAIReviewerAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.aiReviewerOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnNotFoundOnDeleteAIReviewerIfApprovalDoesNotExistAsync()
        {
            // given
            string someMessage = GetRandomString();

            var notFoundAIReviewerOrchestrationException =
                new NotFoundAIReviewerOrchestrationException(message: someMessage);

            var aiReviewerOrchestrationValidationException =
                new AIReviewerOrchestrationValidationException(
                    message: someMessage,
                    innerException: notFoundAIReviewerOrchestrationException);

            NotFoundObjectResult expectedNotFoundObjectResult =
                NotFound(notFoundAIReviewerOrchestrationException);

            var expectedActionResult =
                new ActionResult<AIReviewerAssignment>(expectedNotFoundObjectResult);

            this.aiReviewerOrchestrationServiceMock.Setup(service =>
                service.WithdrawAIReviewerAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(aiReviewerOrchestrationValidationException);

            // when
            ActionResult<AIReviewerAssignment> actualActionResult =
                await this.aiReviewersController.DeleteAIReviewerAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.aiReviewerOrchestrationServiceMock.Verify(service =>
                service.WithdrawAIReviewerAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.aiReviewerOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldReturnFailedDependencyOnDeleteAIReviewerIfDependencyErrorAsync(
            Xeption dependencyException)
        {
            // given
            FailedDependencyObjectResult expectedFailedDependencyObjectResult =
                FailedDependency(dependencyException.InnerException);

            var expectedActionResult =
                new ActionResult<AIReviewerAssignment>(expectedFailedDependencyObjectResult);

            this.aiReviewerOrchestrationServiceMock.Setup(service =>
                service.WithdrawAIReviewerAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(dependencyException);

            // when
            ActionResult<AIReviewerAssignment> actualActionResult =
                await this.aiReviewersController.DeleteAIReviewerAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.aiReviewerOrchestrationServiceMock.Verify(service =>
                service.WithdrawAIReviewerAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.aiReviewerOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ServerExceptions))]
        public async Task ShouldReturnInternalServerErrorOnDeleteAIReviewerIfServerErrorAsync(
            Xeption serverException)
        {
            // given
            InternalServerErrorObjectResult expectedInternalServerErrorObjectResult =
                InternalServerError(serverException);

            var expectedActionResult =
                new ActionResult<AIReviewerAssignment>(expectedInternalServerErrorObjectResult);

            this.aiReviewerOrchestrationServiceMock.Setup(service =>
                service.WithdrawAIReviewerAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(serverException);

            // when
            ActionResult<AIReviewerAssignment> actualActionResult =
                await this.aiReviewersController.DeleteAIReviewerAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.aiReviewerOrchestrationServiceMock.Verify(service =>
                service.WithdrawAIReviewerAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.aiReviewerOrchestrationServiceMock.VerifyNoOtherCalls();
        }
    }
}
