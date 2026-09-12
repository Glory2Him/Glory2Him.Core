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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests.Exceptions;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Foundations.IdentityUsers.Exceptions;
using Glory2Him.Core.Models.Orchestrations.Approvals;
using Glory2Him.Core.Models.Orchestrations.ApprovalReviewers.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.ApprovalReviewers
{
    /// <summary>
    /// The ApprovalReviewRequest foundation's exceptions must categorise like every other
    /// foundation's. Unmapped, the whole family falls to the orchestration's catch-all and every
    /// routine refusal - an over-long deletion reason, a blocked caller, a uniqueness collision -
    /// is reported to the caller as a 424 infrastructure fault.
    /// </summary>
    public partial class ApprovalReviewerOrchestrationServiceTests
    {
        [Fact]
        public async Task ShouldCategoriseRequestFoundationValidationAsDependencyValidationAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);
            Guid approvalId = Guid.NewGuid();
            Guid invitedId = Guid.NewGuid();
            SetupReviewerScope(approvalId: approvalId);

            SetupTierMembers(CreateIdentityUser(invitedId, preferredName: "Mary"));

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
                this.approvalReviewerOrchestrationService.RequestApprovalReviewAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    invitedId.ToString(),
                    TestContext.Current.CancellationToken);

            // then: a caller-fixable refusal, NOT a dependency fault
            await Assert.ThrowsAsync<ApprovalReviewerOrchestrationDependencyValidationException>(
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

            SetupTierMembers(CreateIdentityUser(invitedId, preferredName: "Mary"));

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
                this.approvalReviewerOrchestrationService.RequestApprovalReviewAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    invitedId.ToString(),
                    TestContext.Current.CancellationToken);

            ApprovalReviewerOrchestrationDependencyValidationException actual =
                await Assert.ThrowsAsync<ApprovalReviewerOrchestrationDependencyValidationException>(
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

            // No round on the key, stated on the ENTITY-KEYED gather because that is the read the
            // resolver makes. The repair that follows cannot open one either: the entity's own
            // status is unreadable here, so §9.8's in-play gate refuses and the retry answers
            // null again.
            this.accessBrokerMock.Setup(broker =>
                broker.RetrieveApprovalReviewerScopeByEntityAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((ApprovalReviewerScope)null);

            // when
            ValueTask<ApprovalReviewRequest> withdrawTask =
                this.approvalReviewerOrchestrationService.WithdrawApprovalReviewRequestAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    Guid.NewGuid().ToString(),
                    deletionReason: null,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalReviewerOrchestrationValidationException actual =
                await Assert.ThrowsAsync<ApprovalReviewerOrchestrationValidationException>(
                    withdrawTask.AsTask);

            // then: the exposer maps THIS to 404
            actual.InnerException.Should().BeOfType<NotFoundApprovalReviewerOrchestrationException>();

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
                this.approvalReviewerOrchestrationService.WithdrawApprovalReviewRequestAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    requestedUserId,
                    deletionReason: null,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then: a dependency-validation failure, which the exposer maps to 400 - not 404
            await Assert.ThrowsAsync<ApprovalReviewerOrchestrationDependencyValidationException>(
                withdrawTask.AsTask);
        }

        // The IdentityUser foundation is a THREE-type family, and that is correct rather than an
        // omission (issue #518 finding 1): IIdentityUserService exposes two read-only operations,
        // and a read-only contract has no uniqueness collision, no foreign-key violation and no
        // constraint conflict — nothing that produces a dependency-validation failure. There is no
        // IdentityUserDependencyValidationException anywhere in the solution, and there should not
        // be one; nobody is to mint a fourth type for symmetry with the four-block arms beside it.
        public static TheoryData<Xeption> ReviewerCandidatesIdentityUserValidationExceptions()
        {
            string randomMessage = GetRandomString();
            var innerException = new Xeption(message: randomMessage);

            return new TheoryData<Xeption>
            {
                new IdentityUserValidationException(
                    message: randomMessage, innerException: innerException),
            };
        }

        // The two failure-shaped IdentityUser families (issue #518 criterion 6).
        public static TheoryData<Xeption> ReviewerCandidatesIdentityUserFailureExceptions()
        {
            string randomMessage = GetRandomString();
            var innerException = new Xeption(message: randomMessage);

            return new TheoryData<Xeption>
            {
                new IdentityUserDependencyException(
                    message: randomMessage, innerException: innerException),

                new IdentityUserServiceException(
                    message: randomMessage, innerException: innerException),
            };
        }

        [Theory]
        [MemberData(nameof(ReviewerCandidatesIdentityUserValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnCandidatesIfTheTierReadDoesAndLogItAsync(
            Xeption identityFoundationException)
        {
            // given: a validation-shaped refusal from the IdentityUser foundation, raised by the
            // tier-membership read the candidates listing composes (issue #518 criterion 5) — a
            // fault in what THIS orchestration asked for, so it becomes a DEPENDENCY VALIDATION
            // exception rather than the 424 the missing arm used to produce.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);
            Guid approvalId = Guid.NewGuid();
            SetupReviewerScope(approvalId: approvalId);

            var expectedDependencyValidationException =
                new ApprovalReviewerOrchestrationDependencyValidationException(
                    message: ExpectedDependencyValidationMessage,
                    innerException: (identityFoundationException.InnerException as Xeption)!);

            this.identityUserServiceMock.Setup(service =>
                service.RetrieveIdentityUsersInRolesAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(identityFoundationException);

            // when
            ValueTask<IReadOnlyList<ReviewerCandidate>> candidatesTask =
                this.approvalReviewerOrchestrationService.RetrieveReviewerCandidatesAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            ApprovalReviewerOrchestrationDependencyValidationException actualException =
                await Assert.ThrowsAsync<ApprovalReviewerOrchestrationDependencyValidationException>(
                    candidatesTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyValidationException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);
        }

        [Theory]
        [MemberData(nameof(ReviewerCandidatesIdentityUserFailureExceptions))]
        public async Task ShouldThrowDependencyExceptionOnCandidatesIfTheTierReadDoesAndLogItAsync(
            Xeption identityFoundationException)
        {
            // given: the same tier read, failing for an external reason instead (issue #518
            // criterion 6) — unchanged behaviour, pinned so criterion 5 cannot over-reach.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);
            Guid approvalId = Guid.NewGuid();
            SetupReviewerScope(approvalId: approvalId);

            var expectedDependencyException =
                new ApprovalReviewerOrchestrationDependencyException(
                    message: ExpectedDependencyMessage,
                    innerException: (identityFoundationException.InnerException as Xeption)!);

            this.identityUserServiceMock.Setup(service =>
                service.RetrieveIdentityUsersInRolesAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(identityFoundationException);

            // when
            ValueTask<IReadOnlyList<ReviewerCandidate>> candidatesTask =
                this.approvalReviewerOrchestrationService.RetrieveReviewerCandidatesAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            ApprovalReviewerOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalReviewerOrchestrationDependencyException>(
                    candidatesTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);
        }
    }
}
