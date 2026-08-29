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
using Glory2Him.Core.Models.Foundations.IdentityUsers;
using Glory2Him.Core.Models.Orchestrations.Approvals.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests.Exceptions;
using G2H.Security.Client.Models.Foundations.Access;
using Xeptions;
using Glory2Him.Core.Models.Foundations.Approvals;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Approvals
{
    public partial class ApprovalOrchestrationServiceTests
    {
        [Fact]
        public async Task ShouldRequestApprovalReviewAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher);
            Guid approvalId = Guid.NewGuid();
            Guid invitedId = Guid.NewGuid();
            SetupReviewerScope(approvalId: approvalId);

            this.identityUserServiceMock.Setup(service =>
                service.RetrieveIdentityUsersInRolesAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new List<IdentityUser>
                        {
                            CreateIdentityUser(invitedId, preferredName: "Mary"),
                        });

            ApprovalReviewRequest captured = null;

            this.approvalReviewRequestServiceMock.Setup(service =>
                service.AddApprovalReviewRequestAsync(
                    It.IsAny<ApprovalReviewRequest>(),
                    It.IsAny<CancellationToken>()))
                        .Callback<ApprovalReviewRequest, CancellationToken>(
                            (request, token) => captured = request)
                        .ReturnsAsync((ApprovalReviewRequest request, CancellationToken token) =>
                            request);

            // when
            ApprovalReviewRequest actual =
                await this.approvalOrchestrationService.RequestApprovalReviewAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    invitedId.ToString(),
                    TestContext.Current.CancellationToken);

            // then
            actual.Should().NotBeNull();
            captured.ApprovalId.Should().Be(approvalId);
            captured.RequestedUserId.Should().Be(invitedId.ToString());

            // Denormalised at request time (7.9) so the panel does not re-read a name across a
            // boundary that may be unavailable.
            captured.RequestedUserDisplayName.Should().Be("Mary");
        }

        /// <summary>
        /// Rule 4 - asking twice is harmless, so the standing invitation comes back unchanged
        /// rather than colliding with the uniqueness index or raising a conflict. Nothing is
        /// written, and the identity store is not even consulted.
        /// </summary>
        [Fact]
        public async Task ShouldReturnTheStandingRequestWhenTheUserIsAlreadyInvitedAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher);
            Guid approvalId = Guid.NewGuid();
            Guid invitedId = Guid.NewGuid();
            Guid standingRequestId = Guid.NewGuid();

            SetupReviewerScope(
                approvalId: approvalId,
                activeRequests: new[]
                {
                    new ActiveReviewRequest
                    {
                        Id = standingRequestId,
                        RequestedUserId = invitedId.ToString(),
                    }
                });

            var standingRequest = new ApprovalReviewRequest { Id = standingRequestId };

            this.approvalReviewRequestServiceMock.Setup(service =>
                service.RetrieveApprovalReviewRequestByIdAsync(
                    standingRequestId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(standingRequest);

            // when
            ApprovalReviewRequest actual =
                await this.approvalOrchestrationService.RequestApprovalReviewAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    invitedId.ToString(),
                    TestContext.Current.CancellationToken);

            // then
            actual.Should().BeSameAs(standingRequest);

            this.approvalReviewRequestServiceMock.Verify(service =>
                service.AddApprovalReviewRequestAsync(
                    It.IsAny<ApprovalReviewRequest>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.identityUserServiceMock.Verify(service =>
                service.RetrieveIdentityUsersInRolesAsync(
                    It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// Rule 3, owner half. HR-1 has no bypass, so inviting the owner would create an
        /// invitation the foundation would then refuse to let them answer.
        /// </summary>
        [Fact]
        public async Task ShouldThrowOnRequestIfTheInvitedUserOwnsTheEntityAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher);
            Guid approvalId = Guid.NewGuid();
            Guid ownerId = Guid.NewGuid();
            SetupReviewerScope(approvalId: approvalId, entityCreatedBy: ownerId.ToString());

            // when
            ValueTask<ApprovalReviewRequest> requestTask =
                this.approvalOrchestrationService.RequestApprovalReviewAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    ownerId.ToString(),
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    requestTask.AsTask);

            // then
            actualException.InnerException.Should()
                .BeOfType<InvalidApprovalOrchestrationException>();

            this.approvalReviewRequestServiceMock.Verify(service =>
                service.AddApprovalReviewRequestAsync(
                    It.IsAny<ApprovalReviewRequest>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// Rule 3, tier half - resolved from the identity store rather than from the caller. An
        /// invitation to somebody ineligible is a lie the panel would render, and one the
        /// foundation could not catch: a request row names no entity type.
        /// </summary>
        [Fact]
        public async Task ShouldThrowOnRequestIfTheInvitedUserIsNotInTheReviewTierAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher);
            Guid approvalId = Guid.NewGuid();
            SetupReviewerScope(approvalId: approvalId);

            // the tier read comes back without the invited person in it
            this.identityUserServiceMock.Setup(service =>
                service.RetrieveIdentityUsersInRolesAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new List<IdentityUser>());

            // when
            ValueTask<ApprovalReviewRequest> requestTask =
                this.approvalOrchestrationService.RequestApprovalReviewAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    Guid.NewGuid().ToString(),
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    requestTask.AsTask);

            // then
            actualException.InnerException.Should()
                .BeOfType<InvalidApprovalOrchestrationException>();

            this.approvalReviewRequestServiceMock.Verify(service =>
                service.AddApprovalReviewRequestAsync(
                    It.IsAny<ApprovalReviewRequest>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// Rule 7 - only a Submitted round accepts invitations. Before submission there is nothing
        /// to review; once it closes a review may no longer be written (7.7 rule 2b), so the
        /// invitation could never be answered.
        /// </summary>
        [Theory]
        [InlineData(ApprovalStatus.Draft)]
        [InlineData(ApprovalStatus.Approved)]
        [InlineData(ApprovalStatus.Rejected)]
        public async Task ShouldThrowOnRequestIfTheRoundIsNotOpenAsync(ApprovalStatus status)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher);
            Guid approvalId = Guid.NewGuid();
            SetupReviewerScope(approvalId: approvalId, approvalStatus: status);

            // when
            ValueTask<ApprovalReviewRequest> requestTask =
                this.approvalOrchestrationService.RequestApprovalReviewAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    Guid.NewGuid().ToString(),
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    requestTask.AsTask);

            // then
            actualException.InnerException.Should()
                .BeOfType<InvalidApprovalOrchestrationException>();

            this.approvalReviewRequestServiceMock.Verify(service =>
                service.AddApprovalReviewRequestAsync(
                    It.IsAny<ApprovalReviewRequest>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// The other half of rule 4, and it behaves exactly like the first: inviting somebody who
        /// has already answered dissolves quietly. The goal of the operation is that the person
        /// has been asked, and an answer is more than that.
        ///
        /// <para>Nothing is created, deliberately. Rule 6 retires an invitation the moment its
        /// target answers, so a fresh one here could never be retired - the vote that would have
        /// done it has already happened - and rule 5 refuses to withdraw an answered invitation,
        /// so nobody could clear it by hand either. It would sit in the picker forever.</para>
        /// </summary>
        [Fact]
        public async Task ShouldDissolveTheRequestQuietlyIfTheInvitedUserHasAlreadyReviewedAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher);
            Guid approvalId = Guid.NewGuid();
            Guid reviewedId = Guid.NewGuid();

            SetupReviewerScope(
                approvalId: approvalId,
                activeReviewerUserIds: new[] { reviewedId.ToString() });

            // when
            ApprovalReviewRequest actualRequest =
                await this.approvalOrchestrationService.RequestApprovalReviewAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    reviewedId.ToString(),
                    TestContext.Current.CancellationToken);

            // then: no error, and nothing to hand back - the invitation this would have returned
            // was retired when they answered
            actualRequest.Should().BeNull();

            this.approvalReviewRequestServiceMock.Verify(service =>
                service.AddApprovalReviewRequestAsync(
                    It.IsAny<ApprovalReviewRequest>(), It.IsAny<CancellationToken>()),
                // An invitation created after the answer could never be retired or withdrawn,
                // and would sit in the picker forever.
                Times.Never);

            // and: it stops before the identity store, because there is nothing left to decide
            this.identityUserServiceMock.Verify(service =>
                service.RetrieveIdentityUsersInRolesAsync(
                    It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowUnauthorizedOnRequestIfCallerIsOutsideTheRequestingTierAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            // when
            ValueTask<ApprovalReviewRequest> requestTask =
                this.approvalOrchestrationService.RequestApprovalReviewAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    Guid.NewGuid().ToString(),
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    requestTask.AsTask);

            // then
            actualException.InnerException.Should()
                .BeOfType<UnauthorizedApprovalOrchestrationException>();

            this.approvalReviewRequestServiceMock.Verify(service =>
                service.AddApprovalReviewRequestAsync(
                    It.IsAny<ApprovalReviewRequest>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldWithdrawApprovalReviewRequestAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            Guid requestId = Guid.NewGuid();
            string deletionReason = GetRandomString();
            var withdrawn = new ApprovalReviewRequest { Id = requestId, IsDeleted = true };

            this.approvalReviewRequestServiceMock.Setup(service =>
                service.RemoveApprovalReviewRequestByIdAsync(
                    requestId,
                    deletionReason,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(withdrawn);

            // when
            ApprovalReviewRequest actual =
                await this.approvalOrchestrationService.WithdrawApprovalReviewRequestAsync(
                    requestId,
                    deletionReason,
                    TestContext.Current.CancellationToken);

            // then
            actual.Should().BeSameAs(withdrawn);
        }

        /// <summary>
        /// Rule 5 stops at the answer. Withdrawing says the invitation was a mistake, and once it
        /// has been answered that is no longer anyone's to say - the verdict stands, and the
        /// record of who was asked is part of how it came about.
        /// </summary>
        [Fact]
        public async Task ShouldRefuseToWithdrawARequestItsTargetHasAlreadyAnsweredAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            Guid requestId = Guid.NewGuid();
            Guid approvalId = Guid.NewGuid();
            Guid answeredById = Guid.NewGuid();

            this.approvalReviewRequestServiceMock.Setup(service =>
                service.RetrieveApprovalReviewRequestByIdAsync(
                    requestId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new ApprovalReviewRequest
                        {
                            Id = requestId,
                            ApprovalId = approvalId,
                            RequestedUserId = answeredById.ToString()
                        });

            SetupReviewerScope(
                approvalId: approvalId,
                activeReviewerUserIds: new[] { answeredById.ToString() });

            // when
            ValueTask<ApprovalReviewRequest> withdrawTask =
                this.approvalOrchestrationService.WithdrawApprovalReviewRequestAsync(
                    requestId,
                    GetRandomString(),
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    withdrawTask.AsTask);

            // then
            actualException.InnerException.Should()
                .BeOfType<InvalidApprovalOrchestrationException>();

            this.approvalReviewRequestServiceMock.Verify(service =>
                service.RemoveApprovalReviewRequestByIdAsync(
                    It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// The answered case is normally already GONE - rule 6 retires an invitation the moment
        /// its target answers - so the gate above is reached only where retirement has not run.
        /// Withdrawing a row that is already withdrawn must stay the harmless no-op it has always
        /// been, rather than becoming a not-found because a new lookup was added in front of it.
        /// </summary>
        [Fact]
        public async Task ShouldStillWithdrawIdempotentlyWhenTheRequestIsAlreadyGoneAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            Guid requestId = Guid.NewGuid();
            string deletionReason = GetRandomString();
            var alreadyWithdrawn = new ApprovalReviewRequest { Id = requestId, IsDeleted = true };

            this.approvalReviewRequestServiceMock.Setup(service =>
                service.RetrieveApprovalReviewRequestByIdAsync(
                    requestId,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new ApprovalReviewRequestValidationException(
                            message: "not found",
                            innerException: new NotFoundApprovalReviewRequestException(
                                message: "not found")));

            this.approvalReviewRequestServiceMock.Setup(service =>
                service.RemoveApprovalReviewRequestByIdAsync(
                    requestId,
                    deletionReason,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(alreadyWithdrawn);

            // when
            ApprovalReviewRequest actual =
                await this.approvalOrchestrationService.WithdrawApprovalReviewRequestAsync(
                    requestId,
                    deletionReason,
                    TestContext.Current.CancellationToken);

            // then: not-found on the LOOKUP means "nothing to check", never "nothing to remove"
            actual.Should().BeSameAs(alreadyWithdrawn);
        }

        [Fact]
        public async Task ShouldThrowUnauthorizedOnWithdrawIfCallerIsOutsideTheRequestingTierAsync()
        {
            // given: rule 5 is wide across the tier, but it stops AT the tier
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            // when
            ValueTask<ApprovalReviewRequest> withdrawTask =
                this.approvalOrchestrationService.WithdrawApprovalReviewRequestAsync(
                    Guid.NewGuid(),
                    deletionReason: null,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    withdrawTask.AsTask);

            // then
            actualException.InnerException.Should()
                .BeOfType<UnauthorizedApprovalOrchestrationException>();

            this.approvalReviewRequestServiceMock.Verify(service =>
                service.RemoveApprovalReviewRequestByIdAsync(
                    It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// The race rule 4's pre-check cannot see. Two callers inviting the same person both read
        /// a scope with no standing request, both try to write, and the unique index refuses the
        /// loser. "Somebody else asked them half a second before you" is the same outcome as "you
        /// asked twice", so it dissolves the same way rather than surfacing as a collision.
        /// </summary>
        [Fact]
        public async Task ShouldReturnTheWinningRequestWhenTwoInvitationsRaceAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher);
            Guid approvalId = Guid.NewGuid();
            Guid invitedId = Guid.NewGuid();
            Guid winningRequestId = Guid.NewGuid();

            ApprovalReviewerScope ScopeWith(IReadOnlyList<ActiveReviewRequest> activeRequests) =>
                new ApprovalReviewerScope
                {
                    ApprovalId = approvalId,
                    ApprovalStatus = ApprovalStatus.Submitted,
                    EntityCreatedBy = "the-entity-owner",

                    RoleSubjects = new[]
                    {
                        new RoleSubject
                        {
                            EntityType = nameof(EntityType.ContentItem),
                            ContentType = null,
                        }
                    },

                    ActiveReviewerUserIds = Array.Empty<string>(),
                    ActiveRequests = activeRequests,
                };

            this.approvalServiceMock.Setup(service =>
                service.FindApprovalByEntityAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new ApprovalEntityMatch
                        {
                            Id = approvalId,
                            ApprovalStatus = ApprovalStatus.Submitted,
                            IsDeleted = false,
                        });

            // the FIRST read sees nothing, which is what lets both callers try; the re-read after
            // the collision sees the winner's row
            this.accessBrokerMock.SetupSequence(broker =>
                broker.RetrieveApprovalReviewerScopeByIdAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(ScopeWith(Array.Empty<ActiveReviewRequest>()))
                        .ReturnsAsync(ScopeWith(new[]
                        {
                            new ActiveReviewRequest
                            {
                                Id = winningRequestId,
                                RequestedUserId = invitedId.ToString(),
                            }
                        }));

            this.identityUserServiceMock.Setup(service =>
                service.RetrieveIdentityUsersInRolesAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new List<IdentityUser>
                        {
                            CreateIdentityUser(invitedId, preferredName: "Invited"),
                        });

            this.approvalReviewRequestServiceMock.Setup(service =>
                service.AddApprovalReviewRequestAsync(
                    It.IsAny<ApprovalReviewRequest>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new ApprovalReviewRequestDependencyValidationException(
                            message: "collision",
                            innerException: new AlreadyExistsApprovalReviewRequestException(
                                message: "already exists",
                                innerException: new Xeption(),
                                data: new Xeption().Data)));

            var winningRequest = new ApprovalReviewRequest { Id = winningRequestId };

            this.approvalReviewRequestServiceMock.Setup(service =>
                service.RetrieveApprovalReviewRequestByIdAsync(
                    winningRequestId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(winningRequest);

            // when
            ApprovalReviewRequest actualRequest =
                await this.approvalOrchestrationService.RequestApprovalReviewAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    invitedId.ToString(),
                    TestContext.Current.CancellationToken);

            // then: the winner's row, not a collision
            actualRequest.Should().BeSameAs(winningRequest);
        }
    }
}
