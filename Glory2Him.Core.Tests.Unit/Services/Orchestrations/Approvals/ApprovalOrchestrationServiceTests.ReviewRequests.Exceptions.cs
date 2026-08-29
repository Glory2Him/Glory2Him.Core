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
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher);
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
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher);
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
        /// Withdrawal is keyed on the request ROW, so unlike every sibling it does no resolution
        /// of its own and has no earlier site at which a missing row becomes a not-found. It has
        /// to be translated at the call site, or the caller is told 400 for an id that simply
        /// does not exist and the exposer's NotFound branch is unreachable.
        /// </summary>
        [Fact]
        public async Task ShouldTranslateAMissingRequestIntoNotFoundOnWithdrawAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            Guid missingId = Guid.NewGuid();

            var foundationNotFound = new ApprovalReviewRequestValidationException(
                message: "Approval review request validation error occurred, " +
                    "fix the errors and try again.",
                innerException: new NotFoundApprovalReviewRequestException(
                    message: $"Approval review request not found with id: {missingId}."));

            this.approvalReviewRequestServiceMock.Setup(service =>
                service.RemoveApprovalReviewRequestByIdAsync(
                    missingId,
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(foundationNotFound);

            // when
            ValueTask<ApprovalReviewRequest> withdrawTask =
                this.approvalOrchestrationService.WithdrawApprovalReviewRequestAsync(
                    missingId,
                    deletionReason: null,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actual =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    withdrawTask.AsTask);

            // then: the exposer maps THIS to 404
            actual.InnerException.Should().BeOfType<NotFoundApprovalOrchestrationException>();
        }

        /// <summary>
        /// Only a not-found is translated. Every other foundation validation failure on the
        /// withdraw path must keep its own category, or a genuine bad request would be reported
        /// as a missing row.
        /// </summary>
        [Fact]
        public async Task ShouldNotTranslateOtherWithdrawValidationFailuresIntoNotFoundAsync()
        {
            // given: an over-long deletion reason, which the foundation caps at 500
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            Guid someId = Guid.NewGuid();

            var foundationInvalid = new ApprovalReviewRequestValidationException(
                message: "Approval review request validation error occurred, " +
                    "fix the errors and try again.",
                innerException: new InvalidApprovalReviewRequestException(
                    message: "Approval review request is invalid, fix the errors and try again."));

            this.approvalReviewRequestServiceMock.Setup(service =>
                service.RemoveApprovalReviewRequestByIdAsync(
                    someId,
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(foundationInvalid);

            // when
            ValueTask<ApprovalReviewRequest> withdrawTask =
                this.approvalOrchestrationService.WithdrawApprovalReviewRequestAsync(
                    someId,
                    deletionReason: null,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then: a dependency-validation failure, which the exposer maps to 400 - not 404
            await Assert.ThrowsAsync<ApprovalOrchestrationDependencyValidationException>(
                withdrawTask.AsTask);
        }
    }
}
