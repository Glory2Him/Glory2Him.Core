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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Force.DeepCloner;
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Approvals.Exceptions;
using Glory2Him.Core.Models.Orchestrations.Approvals;
using Glory2Him.Core.Models.Orchestrations.Approvals.Exceptions;
using Glory2Him.Core.Services.Orchestrations.Approvals;
using Glory2Him.WebApp.Controllers.Approvals;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Moq;
using RESTFulSense.Clients.Extensions;
using RESTFulSense.Controllers;
using RESTFulSense.Models;
using Tynamix.ObjectFiller;
using Xeptions;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.Approvals
{
    /// <summary>
    /// Kept in one file rather than split into the per-operation partials the sibling exposer
    /// suites use. That began as an argument from size — two actions, and the split would have
    /// produced six files of three tests each — which no longer holds now the controller carries
    /// six actions. What keeps it together is the three security theories at the foot of the
    /// file: they enumerate every action by name, and a reader checking that a new endpoint was
    /// added to all three should not have to open a second file to see the tests it was added
    /// alongside.
    /// </summary>
    public class ApprovalsControllerTests : RESTFulController
    {
        private readonly Mock<IApprovalOrchestrationService> approvalOrchestrationServiceMock;
        private readonly ApprovalsController approvalsController;

        public ApprovalsControllerTests()
        {
            approvalOrchestrationServiceMock = new Mock<IApprovalOrchestrationService>();

            approvalsController =
                new ApprovalsController(approvalOrchestrationServiceMock.Object);
        }

        public static TheoryData<Xeption> ValidationExceptions()
        {
            var someInnerException = new Xeption();
            string someMessage = GetRandomString();

            return new TheoryData<Xeption>
            {
                new ApprovalOrchestrationValidationException(
                    message: someMessage,
                    innerException: someInnerException),

                new ApprovalOrchestrationDependencyValidationException(
                    message: someMessage,
                    innerException: someInnerException)
            };
        }

        public static TheoryData<Xeption> DependencyExceptions()
        {
            var someInnerException = new Xeption();
            string someMessage = GetRandomString();

            return new TheoryData<Xeption>
            {
                new ApprovalOrchestrationDependencyException(
                    message: someMessage,
                    innerException: someInnerException)
            };
        }

        public static TheoryData<Xeption> ServerExceptions()
        {
            var someInnerException = new Xeption();
            string someMessage = GetRandomString();

            return new TheoryData<Xeption>
            {
                new ApprovalOrchestrationServiceException(
                    message: someMessage,
                    innerException: someInnerException)
            };
        }

        [Fact]
        public async Task ShouldReturnVerdictOnGetVerdictAsync()
        {
            // given
            EntityType randomEntityType = GetRandomEntityType();
            Guid randomEntityId = Guid.NewGuid();
            ApprovalVerdict randomVerdict = CreateRandomApprovalVerdict();
            ApprovalVerdict storageVerdict = randomVerdict;
            ApprovalVerdict expectedVerdict = storageVerdict.DeepClone();

            var expectedObjectResult =
                new OkObjectResult(expectedVerdict);

            var expectedActionResult =
                new ActionResult<ApprovalVerdict>(expectedObjectResult);

            this.approvalOrchestrationServiceMock.Setup(service =>
                service.RetrieveApprovalVerdictAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageVerdict);

            // when
            ActionResult<ApprovalVerdict> actualActionResult =
                await this.approvalsController.GetApprovalVerdictAsync(
                    randomEntityType,
                    randomEntityId,
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalOrchestrationServiceMock.Verify(service =>
                service.RetrieveApprovalVerdictAsync(
                    randomEntityType,
                    randomEntityId,
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ValidationExceptions))]
        public async Task ShouldReturnBadRequestOnGetVerdictIfValidationErrorOccurredAsync(
            Xeption validationException)
        {
            // given
            EntityType someEntityType = GetRandomEntityType();
            Guid someEntityId = Guid.NewGuid();

            BadRequestObjectResult expectedBadRequestObjectResult =
                BadRequest(validationException.InnerException);

            var expectedActionResult =
                new ActionResult<ApprovalVerdict>(expectedBadRequestObjectResult);

            this.approvalOrchestrationServiceMock.Setup(service =>
                service.RetrieveApprovalVerdictAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(validationException);

            // when
            ActionResult<ApprovalVerdict> actualActionResult =
                await this.approvalsController.GetApprovalVerdictAsync(
                    someEntityType,
                    someEntityId,
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalOrchestrationServiceMock.Verify(service =>
                service.RetrieveApprovalVerdictAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnNotFoundOnGetVerdictIfApprovalDoesNotExistAsync()
        {
            // given
            EntityType someEntityType = GetRandomEntityType();
            Guid someEntityId = Guid.NewGuid();
            string someMessage = GetRandomString();

            var notFoundApprovalOrchestrationException =
                new NotFoundApprovalOrchestrationException(
                    message: someMessage);

            var approvalOrchestrationValidationException =
                new ApprovalOrchestrationValidationException(
                    message: someMessage,
                    innerException: notFoundApprovalOrchestrationException);

            NotFoundObjectResult expectedNotFoundObjectResult =
                NotFound(notFoundApprovalOrchestrationException);

            var expectedActionResult =
                new ActionResult<ApprovalVerdict>(expectedNotFoundObjectResult);

            this.approvalOrchestrationServiceMock.Setup(service =>
                service.RetrieveApprovalVerdictAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(approvalOrchestrationValidationException);

            // when
            ActionResult<ApprovalVerdict> actualActionResult =
                await this.approvalsController.GetApprovalVerdictAsync(
                    someEntityType,
                    someEntityId,
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalOrchestrationServiceMock.Verify(service =>
                service.RetrieveApprovalVerdictAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// The service-side half of the tier gate (§16.7.2). The controller's bare
        /// <c>[Authorize]</c> establishes only that a caller is authenticated, so this 401 is the
        /// answer an authenticated caller below the tier actually receives — which is why the
        /// mapping is pinned rather than left to the catch-all <c>BadRequest</c>.
        /// </summary>
        [Fact]
        public async Task ShouldReturnUnauthorizedOnGetVerdictIfUnauthorizedErrorOccurredAsync()
        {
            // given
            EntityType someEntityType = GetRandomEntityType();
            Guid someEntityId = Guid.NewGuid();
            string someMessage = GetRandomString();

            var unauthorizedApprovalOrchestrationException =
                new UnauthorizedApprovalOrchestrationException(
                    message: someMessage);

            var approvalOrchestrationValidationException =
                new ApprovalOrchestrationValidationException(
                    message: someMessage,
                    innerException: unauthorizedApprovalOrchestrationException);

            UnauthorizedObjectResult expectedUnauthorizedObjectResult =
                Unauthorized(unauthorizedApprovalOrchestrationException);

            var expectedActionResult =
                new ActionResult<ApprovalVerdict>(expectedUnauthorizedObjectResult);

            this.approvalOrchestrationServiceMock.Setup(service =>
                service.RetrieveApprovalVerdictAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(approvalOrchestrationValidationException);

            // when
            ActionResult<ApprovalVerdict> actualActionResult =
                await this.approvalsController.GetApprovalVerdictAsync(
                    someEntityType,
                    someEntityId,
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalOrchestrationServiceMock.Verify(service =>
                service.RetrieveApprovalVerdictAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldReturnFailedDependencyOnGetVerdictIfDependencyErrorOccurredAsync(
            Xeption dependencyException)
        {
            // given
            EntityType someEntityType = GetRandomEntityType();
            Guid someEntityId = Guid.NewGuid();

            FailedDependencyObjectResult expectedFailedDependencyObjectResult =
                FailedDependency(dependencyException.InnerException);

            var expectedActionResult =
                new ActionResult<ApprovalVerdict>(expectedFailedDependencyObjectResult);

            this.approvalOrchestrationServiceMock.Setup(service =>
                service.RetrieveApprovalVerdictAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(dependencyException);

            // when
            ActionResult<ApprovalVerdict> actualActionResult =
                await this.approvalsController.GetApprovalVerdictAsync(
                    someEntityType,
                    someEntityId,
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalOrchestrationServiceMock.Verify(service =>
                service.RetrieveApprovalVerdictAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ServerExceptions))]
        public async Task ShouldReturnInternalServerErrorOnGetVerdictIfServerErrorOccurredAsync(
            Xeption serverException)
        {
            // given
            EntityType someEntityType = GetRandomEntityType();
            Guid someEntityId = Guid.NewGuid();

            InternalServerErrorObjectResult expectedInternalServerErrorObjectResult =
                InternalServerError(serverException);

            var expectedActionResult =
                new ActionResult<ApprovalVerdict>(expectedInternalServerErrorObjectResult);

            this.approvalOrchestrationServiceMock.Setup(service =>
                service.RetrieveApprovalVerdictAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(serverException);

            // when
            ActionResult<ApprovalVerdict> actualActionResult =
                await this.approvalsController.GetApprovalVerdictAsync(
                    someEntityType,
                    someEntityId,
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalOrchestrationServiceMock.Verify(service =>
                service.RetrieveApprovalVerdictAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// The bypass pair is forwarded exactly as it arrived — the verified arguments below are
        /// the point of the test. The controller must not decide that an unexplained bypass is
        /// pointless or that a waiver is unnecessary: both are the orchestration's calls, made
        /// against the stored row and the resolved policy (§9.7.1 rule 3).
        /// </summary>
        [Fact]
        public async Task ShouldReturnOutcomeOnPostDecisionAsync()
        {
            // given
            EntityType randomEntityType = GetRandomEntityType();
            Guid randomEntityId = Guid.NewGuid();
            ApprovalDecision randomDecision = GetRandomApprovalDecision();
            bool randomIsBypassRequested = GetRandomBoolean();
            string randomBypassReason = GetRandomString();
            ApprovalOutcome randomOutcome = CreateRandomApprovalOutcome();
            ApprovalOutcome storageOutcome = randomOutcome;
            ApprovalOutcome expectedOutcome = storageOutcome.DeepClone();

            var expectedObjectResult =
                new OkObjectResult(expectedOutcome);

            var expectedActionResult =
                new ActionResult<ApprovalOutcome>(expectedObjectResult);

            this.approvalOrchestrationServiceMock.Setup(service =>
                service.DecideApprovalAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<ApprovalDecision>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageOutcome);

            // when
            ActionResult<ApprovalOutcome> actualActionResult =
                await this.approvalsController.PostApprovalDecisionAsync(
                    randomEntityType,
                    randomEntityId,
                    randomDecision,
                    randomIsBypassRequested,
                    randomBypassReason,
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalOrchestrationServiceMock.Verify(service =>
                service.DecideApprovalAsync(
                    randomEntityType,
                    randomEntityId,
                    randomDecision,
                    randomIsBypassRequested,
                    randomBypassReason,
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ValidationExceptions))]
        public async Task ShouldReturnBadRequestOnPostDecisionIfValidationErrorOccurredAsync(
            Xeption validationException)
        {
            // given
            BadRequestObjectResult expectedBadRequestObjectResult =
                BadRequest(validationException.InnerException);

            var expectedActionResult =
                new ActionResult<ApprovalOutcome>(expectedBadRequestObjectResult);

            this.approvalOrchestrationServiceMock.Setup(service =>
                service.DecideApprovalAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<ApprovalDecision>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(validationException);

            // when
            ActionResult<ApprovalOutcome> actualActionResult =
                await PostSomeDecisionAsync();

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            VerifyDecideCalledOnce();
        }

        [Fact]
        public async Task ShouldReturnNotFoundOnPostDecisionIfApprovalDoesNotExistAsync()
        {
            // given
            string someMessage = GetRandomString();

            var notFoundApprovalOrchestrationException =
                new NotFoundApprovalOrchestrationException(
                    message: someMessage);

            var approvalOrchestrationValidationException =
                new ApprovalOrchestrationValidationException(
                    message: someMessage,
                    innerException: notFoundApprovalOrchestrationException);

            NotFoundObjectResult expectedNotFoundObjectResult =
                NotFound(notFoundApprovalOrchestrationException);

            var expectedActionResult =
                new ActionResult<ApprovalOutcome>(expectedNotFoundObjectResult);

            this.approvalOrchestrationServiceMock.Setup(service =>
                service.DecideApprovalAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<ApprovalDecision>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(approvalOrchestrationValidationException);

            // when
            ActionResult<ApprovalOutcome> actualActionResult =
                await PostSomeDecisionAsync();

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            VerifyDecideCalledOnce();
        }

        /// <summary>
        /// The one authorisation in the flow (§16.7.1) refusing. It covers every way a decision is
        /// declined — below the tier, self-approval (HR-2), an unpermitted bypass — because the
        /// orchestration deliberately does not re-derive the reason the decision function gave
        /// (§8.6.1 rule 4), so the exposer has exactly one refusal to map.
        /// </summary>
        [Fact]
        public async Task ShouldReturnUnauthorizedOnPostDecisionIfUnauthorizedErrorOccurredAsync()
        {
            // given
            string someMessage = GetRandomString();

            var unauthorizedApprovalOrchestrationException =
                new UnauthorizedApprovalOrchestrationException(
                    message: someMessage);

            var approvalOrchestrationValidationException =
                new ApprovalOrchestrationValidationException(
                    message: someMessage,
                    innerException: unauthorizedApprovalOrchestrationException);

            UnauthorizedObjectResult expectedUnauthorizedObjectResult =
                Unauthorized(unauthorizedApprovalOrchestrationException);

            var expectedActionResult =
                new ActionResult<ApprovalOutcome>(expectedUnauthorizedObjectResult);

            this.approvalOrchestrationServiceMock.Setup(service =>
                service.DecideApprovalAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<ApprovalDecision>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(approvalOrchestrationValidationException);

            // when
            ActionResult<ApprovalOutcome> actualActionResult =
                await PostSomeDecisionAsync();

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            VerifyDecideCalledOnce();
        }

        [Fact]
        public async Task ShouldReturnConflictOnPostDecisionIfAlreadyExistsErrorOccurredAsync()
        {
            // given
            var someInnerException = new Exception();
            string someMessage = GetRandomString();

            var alreadyExistsApprovalException =
                new AlreadyExistsApprovalException(
                    message: someMessage,
                    innerException: someInnerException,
                    data: someInnerException.Data);

            var approvalOrchestrationDependencyValidationException =
                new ApprovalOrchestrationDependencyValidationException(
                    message: someMessage,
                    innerException: alreadyExistsApprovalException);

            ConflictObjectResult expectedConflictObjectResult =
                Conflict(alreadyExistsApprovalException);

            var expectedActionResult =
                new ActionResult<ApprovalOutcome>(expectedConflictObjectResult);

            this.approvalOrchestrationServiceMock.Setup(service =>
                service.DecideApprovalAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<ApprovalDecision>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(approvalOrchestrationDependencyValidationException);

            // when
            ActionResult<ApprovalOutcome> actualActionResult =
                await PostSomeDecisionAsync();

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            VerifyDecideCalledOnce();
        }

        [Fact]
        public async Task ShouldReturnLockedOnPostDecisionIfRecordIsLockedAsync()
        {
            // given
            var someInnerException = new Exception();
            string someMessage = GetRandomString();

            var lockedApprovalException =
                new LockedApprovalException(
                    message: someMessage,
                    innerException: someInnerException,
                    data: someInnerException.Data);

            var approvalOrchestrationDependencyValidationException =
                new ApprovalOrchestrationDependencyValidationException(
                    message: someMessage,
                    innerException: lockedApprovalException);

            LockedObjectResult expectedLockedObjectResult =
                Locked(lockedApprovalException);

            var expectedActionResult =
                new ActionResult<ApprovalOutcome>(expectedLockedObjectResult);

            this.approvalOrchestrationServiceMock.Setup(service =>
                service.DecideApprovalAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<ApprovalDecision>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(approvalOrchestrationDependencyValidationException);

            // when
            ActionResult<ApprovalOutcome> actualActionResult =
                await PostSomeDecisionAsync();

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            VerifyDecideCalledOnce();
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldReturnFailedDependencyOnPostDecisionIfDependencyErrorOccurredAsync(
            Xeption dependencyException)
        {
            // given
            FailedDependencyObjectResult expectedFailedDependencyObjectResult =
                FailedDependency(dependencyException.InnerException);

            var expectedActionResult =
                new ActionResult<ApprovalOutcome>(expectedFailedDependencyObjectResult);

            this.approvalOrchestrationServiceMock.Setup(service =>
                service.DecideApprovalAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<ApprovalDecision>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(dependencyException);

            // when
            ActionResult<ApprovalOutcome> actualActionResult =
                await PostSomeDecisionAsync();

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            VerifyDecideCalledOnce();
        }

        [Theory]
        [MemberData(nameof(ServerExceptions))]
        public async Task ShouldReturnInternalServerErrorOnPostDecisionIfServerErrorOccurredAsync(
            Xeption serverException)
        {
            // given
            InternalServerErrorObjectResult expectedInternalServerErrorObjectResult =
                InternalServerError(serverException);

            var expectedActionResult =
                new ActionResult<ApprovalOutcome>(expectedInternalServerErrorObjectResult);

            this.approvalOrchestrationServiceMock.Setup(service =>
                service.DecideApprovalAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<ApprovalDecision>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(serverException);

            // when
            ActionResult<ApprovalOutcome> actualActionResult =
                await PostSomeDecisionAsync();

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            VerifyDecideCalledOnce();
        }

        [Fact]
        public void ControllerShouldHaveApiControllerAttribute()
        {
            // Given
            var controllerType = typeof(ApprovalsController);
            Type attributeType = typeof(ApiControllerAttribute);

            // When
            var attribute = controllerType
                .GetCustomAttributes(attributeType, inherit: true)
                .FirstOrDefault();

            // Then
            attribute.Should().NotBeNull();
        }

        [Fact]
        public void ControllerShouldHaveRouteAttributeWithApiTemplate()
        {
            // Given
            var controllerType = typeof(ApprovalsController);
            Type attributeType = typeof(RouteAttribute);
            string expectedTemplate = "api/[controller]";

            // When
            var attribute = controllerType
                .GetCustomAttributes(attributeType, inherit: true)
                .FirstOrDefault() as RouteAttribute;

            // Then
            attribute.Should().NotBeNull();
            attribute.Template.Should().Be(expectedTemplate);
        }

        /// <summary>
        /// The verdict names resolved policy and the decision writes the source of truth, so this
        /// surface is §14.7 posture D throughout — unlike the tag exposer, no action here may opt
        /// out of authentication.
        /// </summary>
        [Fact]
        public void ControllerShouldNotAllowAnonymous()
        {
            // Given
            var controllerType = typeof(ApprovalsController);
            Type attributeType = typeof(AllowAnonymousAttribute);

            // When
            var attribute = controllerType
                .GetCustomAttributes(attributeType, inherit: true)
                .FirstOrDefault();

            // Then
            attribute.Should().BeNull();
        }

        [Fact]
        public void EveryActionShouldRequireAuthentication()
        {
            // Given
            List<MethodInfo> actions = GetActions();

            // When
            List<string> unauthorizedActions = actions
                .Where(action =>
                    HasAttribute(action, typeof(AuthorizeAttribute)) is false
                        || HasAttribute(action, typeof(AllowAnonymousAttribute)))
                .Select(action => action.Name)
                .ToList();

            // Then
            unauthorizedActions.Should().BeEmpty();
        }

        [Fact]
        public void EveryActionShouldCarryExactlyOneAuthorizationDecision()
        {
            // Given
            List<MethodInfo> actions = GetActions();

            // When
            List<string> undecidedActions = actions
                .Where(action =>
                    HasAttribute(action, typeof(AuthorizeAttribute))
                        == HasAttribute(action, typeof(AllowAnonymousAttribute)))
                .Select(action => action.Name)
                .ToList();

            // Then
            undecidedActions.Should().BeEmpty();
        }

        [Fact]
        public void EveryActionShouldBeAccountedForBySecurityTests()
        {
            // Given
            List<string> expectedActions = new List<string>
            {
                nameof(ApprovalsController.GetApprovalVerdictAsync),
                nameof(ApprovalsController.PostApprovalDecisionAsync),
                nameof(ApprovalsController.PostApprovalResetAsync),
                nameof(ApprovalsController.GetReviewerCandidatesAsync),
                nameof(ApprovalsController.GetReviewerDisplayNamesAsync),
                nameof(ApprovalsController.PostReviewRequestAsync),
                nameof(ApprovalsController.GetReviewRequestsAsync),
                nameof(ApprovalsController.DeleteReviewRequestAsync)
            };

            // When
            List<string> actualActions = GetActions()
                .Select(action => action.Name)
                .ToList();

            // Then
            actualActions.Should().BeEquivalentTo(expectedActions);
        }

        /// <summary>
        /// <b>The empty expected list is the assertion, not a placeholder.</b> §16.7.2 restricts
        /// both operations to the publisher tier and <c>Administrators</c>, and that tier is suffix-matched
        /// — global <c>Publishers</c>, global <c>Administrators</c>, any <c>%EntityType%-Publishers</c>, and
        /// the content-type-scoped <c>%EntityType%-%ContentType%-Publishers</c> of §18.6 rule 5.
        /// These routes are generic over <c>EntityType</c> as well, so no fixed
        /// <c>Roles = ...</c> list can express the set, and any partial list would lock out the
        /// content-type tier today and every entity type added later. The tier decision therefore
        /// lives in the orchestration alone (§14.6), and this pins the attribute to the coarse
        /// authenticated-only gate so a future fixed list has to be argued for rather than
        /// slipped in.
        /// </summary>
        [Theory]
        [InlineData(nameof(ApprovalsController.GetApprovalVerdictAsync))]
        [InlineData(nameof(ApprovalsController.PostApprovalDecisionAsync))]
        [InlineData(nameof(ApprovalsController.PostApprovalResetAsync))]
        [InlineData(nameof(ApprovalsController.GetReviewerCandidatesAsync))]
        [InlineData(nameof(ApprovalsController.GetReviewerDisplayNamesAsync))]
        [InlineData(nameof(ApprovalsController.PostReviewRequestAsync))]
        [InlineData(nameof(ApprovalsController.GetReviewRequestsAsync))]
        [InlineData(nameof(ApprovalsController.DeleteReviewRequestAsync))]
        public void ActionShouldCarryAuthorizeWithNoFixedRoleList(string actionName)
        {
            // Given
            var controllerType = typeof(ApprovalsController);
            MethodInfo methodInfo = controllerType.GetMethod(actionName);
            Type attributeType = typeof(AuthorizeAttribute);
            string attributeProperty = "Roles";

            List<string> expectedAttributeValues = new List<string>
            {
            };

            // When
            var methodAttribute = methodInfo?
                .GetCustomAttributes(attributeType, inherit: true)
                .FirstOrDefault();

            var controllerAttribute = controllerType
                .GetCustomAttributes(attributeType, inherit: true)
                .FirstOrDefault();

            var attribute = methodAttribute ?? controllerAttribute;

            // Then
            attribute.Should().NotBeNull();

            var actualAttributeValue = attributeType
                .GetProperty(attributeProperty)?
                .GetValue(attribute) as string ?? string.Empty;

            var actualAttributeValues = actualAttributeValue?
                .Split(',')
                .Select(role => role.Trim())
                .Where(role => !string.IsNullOrEmpty(role))
                .ToList();

            actualAttributeValues.Should().BeEquivalentTo(expectedAttributeValues);
        }

        [Theory]
        [InlineData(nameof(ApprovalsController.GetApprovalVerdictAsync))]
        [InlineData(nameof(ApprovalsController.PostApprovalDecisionAsync))]
        [InlineData(nameof(ApprovalsController.PostApprovalResetAsync))]
        [InlineData(nameof(ApprovalsController.GetReviewerCandidatesAsync))]
        [InlineData(nameof(ApprovalsController.GetReviewerDisplayNamesAsync))]
        [InlineData(nameof(ApprovalsController.PostReviewRequestAsync))]
        [InlineData(nameof(ApprovalsController.GetReviewRequestsAsync))]
        [InlineData(nameof(ApprovalsController.DeleteReviewRequestAsync))]
        public void ActionShouldNotAllowAnonymous(string actionName)
        {
            // Given
            var controllerType = typeof(ApprovalsController);
            MethodInfo methodInfo = controllerType.GetMethod(actionName);
            Type attributeType = typeof(AllowAnonymousAttribute);

            // When
            var attribute = methodInfo?
                .GetCustomAttributes(attributeType, inherit: true)
                .FirstOrDefault();

            // Then
            attribute.Should().BeNull();
        }

        /// <summary>
        /// <c>Approve</c> is the zero member of <c>ApprovalDecision</c>, so an absent
        /// <c>decision</c> would bind to it with a valid model state and a caller who said nothing
        /// would approve. <c>[BindRequired]</c> is what makes the omission a 400 instead, and
        /// nothing else in the stack catches it — the orchestration's shape validation only
        /// refuses values outside the enum.
        /// </summary>
        [Fact]
        public void PostDecisionShouldRequireTheDecisionToBeBound()
        {
            // Given
            MethodInfo methodInfo = typeof(ApprovalsController)
                .GetMethod(nameof(ApprovalsController.PostApprovalDecisionAsync));

            ParameterInfo decisionParameter = methodInfo
                .GetParameters()
                .Single(parameter => parameter.Name == "decision");

            // When
            bool isBindRequired = decisionParameter
                .GetCustomAttributes(typeof(BindRequiredAttribute), inherit: true)
                .Any();

            // Then
            isBindRequired.Should().BeTrue();
        }

        private ValueTask<ActionResult<ApprovalOutcome>> PostSomeDecisionAsync() =>
            this.approvalsController.PostApprovalDecisionAsync(
                GetRandomEntityType(),
                Guid.NewGuid(),
                GetRandomApprovalDecision(),
                GetRandomBoolean(),
                GetRandomString(),
                default);

        private void VerifyDecideCalledOnce()
        {
            this.approvalOrchestrationServiceMock.Verify(service =>
                service.DecideApprovalAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<ApprovalDecision>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        private static List<MethodInfo> GetActions() =>
            typeof(ApprovalsController)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(method => method.IsSpecialName is false)
                .ToList();

        private static bool HasAttribute(MethodInfo method, Type attributeType) =>
            method.GetCustomAttributes(attributeType, inherit: true).Any();

        private static string GetRandomString() =>
            new MnemonicString(wordCount: GetRandomNumber()).GetValue();

        private static int GetRandomNumber() =>
            new IntRange(min: 2, max: 10).GetValue();

        private static bool GetRandomBoolean() =>
            Randomizer<bool>.Create();

        private static EntityType GetRandomEntityType() =>
            GetRandomEnumValue<EntityType>();

        private static ApprovalDecision GetRandomApprovalDecision() =>
            GetRandomEnumValue<ApprovalDecision>();

        private static ApprovalStatus GetRandomApprovalStatus() =>
            GetRandomEnumValue<ApprovalStatus>();

        private static AccessDenialReason GetRandomAccessDenialReason() =>
            GetRandomEnumValue<AccessDenialReason>();

        private static T GetRandomEnumValue<T>() where T : struct, Enum
        {
            T[] values = Enum.GetValues<T>();

            return values[new IntRange(min: 0, max: values.Length - 1).GetValue()];
        }

        // Built by hand rather than by the object filler: every member is `required` and `init`,
        // and IsBlocked is derived from BlockReasons, so a filler would either refuse the type or
        // produce a verdict whose reasons and flags disagree.
        private static ApprovalVerdict CreateRandomApprovalVerdict()
        {
            return new ApprovalVerdict
            {
                ApprovalId = Guid.NewGuid(),
                EntityType = GetRandomEntityType(),
                EntityId = Guid.NewGuid(),
                ApprovalStatus = GetRandomApprovalStatus(),

                BlockReasons = new List<ApprovalBlockReason>
                {
                    new ApprovalBlockReason
                    {
                        Code = GetRandomAccessDenialReason(),
                        Message = GetRandomString()
                    }
                },

                IsBypassAllowedForCurrentUser = GetRandomBoolean(),
                CanApprove = GetRandomBoolean(),
                ApprovalCount = GetRandomNumber(),
                RequiredNumberOfApprovals = GetRandomNumber(),
                UnresolvedApprovalCommentCount = GetRandomNumber()
            };
        }

        private static ApprovalOutcome CreateRandomApprovalOutcome()
        {
            return new ApprovalOutcome
            {
                ApprovalId = Guid.NewGuid(),
                EntityType = GetRandomEntityType(),
                EntityId = Guid.NewGuid(),
                ApprovalStatus = GetRandomApprovalStatus(),
                IsApprovedByBypass = GetRandomBoolean(),
                ApprovedByBypassReason = GetRandomString(),
                IsEntitySyncRequested = true
            };
        }

        /// <summary>
        /// The invite answers 204 on every success, and the same 204 for all of them. Its
        /// outcomes are "already invited", "created" and "already answered, nothing to create"
        /// (7.9 rule 4), and a caller has no use for the difference - it refreshes from the round
        /// either way, which is the only source that stays right while somebody else works the
        /// same item.
        /// </summary>
        [Fact]
        public async Task ShouldReturnNoContentOnPostReviewRequestAsync()
        {
            // given
            EntityType randomEntityType = GetRandomEntityType();
            Guid randomEntityId = Guid.NewGuid();
            string randomRequestedUserId = Guid.NewGuid().ToString();

            this.approvalOrchestrationServiceMock.Setup(service =>
                service.RequestApprovalReviewAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new ApprovalReviewRequest { Id = Guid.NewGuid() });

            // when
            ActionResult<ApprovalReviewRequest> actualActionResult =
                await this.approvalsController.PostReviewRequestAsync(
                    randomEntityType,
                    randomEntityId,
                    randomRequestedUserId,
                    default);

            // then
            actualActionResult.Result.Should().BeOfType<NoContentResult>();

            this.approvalOrchestrationServiceMock.Verify(service =>
                service.RequestApprovalReviewAsync(
                    randomEntityType,
                    randomEntityId,
                    randomRequestedUserId,
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// The answered case has nothing to hand back at all - rule 6 retired the invitation when
        /// the person answered - so the orchestration returns null. That must still be a 204 and
        /// never an error: the likely caller is a panel a few seconds stale, not a mistake.
        /// </summary>
        [Fact]
        public async Task ShouldReturnNoContentOnPostReviewRequestWhenThereIsNothingToCreateAsync()
        {
            // given
            this.approvalOrchestrationServiceMock.Setup(service =>
                service.RequestApprovalReviewAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((ApprovalReviewRequest)null);

            // when
            ActionResult<ApprovalReviewRequest> actualActionResult =
                await this.approvalsController.PostReviewRequestAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    Guid.NewGuid().ToString(),
                    default);

            // then
            actualActionResult.Result.Should().BeOfType<NoContentResult>();
        }

        /// <summary>
        /// The read §7.9 was written around. Until this route the request rows could be created
        /// and withdrawn but never seen, so the panel's Requested section was permanently empty —
        /// not because nobody had been asked, but because it could not be known.
        /// </summary>
        [Fact]
        public async Task ShouldReturnReviewRequestsOnGetReviewRequestsAsync()
        {
            // given
            EntityType randomEntityType = GetRandomEntityType();
            Guid randomEntityId = Guid.NewGuid();

            IReadOnlyList<ApprovalReviewRequest> randomApprovalReviewRequests =
                new List<ApprovalReviewRequest>
                {
                    new ApprovalReviewRequest { Id = Guid.NewGuid() },
                    new ApprovalReviewRequest { Id = Guid.NewGuid() },
                };

            this.approvalOrchestrationServiceMock.Setup(service =>
                service.RetrieveApprovalReviewRequestsAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(randomApprovalReviewRequests);

            // when
            ActionResult<IReadOnlyList<ApprovalReviewRequest>> actualActionResult =
                await this.approvalsController.GetReviewRequestsAsync(
                    randomEntityType,
                    randomEntityId,
                    default);

            // then
            actualActionResult.Result.Should().BeOfType<OkObjectResult>();

            ((OkObjectResult)actualActionResult.Result).Value
                .Should().BeSameAs(randomApprovalReviewRequests);

            this.approvalOrchestrationServiceMock.Verify(service =>
                service.RetrieveApprovalReviewRequestsAsync(
                    randomEntityType,
                    randomEntityId,
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// The panel's one name resolver. An <c>ApprovalReview</c> row names its reviewer by
        /// account id, and until this route the only thing that named other people was
        /// <c>/api/admin/users</c> behind <c>Administrators</c> — so a <c>Publisher</c> who is not
        /// an administrator could render their own name and nobody else's.
        ///
        /// <para>Keyed on the round, so the entity key is what travels and the caller names no
        /// ids of its own — which is what leaves nothing to probe with and no batch to cap.</para>
        /// </summary>
        [Fact]
        public async Task ShouldReturnDisplayNamesOnGetReviewerDisplayNamesAsync()
        {
            // given
            EntityType randomEntityType = GetRandomEntityType();
            Guid randomEntityId = Guid.NewGuid();

            IReadOnlyList<ReviewerDisplayName> randomReviewerDisplayNames =
                new List<ReviewerDisplayName>
                {
                    new ReviewerDisplayName
                    {
                        UserId = Guid.NewGuid().ToString(),
                        DisplayName = GetRandomString(),
                    },
                    new ReviewerDisplayName
                    {
                        UserId = Guid.NewGuid().ToString(),
                        DisplayName = GetRandomString(),
                    },
                };

            this.approvalOrchestrationServiceMock.Setup(service =>
                service.RetrieveReviewerDisplayNamesAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(randomReviewerDisplayNames);

            // when
            ActionResult<IReadOnlyList<ReviewerDisplayName>> actualActionResult =
                await this.approvalsController.GetReviewerDisplayNamesAsync(
                    randomEntityType,
                    randomEntityId,
                    default);

            // then
            actualActionResult.Result.Should().BeOfType<OkObjectResult>();

            ((OkObjectResult)actualActionResult.Result).Value
                .Should().BeSameAs(randomReviewerDisplayNames);

            this.approvalOrchestrationServiceMock.Verify(service =>
                service.RetrieveReviewerDisplayNamesAsync(
                    randomEntityType,
                    randomEntityId,
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Covers the entity key's shape rule, which is all the resolver validates now that the
        /// round has replaced the caller-supplied batch.
        ///
        /// <para>NOT the tier gate and NOT the missing round, despite all three arriving as an
        /// <c>ApprovalOrchestrationValidationException</c>: those wrap an
        /// <c>UnauthorizedApprovalOrchestrationException</c> and a
        /// <c>NotFoundApprovalOrchestrationException</c>, and the action catches both shapes
        /// FIRST, so neither can reach this <c>400</c> arm. Each is pinned by its own test.</para>
        /// </summary>
        [Theory]
        [MemberData(nameof(ValidationExceptions))]
        public async Task ShouldReturnBadRequestOnGetReviewerDisplayNamesIfValidationErrorAsync(
            Xeption validationException)
        {
            // given
            BadRequestObjectResult expectedBadRequestObjectResult =
                BadRequest(validationException.InnerException);

            var expectedActionResult =
                new ActionResult<IReadOnlyList<ReviewerDisplayName>>(
                    expectedBadRequestObjectResult);

            this.approvalOrchestrationServiceMock.Setup(service =>
                service.RetrieveReviewerDisplayNamesAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(validationException);

            // when
            ActionResult<IReadOnlyList<ReviewerDisplayName>> actualActionResult =
                await this.approvalsController.GetReviewerDisplayNamesAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalOrchestrationServiceMock.Verify(service =>
                service.RetrieveReviewerDisplayNamesAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnUnauthorizedOnGetReviewerDisplayNamesIfRefusedAsync()
        {
            // given
            var unauthorizedException = new UnauthorizedApprovalOrchestrationException(
                message: GetRandomString());

            var validationException = new ApprovalOrchestrationValidationException(
                message: GetRandomString(),
                innerException: unauthorizedException);

            UnauthorizedObjectResult expectedUnauthorizedObjectResult =
                Unauthorized(unauthorizedException);

            var expectedActionResult =
                new ActionResult<IReadOnlyList<ReviewerDisplayName>>(
                    expectedUnauthorizedObjectResult);

            this.approvalOrchestrationServiceMock.Setup(service =>
                service.RetrieveReviewerDisplayNamesAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(validationException);

            // when
            ActionResult<IReadOnlyList<ReviewerDisplayName>> actualActionResult =
                await this.approvalsController.GetReviewerDisplayNamesAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalOrchestrationServiceMock.Verify(service =>
                service.RetrieveReviewerDisplayNamesAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// A missing round is a <c>404</c>, the same as on every other operation keyed on the
        /// entity — and a new arm, since the unscoped resolver read no approval and so had no
        /// not-found case at all. It reaches the caller as the same outer type as the <c>400</c>
        /// and the <c>401</c> above, told apart only by the <c>when</c> filter.
        /// </summary>
        [Fact]
        public async Task ShouldReturnNotFoundOnGetReviewerDisplayNamesIfApprovalDoesNotExistAsync()
        {
            // given
            string someMessage = GetRandomString();

            var notFoundApprovalOrchestrationException =
                new NotFoundApprovalOrchestrationException(
                    message: someMessage);

            var approvalOrchestrationValidationException =
                new ApprovalOrchestrationValidationException(
                    message: someMessage,
                    innerException: notFoundApprovalOrchestrationException);

            NotFoundObjectResult expectedNotFoundObjectResult =
                NotFound(notFoundApprovalOrchestrationException);

            var expectedActionResult =
                new ActionResult<IReadOnlyList<ReviewerDisplayName>>(
                    expectedNotFoundObjectResult);

            this.approvalOrchestrationServiceMock.Setup(service =>
                service.RetrieveReviewerDisplayNamesAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(approvalOrchestrationValidationException);

            // when
            ActionResult<IReadOnlyList<ReviewerDisplayName>> actualActionResult =
                await this.approvalsController.GetReviewerDisplayNamesAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalOrchestrationServiceMock.Verify(service =>
                service.RetrieveReviewerDisplayNamesAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task
            ShouldReturnFailedDependencyOnGetReviewerDisplayNamesIfDependencyErrorAsync(
                Xeption dependencyException)
        {
            // given
            FailedDependencyObjectResult expectedFailedDependencyObjectResult =
                FailedDependency(dependencyException.InnerException);

            var expectedActionResult =
                new ActionResult<IReadOnlyList<ReviewerDisplayName>>(
                    expectedFailedDependencyObjectResult);

            this.approvalOrchestrationServiceMock.Setup(service =>
                service.RetrieveReviewerDisplayNamesAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(dependencyException);

            // when
            ActionResult<IReadOnlyList<ReviewerDisplayName>> actualActionResult =
                await this.approvalsController.GetReviewerDisplayNamesAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalOrchestrationServiceMock.Verify(service =>
                service.RetrieveReviewerDisplayNamesAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ServerExceptions))]
        public async Task
            ShouldReturnInternalServerErrorOnGetReviewerDisplayNamesIfServerErrorAsync(
                Xeption serverException)
        {
            // given
            InternalServerErrorObjectResult expectedInternalServerErrorObjectResult =
                InternalServerError(serverException);

            var expectedActionResult =
                new ActionResult<IReadOnlyList<ReviewerDisplayName>>(
                    expectedInternalServerErrorObjectResult);

            this.approvalOrchestrationServiceMock.Setup(service =>
                service.RetrieveReviewerDisplayNamesAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(serverException);

            // when
            ActionResult<IReadOnlyList<ReviewerDisplayName>> actualActionResult =
                await this.approvalsController.GetReviewerDisplayNamesAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalOrchestrationServiceMock.Verify(service =>
                service.RetrieveReviewerDisplayNamesAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Keyed on the round and the person, matching the POST beside it. The old
        /// <c>DELETE /api/ApprovalReviewRequests/{id}</c> is gone: the row id it needed appeared
        /// only in the create's response body, which #352 correctly made a 204.
        /// </summary>
        [Fact]
        public async Task ShouldReturnNoContentOnDeleteReviewRequestAsync()
        {
            // given
            EntityType randomEntityType = GetRandomEntityType();
            Guid randomEntityId = Guid.NewGuid();
            string randomRequestedUserId = Guid.NewGuid().ToString();
            string randomDeletionReason = Guid.NewGuid().ToString();

            this.approvalOrchestrationServiceMock.Setup(service =>
                service.WithdrawApprovalReviewRequestAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new ApprovalReviewRequest { Id = Guid.NewGuid() });

            // when
            ActionResult<ApprovalReviewRequest> actualActionResult =
                await this.approvalsController.DeleteReviewRequestAsync(
                    randomEntityType,
                    randomEntityId,
                    randomRequestedUserId,
                    randomDeletionReason,
                    default);

            // then
            actualActionResult.Result.Should().BeOfType<NoContentResult>();

            this.approvalOrchestrationServiceMock.Verify(service =>
                service.WithdrawApprovalReviewRequestAsync(
                    randomEntityType,
                    randomEntityId,
                    randomRequestedUserId,
                    randomDeletionReason,
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Nothing outstanding for that person is a no-op, not a not-found. Withdrawing an
        /// invitation already withdrawn, or one a rule 6 retirement has taken, is a stale panel
        /// rather than a mistake — and the orchestration returns null for it.
        /// </summary>
        [Fact]
        public async Task ShouldReturnNoContentOnDeleteReviewRequestWhenNothingIsOutstandingAsync()
        {
            // given
            this.approvalOrchestrationServiceMock.Setup(service =>
                service.WithdrawApprovalReviewRequestAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((ApprovalReviewRequest)null);

            // when
            ActionResult<ApprovalReviewRequest> actualActionResult =
                await this.approvalsController.DeleteReviewRequestAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    Guid.NewGuid().ToString(),
                    deletionReason: null,
                    default);

            // then
            actualActionResult.Result.Should().BeOfType<NoContentResult>();
        }

        // The status-code contract for the two review-request actions, ported from the suite that
        // died with ApprovalReviewRequestsController when the withdraw was re-keyed onto the round.
        //
        // Worth its own block rather than folding into the happy-path tests above, because THREE
        // of these five refusals arrive as the SAME outer type — the orchestration funnels
        // Unauthorized, NotFound and Invalid alike through CreateAndLogValidationExceptionAsync —
        // and only the `when (... .InnerException is ...)` filters tell them apart. Nothing about
        // that discrimination is visible to the compiler: replacing a `NotFound(...)` body with a
        // `BadRequest(...)`, or narrowing a filter to a type the orchestration never throws, ships
        // silently and answers 400 where §17.5 promises 404 or 401.

        [Fact]
        public async Task ShouldReturnNotFoundOnGetReviewRequestsIfApprovalDoesNotExistAsync()
        {
            // given
            string someMessage = GetRandomString();

            var notFoundApprovalOrchestrationException =
                new NotFoundApprovalOrchestrationException(
                    message: someMessage);

            var approvalOrchestrationValidationException =
                new ApprovalOrchestrationValidationException(
                    message: someMessage,
                    innerException: notFoundApprovalOrchestrationException);

            NotFoundObjectResult expectedNotFoundObjectResult =
                NotFound(notFoundApprovalOrchestrationException);

            var expectedActionResult =
                new ActionResult<IReadOnlyList<ApprovalReviewRequest>>(
                    expectedNotFoundObjectResult);

            this.approvalOrchestrationServiceMock.Setup(service =>
                service.RetrieveApprovalReviewRequestsAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(approvalOrchestrationValidationException);

            // when
            ActionResult<IReadOnlyList<ApprovalReviewRequest>> actualActionResult =
                await this.approvalsController.GetReviewRequestsAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalOrchestrationServiceMock.Verify(service =>
                service.RetrieveApprovalReviewRequestsAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// The service-side half of the tier gate. The controller's bare <c>[Authorize]</c>
        /// establishes only that a caller is authenticated, so this 401 is what an authenticated
        /// caller below the requesting tier actually receives — and these rows name people, so
        /// answering 400 would be the wrong signal on a user-enumeration surface (§16.7.4).
        /// </summary>
        [Fact]
        public async Task ShouldReturnUnauthorizedOnGetReviewRequestsIfOutsideTheTierAsync()
        {
            // given
            string someMessage = GetRandomString();

            var unauthorizedApprovalOrchestrationException =
                new UnauthorizedApprovalOrchestrationException(
                    message: someMessage);

            var approvalOrchestrationValidationException =
                new ApprovalOrchestrationValidationException(
                    message: someMessage,
                    innerException: unauthorizedApprovalOrchestrationException);

            UnauthorizedObjectResult expectedUnauthorizedObjectResult =
                Unauthorized(unauthorizedApprovalOrchestrationException);

            var expectedActionResult =
                new ActionResult<IReadOnlyList<ApprovalReviewRequest>>(
                    expectedUnauthorizedObjectResult);

            this.approvalOrchestrationServiceMock.Setup(service =>
                service.RetrieveApprovalReviewRequestsAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(approvalOrchestrationValidationException);

            // when
            ActionResult<IReadOnlyList<ApprovalReviewRequest>> actualActionResult =
                await this.approvalsController.GetReviewRequestsAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalOrchestrationServiceMock.Verify(service =>
                service.RetrieveApprovalReviewRequestsAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ValidationExceptions))]
        public async Task ShouldReturnBadRequestOnGetReviewRequestsIfValidationErrorOccurredAsync(
            Xeption validationException)
        {
            // given
            BadRequestObjectResult expectedBadRequestObjectResult =
                BadRequest(validationException.InnerException);

            var expectedActionResult =
                new ActionResult<IReadOnlyList<ApprovalReviewRequest>>(
                    expectedBadRequestObjectResult);

            this.approvalOrchestrationServiceMock.Setup(service =>
                service.RetrieveApprovalReviewRequestsAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(validationException);

            // when
            ActionResult<IReadOnlyList<ApprovalReviewRequest>> actualActionResult =
                await this.approvalsController.GetReviewRequestsAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalOrchestrationServiceMock.Verify(service =>
                service.RetrieveApprovalReviewRequestsAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldReturnFailedDependencyOnGetReviewRequestsIfDependencyErrorAsync(
            Xeption dependencyException)
        {
            // given
            FailedDependencyObjectResult expectedFailedDependencyObjectResult =
                FailedDependency(dependencyException.InnerException);

            var expectedActionResult =
                new ActionResult<IReadOnlyList<ApprovalReviewRequest>>(
                    expectedFailedDependencyObjectResult);

            this.approvalOrchestrationServiceMock.Setup(service =>
                service.RetrieveApprovalReviewRequestsAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(dependencyException);

            // when
            ActionResult<IReadOnlyList<ApprovalReviewRequest>> actualActionResult =
                await this.approvalsController.GetReviewRequestsAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalOrchestrationServiceMock.Verify(service =>
                service.RetrieveApprovalReviewRequestsAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ServerExceptions))]
        public async Task ShouldReturnInternalServerErrorOnGetReviewRequestsIfServerErrorAsync(
            Xeption serverException)
        {
            // given
            InternalServerErrorObjectResult expectedInternalServerErrorObjectResult =
                InternalServerError(serverException);

            var expectedActionResult =
                new ActionResult<IReadOnlyList<ApprovalReviewRequest>>(
                    expectedInternalServerErrorObjectResult);

            this.approvalOrchestrationServiceMock.Setup(service =>
                service.RetrieveApprovalReviewRequestsAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(serverException);

            // when
            ActionResult<IReadOnlyList<ApprovalReviewRequest>> actualActionResult =
                await this.approvalsController.GetReviewRequestsAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalOrchestrationServiceMock.Verify(service =>
                service.RetrieveApprovalReviewRequestsAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnNotFoundOnDeleteReviewRequestIfApprovalDoesNotExistAsync()
        {
            // given
            string someMessage = GetRandomString();

            var notFoundApprovalOrchestrationException =
                new NotFoundApprovalOrchestrationException(
                    message: someMessage);

            var approvalOrchestrationValidationException =
                new ApprovalOrchestrationValidationException(
                    message: someMessage,
                    innerException: notFoundApprovalOrchestrationException);

            NotFoundObjectResult expectedNotFoundObjectResult =
                NotFound(notFoundApprovalOrchestrationException);

            var expectedActionResult =
                new ActionResult<ApprovalReviewRequest>(expectedNotFoundObjectResult);

            this.approvalOrchestrationServiceMock.Setup(service =>
                service.WithdrawApprovalReviewRequestAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(approvalOrchestrationValidationException);

            // when
            ActionResult<ApprovalReviewRequest> actualActionResult =
                await this.approvalsController.DeleteReviewRequestAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    Guid.NewGuid().ToString(),
                    deletionReason: null,
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalOrchestrationServiceMock.Verify(service =>
                service.WithdrawApprovalReviewRequestAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnUnauthorizedOnDeleteReviewRequestIfOutsideTheTierAsync()
        {
            // given
            string someMessage = GetRandomString();

            var unauthorizedApprovalOrchestrationException =
                new UnauthorizedApprovalOrchestrationException(
                    message: someMessage);

            var approvalOrchestrationValidationException =
                new ApprovalOrchestrationValidationException(
                    message: someMessage,
                    innerException: unauthorizedApprovalOrchestrationException);

            UnauthorizedObjectResult expectedUnauthorizedObjectResult =
                Unauthorized(unauthorizedApprovalOrchestrationException);

            var expectedActionResult =
                new ActionResult<ApprovalReviewRequest>(expectedUnauthorizedObjectResult);

            this.approvalOrchestrationServiceMock.Setup(service =>
                service.WithdrawApprovalReviewRequestAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(approvalOrchestrationValidationException);

            // when
            ActionResult<ApprovalReviewRequest> actualActionResult =
                await this.approvalsController.DeleteReviewRequestAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    Guid.NewGuid().ToString(),
                    deletionReason: null,
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalOrchestrationServiceMock.Verify(service =>
                service.WithdrawApprovalReviewRequestAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Covers the refusal a moderator can reach by ordinary use: §7.9 rule 5 refuses to
        /// withdraw an invitation its target has already ANSWERED, which the orchestration raises
        /// as an <c>InvalidApprovalOrchestrationException</c> and which must surface as a 400
        /// rather than being mistaken for a missing round.
        /// </summary>
        [Theory]
        [MemberData(nameof(ValidationExceptions))]
        public async Task ShouldReturnBadRequestOnDeleteReviewRequestIfValidationErrorAsync(
            Xeption validationException)
        {
            // given
            BadRequestObjectResult expectedBadRequestObjectResult =
                BadRequest(validationException.InnerException);

            var expectedActionResult =
                new ActionResult<ApprovalReviewRequest>(expectedBadRequestObjectResult);

            this.approvalOrchestrationServiceMock.Setup(service =>
                service.WithdrawApprovalReviewRequestAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(validationException);

            // when
            ActionResult<ApprovalReviewRequest> actualActionResult =
                await this.approvalsController.DeleteReviewRequestAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    Guid.NewGuid().ToString(),
                    deletionReason: null,
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalOrchestrationServiceMock.Verify(service =>
                service.WithdrawApprovalReviewRequestAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldReturnFailedDependencyOnDeleteReviewRequestIfDependencyErrorAsync(
            Xeption dependencyException)
        {
            // given
            FailedDependencyObjectResult expectedFailedDependencyObjectResult =
                FailedDependency(dependencyException.InnerException);

            var expectedActionResult =
                new ActionResult<ApprovalReviewRequest>(expectedFailedDependencyObjectResult);

            this.approvalOrchestrationServiceMock.Setup(service =>
                service.WithdrawApprovalReviewRequestAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(dependencyException);

            // when
            ActionResult<ApprovalReviewRequest> actualActionResult =
                await this.approvalsController.DeleteReviewRequestAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    Guid.NewGuid().ToString(),
                    deletionReason: null,
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalOrchestrationServiceMock.Verify(service =>
                service.WithdrawApprovalReviewRequestAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ServerExceptions))]
        public async Task ShouldReturnInternalServerErrorOnDeleteReviewRequestIfServerErrorAsync(
            Xeption serverException)
        {
            // given
            InternalServerErrorObjectResult expectedInternalServerErrorObjectResult =
                InternalServerError(serverException);

            var expectedActionResult =
                new ActionResult<ApprovalReviewRequest>(
                    expectedInternalServerErrorObjectResult);

            this.approvalOrchestrationServiceMock.Setup(service =>
                service.WithdrawApprovalReviewRequestAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(serverException);

            // when
            ActionResult<ApprovalReviewRequest> actualActionResult =
                await this.approvalsController.DeleteReviewRequestAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    Guid.NewGuid().ToString(),
                    deletionReason: null,
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalOrchestrationServiceMock.Verify(service =>
                service.WithdrawApprovalReviewRequestAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalOrchestrationServiceMock.VerifyNoOtherCalls();
        }
    }
}
