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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests.Exceptions;
using Glory2Him.Core.Models.Orchestrations.ApprovalReviewers.Exceptions;
using Glory2Him.Core.Models.Orchestrations.Approvals;
using Glory2Him.Core.Services.Orchestrations.ApprovalReviewers;
using Glory2Him.WebApp.Controllers.ApprovalReviewers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RESTFulSense.Clients.Extensions;
using RESTFulSense.Controllers;
using RESTFulSense.Models;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.ApprovalReviewers
{
    /// <summary>
    /// Kept in one file rather than split into the per-operation partials the sibling exposer
    /// suites use — five actions is not enough to earn the split. The three security theories at
    /// the foot of the file enumerate every action by name, so a reader checking that a new
    /// endpoint was added to all three should not have to open a second file to see the tests it
    /// was added alongside.
    ///
    /// <para>SPLIT OUT OF <c>ApprovalsControllerTests</c> (#523, §12.5.4 business rule 6): these
    /// five actions — candidates, display names and the three review-request verbs — moved here
    /// verbatim along with their catch chains and their tests, and this controller now binds
    /// <c>IApprovalReviewerOrchestrationService</c> alone. The verdict, the decision and the reset
    /// stayed on <c>ApprovalsController</c> and are asserted in its own suite.</para>
    /// </summary>
    public class ApprovalReviewersControllerTests : RESTFulController
    {
        private readonly Mock<IApprovalReviewerOrchestrationService>
            approvalReviewerOrchestrationServiceMock;

        private readonly ApprovalReviewersController approvalReviewersController;

        public ApprovalReviewersControllerTests()
        {
            approvalReviewerOrchestrationServiceMock =
                new Mock<IApprovalReviewerOrchestrationService>();

            approvalReviewersController = new ApprovalReviewersController(
                approvalReviewerOrchestrationServiceMock.Object);
        }

        // Kept apart from any other family rather than widened to hold both: an action catches
        // ONE service's exceptions, so a theory feeding it another's would prove the catch-all
        // rather than the clause it names.
        public static TheoryData<Xeption> ReviewerValidationExceptions()
        {
            var someInnerException = new Xeption();
            string someMessage = GetRandomString();

            return new TheoryData<Xeption>
            {
                new ApprovalReviewerOrchestrationValidationException(
                    message: someMessage,
                    innerException: someInnerException),

                new ApprovalReviewerOrchestrationDependencyValidationException(
                    message: someMessage,
                    innerException: someInnerException)
            };
        }

        public static TheoryData<Xeption> ReviewerDependencyExceptions()
        {
            var someInnerException = new Xeption();
            string someMessage = GetRandomString();

            return new TheoryData<Xeption>
            {
                new ApprovalReviewerOrchestrationDependencyException(
                    message: someMessage,
                    innerException: someInnerException)
            };
        }

        public static TheoryData<Xeption> ReviewerServerExceptions()
        {
            var someInnerException = new Xeption();
            string someMessage = GetRandomString();

            return new TheoryData<Xeption>
            {
                new ApprovalReviewerOrchestrationServiceException(
                    message: someMessage,
                    innerException: someInnerException)
            };
        }

        [Fact]
        public void ControllerShouldHaveApiControllerAttribute()
        {
            // Given
            var controllerType = typeof(ApprovalReviewersController);
            Type attributeType = typeof(ApiControllerAttribute);

            // When
            var attribute = controllerType
                .GetCustomAttributes(attributeType, inherit: true)
                .FirstOrDefault();

            // Then
            attribute.Should().NotBeNull();
        }

        /// <summary>
        /// THE load-bearing assertion of the split (issue #523 criterion 3): the class is named
        /// in the PLURAL, like every sibling exposer, so the "api/[controller]" convention would
        /// route it at "api/ApprovalReviewers" — and every one of these five URLs would move. The
        /// literal keeps them exactly where a caller has always reached them, on the approval
        /// round's own resource.
        /// </summary>
        [Fact]
        public void ControllerShouldHaveRouteAttributeWithLiteralApprovalsTemplate()
        {
            // Given
            var controllerType = typeof(ApprovalReviewersController);
            Type attributeType = typeof(RouteAttribute);
            string expectedTemplate = "api/Approvals";

            // When
            var attribute = controllerType
                .GetCustomAttributes(attributeType, inherit: true)
                .FirstOrDefault() as RouteAttribute;

            // Then
            attribute.Should().NotBeNull();
            attribute.Template.Should().Be(expectedTemplate);
        }

        /// <summary>
        /// The invite answers 204 on every success. Its outcomes are "already invited", "created"
        /// and "already answered, nothing to create" (7.9 rule 4), and a caller has no use for the
        /// difference - it refreshes from the round either way, which is the only source that
        /// stays right while somebody else works the same item.
        /// </summary>
        [Fact]
        public async Task ShouldReturnNoContentOnPostReviewRequestAsync()
        {
            // given
            EntityType randomEntityType = GetRandomEntityType();
            Guid randomEntityId = Guid.NewGuid();
            string randomRequestedUserId = Guid.NewGuid().ToString();

            this.approvalReviewerOrchestrationServiceMock.Setup(service =>
                service.RequestApprovalReviewAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new ApprovalReviewRequest { Id = Guid.NewGuid() });

            // when
            ActionResult<ApprovalReviewRequest> actualActionResult =
                await this.approvalReviewersController.PostReviewRequestAsync(
                    randomEntityType,
                    randomEntityId,
                    randomRequestedUserId,
                    default);

            // then
            actualActionResult.Result.Should().BeOfType<NoContentResult>();

            this.approvalReviewerOrchestrationServiceMock.Verify(service =>
                service.RequestApprovalReviewAsync(
                    randomEntityType,
                    randomEntityId,
                    randomRequestedUserId,
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalReviewerOrchestrationServiceMock.VerifyNoOtherCalls();
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
            this.approvalReviewerOrchestrationServiceMock.Setup(service =>
                service.RequestApprovalReviewAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((ApprovalReviewRequest)null);

            // when
            ActionResult<ApprovalReviewRequest> actualActionResult =
                await this.approvalReviewersController.PostReviewRequestAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    Guid.NewGuid().ToString(),
                    default);

            // then
            actualActionResult.Result.Should().BeOfType<NoContentResult>();
        }

        /// <summary>
        /// The one refusal the re-read cannot explain away (§7.9 rule 4): the winning row was
        /// withdrawn between the unique-index collision and the second look, so nothing is left
        /// to hand back as the 204. Carried across the split unchanged — issue #523 criterion 6
        /// pins this clause explicitly because the exception family it catches changed underneath
        /// it (#521) while the mapping did not.
        /// </summary>
        [Fact]
        public async Task ShouldReturnConflictOnPostReviewRequestIfAlreadyExistsErrorOccurredAsync()
        {
            // given
            var someInnerException = new Exception();
            string someMessage = GetRandomString();

            var alreadyExistsApprovalReviewRequestException =
                new AlreadyExistsApprovalReviewRequestException(
                    message: someMessage,
                    innerException: someInnerException,
                    data: someInnerException.Data);

            var approvalReviewerOrchestrationDependencyValidationException =
                new ApprovalReviewerOrchestrationDependencyValidationException(
                    message: someMessage,
                    innerException: alreadyExistsApprovalReviewRequestException);

            ConflictObjectResult expectedConflictObjectResult =
                Conflict(alreadyExistsApprovalReviewRequestException);

            var expectedActionResult =
                new ActionResult<ApprovalReviewRequest>(expectedConflictObjectResult);

            this.approvalReviewerOrchestrationServiceMock.Setup(service =>
                service.RequestApprovalReviewAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(approvalReviewerOrchestrationDependencyValidationException);

            // when
            ActionResult<ApprovalReviewRequest> actualActionResult =
                await this.approvalReviewersController.PostReviewRequestAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    Guid.NewGuid().ToString(),
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalReviewerOrchestrationServiceMock.Verify(service =>
                service.RequestApprovalReviewAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalReviewerOrchestrationServiceMock.VerifyNoOtherCalls();
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

            this.approvalReviewerOrchestrationServiceMock.Setup(service =>
                service.RetrieveApprovalReviewRequestsAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(randomApprovalReviewRequests);

            // when
            ActionResult<IReadOnlyList<ApprovalReviewRequest>> actualActionResult =
                await this.approvalReviewersController.GetReviewRequestsAsync(
                    randomEntityType,
                    randomEntityId,
                    default);

            // then
            actualActionResult.Result.Should().BeOfType<OkObjectResult>();

            ((OkObjectResult)actualActionResult.Result).Value
                .Should().BeSameAs(randomApprovalReviewRequests);

            this.approvalReviewerOrchestrationServiceMock.Verify(service =>
                service.RetrieveApprovalReviewRequestsAsync(
                    randomEntityType,
                    randomEntityId,
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalReviewerOrchestrationServiceMock.VerifyNoOtherCalls();
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
                        UserName = GetRandomString(),
                    },
                    new ReviewerDisplayName
                    {
                        UserId = Guid.NewGuid().ToString(),
                        DisplayName = GetRandomString(),
                        UserName = GetRandomString(),
                    },
                };

            this.approvalReviewerOrchestrationServiceMock.Setup(service =>
                service.RetrieveReviewerDisplayNamesAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(randomReviewerDisplayNames);

            // when
            ActionResult<IReadOnlyList<ReviewerDisplayName>> actualActionResult =
                await this.approvalReviewersController.GetReviewerDisplayNamesAsync(
                    randomEntityType,
                    randomEntityId,
                    default);

            // then
            actualActionResult.Result.Should().BeOfType<OkObjectResult>();

            ((OkObjectResult)actualActionResult.Result).Value
                .Should().BeSameAs(randomReviewerDisplayNames);

            this.approvalReviewerOrchestrationServiceMock.Verify(service =>
                service.RetrieveReviewerDisplayNamesAsync(
                    randomEntityType,
                    randomEntityId,
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalReviewerOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Covers the entity key's shape rule, which is all the resolver validates now that the
        /// round has replaced the caller-supplied batch.
        ///
        /// <para>NOT the tier gate and NOT the missing round, despite all three arriving as an
        /// <c>ApprovalReviewerOrchestrationValidationException</c>: those wrap an
        /// <c>UnauthorizedApprovalReviewerOrchestrationException</c> and a
        /// <c>NotFoundApprovalReviewerOrchestrationException</c>, and the action catches both shapes
        /// FIRST, so neither can reach this <c>400</c> arm. Each is pinned by its own test.</para>
        /// </summary>
        [Theory]
        [MemberData(nameof(ReviewerValidationExceptions))]
        public async Task ShouldReturnBadRequestOnGetReviewerDisplayNamesIfValidationErrorAsync(
            Xeption validationException)
        {
            // given
            BadRequestObjectResult expectedBadRequestObjectResult =
                BadRequest(validationException.InnerException);

            var expectedActionResult =
                new ActionResult<IReadOnlyList<ReviewerDisplayName>>(
                    expectedBadRequestObjectResult);

            this.approvalReviewerOrchestrationServiceMock.Setup(service =>
                service.RetrieveReviewerDisplayNamesAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(validationException);

            // when
            ActionResult<IReadOnlyList<ReviewerDisplayName>> actualActionResult =
                await this.approvalReviewersController.GetReviewerDisplayNamesAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalReviewerOrchestrationServiceMock.Verify(service =>
                service.RetrieveReviewerDisplayNamesAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalReviewerOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnUnauthorizedOnGetReviewerDisplayNamesIfRefusedAsync()
        {
            // given
            var unauthorizedException = new UnauthorizedApprovalReviewerOrchestrationException(
                message: GetRandomString());

            var validationException = new ApprovalReviewerOrchestrationValidationException(
                message: GetRandomString(),
                innerException: unauthorizedException);

            UnauthorizedObjectResult expectedUnauthorizedObjectResult =
                Unauthorized(unauthorizedException);

            var expectedActionResult =
                new ActionResult<IReadOnlyList<ReviewerDisplayName>>(
                    expectedUnauthorizedObjectResult);

            this.approvalReviewerOrchestrationServiceMock.Setup(service =>
                service.RetrieveReviewerDisplayNamesAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(validationException);

            // when
            ActionResult<IReadOnlyList<ReviewerDisplayName>> actualActionResult =
                await this.approvalReviewersController.GetReviewerDisplayNamesAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalReviewerOrchestrationServiceMock.Verify(service =>
                service.RetrieveReviewerDisplayNamesAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalReviewerOrchestrationServiceMock.VerifyNoOtherCalls();
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

            var notFoundApprovalReviewerOrchestrationException =
                new NotFoundApprovalReviewerOrchestrationException(
                    message: someMessage);

            var approvalOrchestrationValidationException =
                new ApprovalReviewerOrchestrationValidationException(
                    message: someMessage,
                    innerException: notFoundApprovalReviewerOrchestrationException);

            NotFoundObjectResult expectedNotFoundObjectResult =
                NotFound(notFoundApprovalReviewerOrchestrationException);

            var expectedActionResult =
                new ActionResult<IReadOnlyList<ReviewerDisplayName>>(
                    expectedNotFoundObjectResult);

            this.approvalReviewerOrchestrationServiceMock.Setup(service =>
                service.RetrieveReviewerDisplayNamesAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(approvalOrchestrationValidationException);

            // when
            ActionResult<IReadOnlyList<ReviewerDisplayName>> actualActionResult =
                await this.approvalReviewersController.GetReviewerDisplayNamesAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalReviewerOrchestrationServiceMock.Verify(service =>
                service.RetrieveReviewerDisplayNamesAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalReviewerOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ReviewerDependencyExceptions))]
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

            this.approvalReviewerOrchestrationServiceMock.Setup(service =>
                service.RetrieveReviewerDisplayNamesAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(dependencyException);

            // when
            ActionResult<IReadOnlyList<ReviewerDisplayName>> actualActionResult =
                await this.approvalReviewersController.GetReviewerDisplayNamesAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalReviewerOrchestrationServiceMock.Verify(service =>
                service.RetrieveReviewerDisplayNamesAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalReviewerOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ReviewerServerExceptions))]
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

            this.approvalReviewerOrchestrationServiceMock.Setup(service =>
                service.RetrieveReviewerDisplayNamesAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(serverException);

            // when
            ActionResult<IReadOnlyList<ReviewerDisplayName>> actualActionResult =
                await this.approvalReviewersController.GetReviewerDisplayNamesAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalReviewerOrchestrationServiceMock.Verify(service =>
                service.RetrieveReviewerDisplayNamesAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalReviewerOrchestrationServiceMock.VerifyNoOtherCalls();
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

            this.approvalReviewerOrchestrationServiceMock.Setup(service =>
                service.WithdrawApprovalReviewRequestAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new ApprovalReviewRequest { Id = Guid.NewGuid() });

            // when
            ActionResult<ApprovalReviewRequest> actualActionResult =
                await this.approvalReviewersController.DeleteReviewRequestAsync(
                    randomEntityType,
                    randomEntityId,
                    randomRequestedUserId,
                    randomDeletionReason,
                    default);

            // then
            actualActionResult.Result.Should().BeOfType<NoContentResult>();

            this.approvalReviewerOrchestrationServiceMock.Verify(service =>
                service.WithdrawApprovalReviewRequestAsync(
                    randomEntityType,
                    randomEntityId,
                    randomRequestedUserId,
                    randomDeletionReason,
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalReviewerOrchestrationServiceMock.VerifyNoOtherCalls();
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
            this.approvalReviewerOrchestrationServiceMock.Setup(service =>
                service.WithdrawApprovalReviewRequestAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((ApprovalReviewRequest)null);

            // when
            ActionResult<ApprovalReviewRequest> actualActionResult =
                await this.approvalReviewersController.DeleteReviewRequestAsync(
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

            var notFoundApprovalReviewerOrchestrationException =
                new NotFoundApprovalReviewerOrchestrationException(
                    message: someMessage);

            var approvalOrchestrationValidationException =
                new ApprovalReviewerOrchestrationValidationException(
                    message: someMessage,
                    innerException: notFoundApprovalReviewerOrchestrationException);

            NotFoundObjectResult expectedNotFoundObjectResult =
                NotFound(notFoundApprovalReviewerOrchestrationException);

            var expectedActionResult =
                new ActionResult<IReadOnlyList<ApprovalReviewRequest>>(
                    expectedNotFoundObjectResult);

            this.approvalReviewerOrchestrationServiceMock.Setup(service =>
                service.RetrieveApprovalReviewRequestsAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(approvalOrchestrationValidationException);

            // when
            ActionResult<IReadOnlyList<ApprovalReviewRequest>> actualActionResult =
                await this.approvalReviewersController.GetReviewRequestsAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalReviewerOrchestrationServiceMock.Verify(service =>
                service.RetrieveApprovalReviewRequestsAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalReviewerOrchestrationServiceMock.VerifyNoOtherCalls();
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
                new UnauthorizedApprovalReviewerOrchestrationException(
                    message: someMessage);

            var approvalOrchestrationValidationException =
                new ApprovalReviewerOrchestrationValidationException(
                    message: someMessage,
                    innerException: unauthorizedApprovalOrchestrationException);

            UnauthorizedObjectResult expectedUnauthorizedObjectResult =
                Unauthorized(unauthorizedApprovalOrchestrationException);

            var expectedActionResult =
                new ActionResult<IReadOnlyList<ApprovalReviewRequest>>(
                    expectedUnauthorizedObjectResult);

            this.approvalReviewerOrchestrationServiceMock.Setup(service =>
                service.RetrieveApprovalReviewRequestsAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(approvalOrchestrationValidationException);

            // when
            ActionResult<IReadOnlyList<ApprovalReviewRequest>> actualActionResult =
                await this.approvalReviewersController.GetReviewRequestsAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalReviewerOrchestrationServiceMock.Verify(service =>
                service.RetrieveApprovalReviewRequestsAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalReviewerOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ReviewerValidationExceptions))]
        public async Task ShouldReturnBadRequestOnGetReviewRequestsIfValidationErrorOccurredAsync(
            Xeption validationException)
        {
            // given
            BadRequestObjectResult expectedBadRequestObjectResult =
                BadRequest(validationException.InnerException);

            var expectedActionResult =
                new ActionResult<IReadOnlyList<ApprovalReviewRequest>>(
                    expectedBadRequestObjectResult);

            this.approvalReviewerOrchestrationServiceMock.Setup(service =>
                service.RetrieveApprovalReviewRequestsAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(validationException);

            // when
            ActionResult<IReadOnlyList<ApprovalReviewRequest>> actualActionResult =
                await this.approvalReviewersController.GetReviewRequestsAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalReviewerOrchestrationServiceMock.Verify(service =>
                service.RetrieveApprovalReviewRequestsAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalReviewerOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ReviewerDependencyExceptions))]
        public async Task ShouldReturnFailedDependencyOnGetReviewRequestsIfDependencyErrorAsync(
            Xeption dependencyException)
        {
            // given
            FailedDependencyObjectResult expectedFailedDependencyObjectResult =
                FailedDependency(dependencyException.InnerException);

            var expectedActionResult =
                new ActionResult<IReadOnlyList<ApprovalReviewRequest>>(
                    expectedFailedDependencyObjectResult);

            this.approvalReviewerOrchestrationServiceMock.Setup(service =>
                service.RetrieveApprovalReviewRequestsAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(dependencyException);

            // when
            ActionResult<IReadOnlyList<ApprovalReviewRequest>> actualActionResult =
                await this.approvalReviewersController.GetReviewRequestsAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalReviewerOrchestrationServiceMock.Verify(service =>
                service.RetrieveApprovalReviewRequestsAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalReviewerOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ReviewerServerExceptions))]
        public async Task ShouldReturnInternalServerErrorOnGetReviewRequestsIfServerErrorAsync(
            Xeption serverException)
        {
            // given
            InternalServerErrorObjectResult expectedInternalServerErrorObjectResult =
                InternalServerError(serverException);

            var expectedActionResult =
                new ActionResult<IReadOnlyList<ApprovalReviewRequest>>(
                    expectedInternalServerErrorObjectResult);

            this.approvalReviewerOrchestrationServiceMock.Setup(service =>
                service.RetrieveApprovalReviewRequestsAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(serverException);

            // when
            ActionResult<IReadOnlyList<ApprovalReviewRequest>> actualActionResult =
                await this.approvalReviewersController.GetReviewRequestsAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalReviewerOrchestrationServiceMock.Verify(service =>
                service.RetrieveApprovalReviewRequestsAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalReviewerOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnNotFoundOnDeleteReviewRequestIfApprovalDoesNotExistAsync()
        {
            // given
            string someMessage = GetRandomString();

            var notFoundApprovalReviewerOrchestrationException =
                new NotFoundApprovalReviewerOrchestrationException(
                    message: someMessage);

            var approvalOrchestrationValidationException =
                new ApprovalReviewerOrchestrationValidationException(
                    message: someMessage,
                    innerException: notFoundApprovalReviewerOrchestrationException);

            NotFoundObjectResult expectedNotFoundObjectResult =
                NotFound(notFoundApprovalReviewerOrchestrationException);

            var expectedActionResult =
                new ActionResult<ApprovalReviewRequest>(expectedNotFoundObjectResult);

            this.approvalReviewerOrchestrationServiceMock.Setup(service =>
                service.WithdrawApprovalReviewRequestAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(approvalOrchestrationValidationException);

            // when
            ActionResult<ApprovalReviewRequest> actualActionResult =
                await this.approvalReviewersController.DeleteReviewRequestAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    Guid.NewGuid().ToString(),
                    deletionReason: null,
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalReviewerOrchestrationServiceMock.Verify(service =>
                service.WithdrawApprovalReviewRequestAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalReviewerOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnUnauthorizedOnDeleteReviewRequestIfOutsideTheTierAsync()
        {
            // given
            string someMessage = GetRandomString();

            var unauthorizedApprovalOrchestrationException =
                new UnauthorizedApprovalReviewerOrchestrationException(
                    message: someMessage);

            var approvalOrchestrationValidationException =
                new ApprovalReviewerOrchestrationValidationException(
                    message: someMessage,
                    innerException: unauthorizedApprovalOrchestrationException);

            UnauthorizedObjectResult expectedUnauthorizedObjectResult =
                Unauthorized(unauthorizedApprovalOrchestrationException);

            var expectedActionResult =
                new ActionResult<ApprovalReviewRequest>(expectedUnauthorizedObjectResult);

            this.approvalReviewerOrchestrationServiceMock.Setup(service =>
                service.WithdrawApprovalReviewRequestAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(approvalOrchestrationValidationException);

            // when
            ActionResult<ApprovalReviewRequest> actualActionResult =
                await this.approvalReviewersController.DeleteReviewRequestAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    Guid.NewGuid().ToString(),
                    deletionReason: null,
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalReviewerOrchestrationServiceMock.Verify(service =>
                service.WithdrawApprovalReviewRequestAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalReviewerOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Covers the refusal a moderator can reach by ordinary use: §7.9 rule 5 refuses to
        /// withdraw an invitation its target has already ANSWERED, which the orchestration raises
        /// as an <c>InvalidApprovalOrchestrationException</c> and which must surface as a 400
        /// rather than being mistaken for a missing round.
        /// </summary>
        [Theory]
        [MemberData(nameof(ReviewerValidationExceptions))]
        public async Task ShouldReturnBadRequestOnDeleteReviewRequestIfValidationErrorAsync(
            Xeption validationException)
        {
            // given
            BadRequestObjectResult expectedBadRequestObjectResult =
                BadRequest(validationException.InnerException);

            var expectedActionResult =
                new ActionResult<ApprovalReviewRequest>(expectedBadRequestObjectResult);

            this.approvalReviewerOrchestrationServiceMock.Setup(service =>
                service.WithdrawApprovalReviewRequestAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(validationException);

            // when
            ActionResult<ApprovalReviewRequest> actualActionResult =
                await this.approvalReviewersController.DeleteReviewRequestAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    Guid.NewGuid().ToString(),
                    deletionReason: null,
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalReviewerOrchestrationServiceMock.Verify(service =>
                service.WithdrawApprovalReviewRequestAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalReviewerOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ReviewerDependencyExceptions))]
        public async Task ShouldReturnFailedDependencyOnDeleteReviewRequestIfDependencyErrorAsync(
            Xeption dependencyException)
        {
            // given
            FailedDependencyObjectResult expectedFailedDependencyObjectResult =
                FailedDependency(dependencyException.InnerException);

            var expectedActionResult =
                new ActionResult<ApprovalReviewRequest>(expectedFailedDependencyObjectResult);

            this.approvalReviewerOrchestrationServiceMock.Setup(service =>
                service.WithdrawApprovalReviewRequestAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(dependencyException);

            // when
            ActionResult<ApprovalReviewRequest> actualActionResult =
                await this.approvalReviewersController.DeleteReviewRequestAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    Guid.NewGuid().ToString(),
                    deletionReason: null,
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalReviewerOrchestrationServiceMock.Verify(service =>
                service.WithdrawApprovalReviewRequestAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalReviewerOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ReviewerServerExceptions))]
        public async Task ShouldReturnInternalServerErrorOnDeleteReviewRequestIfServerErrorAsync(
            Xeption serverException)
        {
            // given
            InternalServerErrorObjectResult expectedInternalServerErrorObjectResult =
                InternalServerError(serverException);

            var expectedActionResult =
                new ActionResult<ApprovalReviewRequest>(
                    expectedInternalServerErrorObjectResult);

            this.approvalReviewerOrchestrationServiceMock.Setup(service =>
                service.WithdrawApprovalReviewRequestAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(serverException);

            // when
            ActionResult<ApprovalReviewRequest> actualActionResult =
                await this.approvalReviewersController.DeleteReviewRequestAsync(
                    GetRandomEntityType(),
                    Guid.NewGuid(),
                    Guid.NewGuid().ToString(),
                    deletionReason: null,
                    default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalReviewerOrchestrationServiceMock.Verify(service =>
                service.WithdrawApprovalReviewRequestAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalReviewerOrchestrationServiceMock.VerifyNoOtherCalls();
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
                nameof(ApprovalReviewersController.GetReviewerCandidatesAsync),
                nameof(ApprovalReviewersController.GetReviewerDisplayNamesAsync),
                nameof(ApprovalReviewersController.PostReviewRequestAsync),
                nameof(ApprovalReviewersController.GetReviewRequestsAsync),
                nameof(ApprovalReviewersController.DeleteReviewRequestAsync)
            };

            // When
            List<string> actualActions = GetActions()
                .Select(action => action.Name)
                .ToList();

            // Then
            actualActions.Should().BeEquivalentTo(expectedActions);
        }

        /// <summary>
        /// <b>The empty expected list is the assertion, not a placeholder.</b> §16.7.4 restricts
        /// these routes to the review tier — <c>Administrators</c>, the <c>Publishers</c> tier and
        /// the <c>Reviewers</c> tier — and each tier is matched by SUFFIX: global
        /// <c>Publishers</c> or <c>Reviewers</c>, global <c>Administrators</c>, or any role ending
        /// <c>-Publishers</c> or <c>-Reviewers</c>, including the content-type-scoped
        /// <c>%EntityType%-%ContentType%-Publishers</c> tier of §18.6 rule 5. These routes are
        /// generic over <c>EntityType</c> as well, so no fixed <c>Roles = ...</c> list can express
        /// the set, and any partial list would lock out the content-type tier today and every
        /// entity type added later. The tier decision therefore lives in the orchestration alone
        /// (§14.6), and this pins the attribute to the coarse authenticated-only gate so a future
        /// fixed list has to be argued for rather than slipped in.
        /// </summary>
        [Theory]
        [InlineData(nameof(ApprovalReviewersController.GetReviewerCandidatesAsync))]
        [InlineData(nameof(ApprovalReviewersController.GetReviewerDisplayNamesAsync))]
        [InlineData(nameof(ApprovalReviewersController.PostReviewRequestAsync))]
        [InlineData(nameof(ApprovalReviewersController.GetReviewRequestsAsync))]
        [InlineData(nameof(ApprovalReviewersController.DeleteReviewRequestAsync))]
        public void ActionShouldCarryAuthorizeWithNoFixedRoleList(string actionName)
        {
            // Given
            var controllerType = typeof(ApprovalReviewersController);
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
        [InlineData(nameof(ApprovalReviewersController.GetReviewerCandidatesAsync))]
        [InlineData(nameof(ApprovalReviewersController.GetReviewerDisplayNamesAsync))]
        [InlineData(nameof(ApprovalReviewersController.PostReviewRequestAsync))]
        [InlineData(nameof(ApprovalReviewersController.GetReviewRequestsAsync))]
        [InlineData(nameof(ApprovalReviewersController.DeleteReviewRequestAsync))]
        public void ActionShouldNotAllowAnonymous(string actionName)
        {
            // Given
            var controllerType = typeof(ApprovalReviewersController);
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
        /// §14.7 posture D throughout — every route here names resolved policy or names people —
        /// so nothing on this controller may opt out of authentication.
        /// </summary>
        [Fact]
        public void ControllerShouldNotAllowAnonymous()
        {
            // Given
            var controllerType = typeof(ApprovalReviewersController);
            Type attributeType = typeof(AllowAnonymousAttribute);

            // When
            var attribute = controllerType
                .GetCustomAttributes(attributeType, inherit: true)
                .FirstOrDefault();

            // Then
            attribute.Should().BeNull();
        }

        private static List<MethodInfo> GetActions() =>
            typeof(ApprovalReviewersController)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(method => method.IsSpecialName is false)
                .ToList();

        private static bool HasAttribute(MethodInfo method, Type attributeType) =>
            method.GetCustomAttributes(attributeType, inherit: true).Any();

        private static string GetRandomString() =>
            new MnemonicString(wordCount: GetRandomNumber()).GetValue();

        private static int GetRandomNumber() =>
            new IntRange(min: 2, max: 10).GetValue();

        private static EntityType GetRandomEntityType() =>
            GetRandomEnumValue<EntityType>();

        private static T GetRandomEnumValue<T>() where T : struct, Enum
        {
            T[] values = Enum.GetValues<T>();

            return values[new IntRange(min: 0, max: values.Length - 1).GetValue()];
        }
    }
}
