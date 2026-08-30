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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Approvals
{
    public partial class ApprovalServiceTests
    {
        [Fact]
        public async Task ShouldRetrieveAllApprovalsAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            IQueryable<Approval> randomApprovals = CreateRandomApprovals();

            foreach (Approval approval in randomApprovals)
            {
                approval.IsDeleted = false;
            }

            IQueryable<Approval> storageApprovals = randomApprovals;
            IQueryable<Approval> expectedApprovals = storageApprovals;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllApprovalsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageApprovals);

            // when
            IQueryable<Approval> actualApprovals =
                await this.approvalService.RetrieveAllApprovalsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualApprovals.Should().BeEquivalentTo(expectedApprovals);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldRetrieveAllNoApprovalsWhenCallerIsAnonymousAsync(
            SecurityContext invalidSecurityContext)
        {
            // given: workflow records are never public, so an anonymous caller sees
            // nothing at all — filtered to an empty set, never an error
            this.ambientSecurityContext = invalidSecurityContext;
            IQueryable<Approval> randomApprovals = CreateRandomApprovals();

            foreach (Approval approval in randomApprovals)
            {
                approval.IsDeleted = false;
            }

            IQueryable<Approval> storageApprovals = randomApprovals;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllApprovalsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageApprovals);

            // when
            IQueryable<Approval> actualApprovals =
                await this.approvalService.RetrieveAllApprovalsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualApprovals.Should().BeEmpty();

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRetrieveAllOwnApprovalsWhenUserHasNoReviewRoleAsync()
        {
            // given: "their own" means the approvals whose ENTITY this caller authored, and every
            // Approval.CreatedBy is pinned to the system sentinel — the value the workflow really
            // writes (§14.6.1). Anchoring the filter back on that column turns this red instead of
            // quietly returning nothing, which is the whole of the defect this replaced.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            string randomActorUserId = GetRandomString();

            Approval ownApproval = CreateRandomApproval();
            ownApproval.IsDeleted = false;
            ownApproval.CreatedBy = SystemIdentity.UserId;

            Approval othersApproval = CreateRandomApproval();
            othersApproval.IsDeleted = false;
            othersApproval.CreatedBy = SystemIdentity.UserId;

            Approval ownDeletedApproval = CreateRandomApproval();
            ownDeletedApproval.IsDeleted = true;
            ownDeletedApproval.CreatedBy = SystemIdentity.UserId;

            IQueryable<Approval> storageApprovals = new List<Approval>
            {
                ownApproval,
                othersApproval,
                ownDeletedApproval
            }.AsQueryable();

            IQueryable<Approval> expectedApprovals = new List<Approval>
            {
                ownApproval
            }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllApprovalsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageApprovals);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // what the service actually handed the broker, so the assertions below can prove the
            // soft-deleted row was dropped BEFORE the ownership question was asked
            IQueryable<Approval> delegatedApprovals = null;

            this.accessBrokerMock.Setup(broker =>
                broker.FilterApprovalsToEntityAuthorAsync(
                    It.IsAny<IQueryable<Approval>>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .Callback<IQueryable<Approval>, string, CancellationToken>(
                            (approvals, _, _) => delegatedApprovals = approvals)
                        .ReturnsAsync(expectedApprovals);

            // when
            IQueryable<Approval> actualApprovals =
                await this.approvalService.RetrieveAllApprovalsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualApprovals.Should().BeEquivalentTo(expectedApprovals);

            // the soft-deleted row never reaches the ownership question — a deleted approval is
            // not the caller's to see even when they wrote the entity underneath it
            delegatedApprovals.Should().NotBeNull();

            delegatedApprovals.Should().BeEquivalentTo(new List<Approval>
            {
                ownApproval,
                othersApproval
            });

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            // the ACTOR's id, and the caller's own token — an optional token is the kind of thing
            // that goes missing without the compiler noticing
            this.accessBrokerMock.Verify(broker =>
                broker.FilterApprovalsToEntityAuthorAsync(
                    It.IsAny<IQueryable<Approval>>(),
                    randomActorUserId,
                    TestContext.Current.CancellationToken),
                        Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ReviewRoles))]
        public async Task ShouldRetrieveAllNonDeletedApprovalsWhenUserHasReviewRoleAsync(
            string reviewRole)
        {
            // given: a review-role caller sees every non-deleted row — no user-id
            // resolution needed
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(reviewRole);

            Approval ownApproval = CreateRandomApproval();
            ownApproval.IsDeleted = false;

            Approval othersApproval = CreateRandomApproval();
            othersApproval.IsDeleted = false;

            Approval deletedApproval = CreateRandomApproval();
            deletedApproval.IsDeleted = true;

            IQueryable<Approval> storageApprovals = new List<Approval>
            {
                ownApproval,
                othersApproval,
                deletedApproval
            }.AsQueryable();

            IQueryable<Approval> expectedApprovals = new List<Approval>
            {
                ownApproval,
                othersApproval
            }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllApprovalsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageApprovals);

            // when
            IQueryable<Approval> actualApprovals =
                await this.approvalService.RetrieveAllApprovalsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualApprovals.Should().BeEquivalentTo(expectedApprovals);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
