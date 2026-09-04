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
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);
            Guid approvalId = Guid.NewGuid();
            Guid invitedId = Guid.NewGuid();
            SetupReviewerScope(approvalId: approvalId);

            SetupTierMembers(CreateIdentityUser(invitedId, preferredName: "Mary"));

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
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);
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
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);
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
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);
            Guid approvalId = Guid.NewGuid();
            SetupReviewerScope(approvalId: approvalId);

            // the tier read comes back without the invited person in it
            SetupTierMembers();

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
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);
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
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);
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

        /// <summary>
        /// The read §7.9 was written around. Answered through the CALLER-FACING foundation read
        /// rather than off the scope: the scope's ActiveRequests are gathered unfiltered because
        /// invitability is a fact about storage (§16.7.4), and they carry no display name.
        /// </summary>
        [Fact]
        public async Task ShouldRetrieveTheRoundsOutstandingReviewRequestsAsync()
        {
            // given: two rows on this round and one on another, so a read that forgot to filter
            // by ApprovalId is distinguishable from one that did
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            Guid approvalId = Guid.NewGuid();
            Guid otherApprovalId = Guid.NewGuid();

            SetupReviewerScope(approvalId: approvalId);

            var zoe = new ApprovalReviewRequest
            {
                Id = Guid.NewGuid(),
                ApprovalId = approvalId,
                RequestedUserDisplayName = "Zoe",
            };

            var adam = new ApprovalReviewRequest
            {
                Id = Guid.NewGuid(),
                ApprovalId = approvalId,
                RequestedUserDisplayName = "adam",
            };

            var anotherRoundsRequest = new ApprovalReviewRequest
            {
                Id = Guid.NewGuid(),
                ApprovalId = otherApprovalId,
                RequestedUserDisplayName = "Someone Else",
            };

            this.approvalReviewRequestServiceMock.Setup(service =>
                service.RetrieveAllApprovalReviewRequestsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<ApprovalReviewRequest>
                    {
                        zoe,
                        anotherRoundsRequest,
                        adam,
                    }.AsQueryable());

            // when
            IReadOnlyList<ApprovalReviewRequest> actual =
                await this.approvalOrchestrationService.RetrieveApprovalReviewRequestsAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            // then: this round only, and ordered case-insensitively by display name the way the
            // candidates read beside it is — "adam" before "Zoe", not after
            actual.Should().Equal(new[] { adam, zoe });
        }

        [Fact]
        public async Task ShouldThrowUnauthorizedOnRetrieveReviewRequestsOutsideTheTierAsync()
        {
            // given: the same user-enumeration posture the candidates read carries — these rows
            // name people, so they are not for everyone who happens to be signed in
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            // when
            ValueTask<IReadOnlyList<ApprovalReviewRequest>> retrieveTask =
                this.approvalOrchestrationService.RetrieveApprovalReviewRequestsAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    retrieveTask.AsTask);

            // then
            actualException.InnerException.Should()
                .BeOfType<UnauthorizedApprovalOrchestrationException>();

            this.approvalReviewRequestServiceMock.Verify(service =>
                service.RetrieveAllApprovalReviewRequestsAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// Keyed on the round and the PERSON. The row id is resolved from the scope rather than
        /// supplied, which is what removes the round trip the create's 204 had made impossible.
        /// </summary>
        [Fact]
        public async Task ShouldWithdrawApprovalReviewRequestAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            Guid approvalId = Guid.NewGuid();
            Guid requestId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();
            string requestedUserId = Guid.NewGuid().ToString();
            string deletionReason = GetRandomString();
            var withdrawn = new ApprovalReviewRequest { Id = requestId, IsDeleted = true };

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

            this.approvalReviewRequestServiceMock.Setup(service =>
                service.RemoveApprovalReviewRequestByIdAsync(
                    requestId,
                    deletionReason,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(withdrawn);

            // when
            ApprovalReviewRequest actual =
                await this.approvalOrchestrationService.WithdrawApprovalReviewRequestAsync(
                    EntityType.ContentItem,
                    entityId,
                    requestedUserId,
                    deletionReason,
                    TestContext.Current.CancellationToken);

            // then: the row the SCOPE named was the one removed, not one the caller chose
            actual.Should().BeSameAs(withdrawn);

            this.approvalReviewRequestServiceMock.Verify(service =>
                service.RemoveApprovalReviewRequestByIdAsync(
                    requestId,
                    deletionReason,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        /// <summary>
        /// Rule 5 stops at the answer. Withdrawing says the invitation was a mistake, and once it
        /// has been answered that is no longer anyone's to say - the verdict stands, and the
        /// record of who was asked is part of how it came about.
        ///
        /// <para>Reachable only while the row is still LIVE, which after rule 6 means retirement
        /// did not run. That is the one place a standing invitation and a cast vote coexist.</para>
        /// </summary>
        [Fact]
        public async Task ShouldRefuseToWithdrawARequestItsTargetHasAlreadyAnsweredAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            Guid requestId = Guid.NewGuid();
            Guid approvalId = Guid.NewGuid();
            Guid answeredById = Guid.NewGuid();

            SetupReviewerScope(
                approvalId: approvalId,
                activeReviewerUserIds: new[] { answeredById.ToString() },
                activeRequests: new[]
                {
                    new ActiveReviewRequest
                    {
                        Id = requestId,
                        RequestedUserId = answeredById.ToString(),
                    }
                });

            // when
            ValueTask<ApprovalReviewRequest> withdrawTask =
                this.approvalOrchestrationService.WithdrawApprovalReviewRequestAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    answeredById.ToString(),
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
        /// Withdrawing an invitation that is not outstanding - already withdrawn, or taken by a
        /// rule 6 retirement - stays the harmless no-op it has always been.
        ///
        /// <para>What CHANGED with the re-key is where the no-op is decided. Keyed on a row id the
        /// operation had to attempt the remove and let the foundation answer; resolved from the
        /// round it simply finds nothing outstanding and stops. Null here is the exposer's 204,
        /// which is what the id-keyed route answered for this case too.</para>
        /// </summary>
        [Fact]
        public async Task ShouldWithdrawIdempotentlyWhenNothingIsOutstandingForThatPersonAsync()
        {
            // given: a round with an invitation out to somebody ELSE, so the miss is specific to
            // the person asked for rather than an empty round answering everything
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            Guid approvalId = Guid.NewGuid();

            SetupReviewerScope(
                approvalId: approvalId,
                activeRequests: new[]
                {
                    new ActiveReviewRequest
                    {
                        Id = Guid.NewGuid(),
                        RequestedUserId = Guid.NewGuid().ToString(),
                    }
                });

            // when
            ApprovalReviewRequest actual =
                await this.approvalOrchestrationService.WithdrawApprovalReviewRequestAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    Guid.NewGuid().ToString(),
                    GetRandomString(),
                    TestContext.Current.CancellationToken);

            // then
            actual.Should().BeNull();

            // and nothing was removed — least of all the OTHER person's standing invitation
            this.approvalReviewRequestServiceMock.Verify(service =>
                service.RemoveApprovalReviewRequestByIdAsync(
                    It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowUnauthorizedOnWithdrawIfCallerIsOutsideTheRequestingTierAsync()
        {
            // given: rule 5 is wide across the tier, but it stops AT the tier
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            // when
            ValueTask<ApprovalReviewRequest> withdrawTask =
                this.approvalOrchestrationService.WithdrawApprovalReviewRequestAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    Guid.NewGuid().ToString(),
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
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);
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
                    RecordedReviewerUserIds = Array.Empty<string>(),
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

            SetupTierMembers(CreateIdentityUser(invitedId, preferredName: "Invited"));

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
