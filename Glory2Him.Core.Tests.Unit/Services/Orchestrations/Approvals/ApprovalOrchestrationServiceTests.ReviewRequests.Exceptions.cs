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
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests.Exceptions;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Orchestrations.Approvals.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Approvals
{
    /// <summary>
    /// The ApprovalReviewRequest foundation's exceptions must categorise like every other
    /// foundation's. Unmapped, the whole family falls to the orchestration's catch-all and every
    /// routine refusal - an over-long deletion reason, a blocked caller, a uniqueness collision -
    /// is reported to the caller as a 424 infrastructure fault.
    /// </summary>
    public partial class ApprovalOrchestrationServiceTests
    {
        [Fact]
        public async Task ShouldCategoriseRequestFoundationValidationAsDependencyValidationAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);
            Guid approvalId = Guid.NewGuid();
            Guid invitedId = Guid.NewGuid();
            SetupReviewerScope(approvalId: approvalId);

            this.identityUserServiceMock.Setup(service =>
                service.RetrieveIdentityUsersInRolesAsync(
                    It.IsAny<System.Collections.Generic.IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new System.Collections.Generic.List<
                            Glory2Him.Core.Models.Foundations.IdentityUsers.IdentityUser>
                        {
                            CreateIdentityUser(invitedId, preferredName: "Mary"),
                        });

            var foundationValidationException = new ApprovalReviewRequestValidationException(
                message: "Approval review request validation error occurred, " +
                    "fix the errors and try again.",
                innerException: new InvalidApprovalReviewRequestException(message: "invalid"));

            this.approvalReviewRequestServiceMock.Setup(service =>
                service.AddApprovalReviewRequestAsync(
                    It.IsAny<ApprovalReviewRequest>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(foundationValidationException);

            // when
            ValueTask<ApprovalReviewRequest> requestTask =
                this.approvalOrchestrationService.RequestApprovalReviewAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    invitedId.ToString(),
                    TestContext.Current.CancellationToken);

            // then: a caller-fixable refusal, NOT a dependency fault
            await Assert.ThrowsAsync<ApprovalOrchestrationDependencyValidationException>(
                requestTask.AsTask);
        }

        /// <summary>
        /// The uniqueness collision of 7.9 rule 1, which a concurrent double-invite reaches even
        /// though rule 4 dissolves the sequential case. It must surface as a conflict the caller
        /// can act on rather than as "contact support".
        /// </summary>
        [Fact]
        public async Task ShouldCategoriseRequestUniquenessCollisionAsDependencyValidationAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);
            Guid approvalId = Guid.NewGuid();
            Guid invitedId = Guid.NewGuid();
            SetupReviewerScope(approvalId: approvalId);

            this.identityUserServiceMock.Setup(service =>
                service.RetrieveIdentityUsersInRolesAsync(
                    It.IsAny<System.Collections.Generic.IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new System.Collections.Generic.List<
                            Glory2Him.Core.Models.Foundations.IdentityUsers.IdentityUser>
                        {
                            CreateIdentityUser(invitedId, preferredName: "Mary"),
                        });

            var alreadyExists = new AlreadyExistsApprovalReviewRequestException(
                message: "Approval review request already exists, " +
                    "a uniqueness rule rejected the write.",
                innerException: new Exception(),
                data: new System.Collections.Hashtable());

            var dependencyValidation = new ApprovalReviewRequestDependencyValidationException(
                message: "Approval review request dependency validation error occurred, " +
                    "fix the errors and try again.",
                innerException: alreadyExists);

            this.approvalReviewRequestServiceMock.Setup(service =>
                service.AddApprovalReviewRequestAsync(
                    It.IsAny<ApprovalReviewRequest>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(dependencyValidation);

            // when
            ValueTask<ApprovalReviewRequest> requestTask =
                this.approvalOrchestrationService.RequestApprovalReviewAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    invitedId.ToString(),
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyValidationException actual =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyValidationException>(
                    requestTask.AsTask);

            // then: the inner survives, so the exposer's Conflict branch can see it
            actual.InnerException.Should().BeOfType<AlreadyExistsApprovalReviewRequestException>();
        }

        /// <summary>
        /// Withdrawal used to need a not-found translated at its own call site: keyed on a request
        /// ROW it did no resolution, so a missing row surfaced as the foundation's validation
        /// failure and the caller was told 400 for an id that named nothing.
        ///
        /// <para>Re-keyed on the round and the person, it resolves the entity first like every
        /// sibling, and the not-found arises THERE — from an entity with no approval behind it.
        /// The exposer's NotFound branch stays reachable, from the site that owns the question,
        /// and the translation is gone rather than merely moved.</para>
        /// </summary>
        [Fact]
        public async Task ShouldThrowNotFoundOnWithdrawWhenTheEntityHasNoApprovalAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);

            this.approvalServiceMock.Setup(service =>
                service.FindApprovalByEntityAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((ApprovalEntityMatch)null);

            // when
            ValueTask<ApprovalReviewRequest> withdrawTask =
                this.approvalOrchestrationService.WithdrawApprovalReviewRequestAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    Guid.NewGuid().ToString(),
                    deletionReason: null,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actual =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    withdrawTask.AsTask);

            // then: the exposer maps THIS to 404
            actual.InnerException.Should().BeOfType<NotFoundApprovalOrchestrationException>();

            this.approvalReviewRequestServiceMock.Verify(service =>
                service.RemoveApprovalReviewRequestByIdAsync(
                    It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// A foundation validation failure on the remove keeps its own category. Without this a
        /// genuine bad request — an over-long deletion reason, which the foundation caps at 500 —
        /// would reach the caller wearing the wrong status.
        /// </summary>
        [Fact]
        public async Task ShouldSurfaceOtherWithdrawValidationFailuresAsDependencyValidationAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            Guid approvalId = Guid.NewGuid();
            Guid requestId = Guid.NewGuid();
            string requestedUserId = Guid.NewGuid().ToString();

            SetupReviewerScope(
                approvalId: approvalId,
                activeRequests: new[]
                {
                    new ActiveReviewRequest
                    {
                        Id = requestId,
                        RequestedUserId = requestedUserId,
                    }
                });

            var foundationInvalid = new ApprovalReviewRequestValidationException(
                message: "Approval review request validation error occurred, " +
                    "fix the errors and try again.",
                innerException: new InvalidApprovalReviewRequestException(
                    message: "Approval review request is invalid, fix the errors and try again."));

            this.approvalReviewRequestServiceMock.Setup(service =>
                service.RemoveApprovalReviewRequestByIdAsync(
                    requestId,
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(foundationInvalid);

            // when
            ValueTask<ApprovalReviewRequest> withdrawTask =
                this.approvalOrchestrationService.WithdrawApprovalReviewRequestAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    requestedUserId,
                    deletionReason: null,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then: a dependency-validation failure, which the exposer maps to 400 - not 404
            await Assert.ThrowsAsync<ApprovalOrchestrationDependencyValidationException>(
                withdrawTask.AsTask);
        }
    }
}
