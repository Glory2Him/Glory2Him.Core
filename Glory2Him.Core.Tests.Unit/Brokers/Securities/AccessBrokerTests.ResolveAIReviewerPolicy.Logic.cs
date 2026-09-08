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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ApprovalSettings;
using Glory2Him.Core.Models.Foundations.Approvals;
using Moq;
using Xunit;

namespace Glory2Him.Core.Tests.Unit.Brokers.Securities
{
    /// <summary>
    /// §8.6.2's feature switch, resolved on its OWN member rather than gathered with the reviewer
    /// scope. These tests state the two halves of that split: the answer itself, and the reads it
    /// does — and does not — pay for.
    ///
    /// <para>Only settled here. Every caller above mocks <c>IAccessBroker</c>, so the orchestration
    /// suite can prove what the AI-reviewer paths do with the verdict but never that the broker
    /// composes its policy key off the STORED approval's target.</para>
    /// </summary>
    public partial class AccessBrokerTests
    {
        /// <summary>
        /// The whole point of the member: the resolved <c>IsAIReviewerOffered</c> comes back, so
        /// the orchestration's fail-closed gate has something explicit to read. Arranged as
        /// <c>true</c> because the fixture defaults it <c>false</c> — a test proving the false
        /// case could pass on a member that never asked at all.
        /// </summary>
        [Fact]
        public async Task ShouldReportTheResolvedAIReviewerOfferAsync()
        {
            // given
            Guid approvalId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();

            Approval approval = CreateApproval(
                approvalId: approvalId,
                entityType: EntityType.ContentItem,
                entityId: entityId,
                approvalStatus: ApprovalStatus.Submitted);

            SetupApprovalById(approval);
            SetupEntityAuthor(EntityType.ContentItem, entityId, createdBy: "the-entity-owner");

            this.accessClientMock.Setup(client =>
                client.ResolveAIReviewerPolicyAsync(
                    It.IsAny<ResolveAIReviewerPolicyRequest>()))
                        .ReturnsAsync(new AIReviewerPolicyVerdict { IsOffered = true });

            // when
            AIReviewerPolicyVerdict actualVerdict =
                await this.accessBroker.ResolveAIReviewerPolicyByIdAsync(
                    approvalId: approvalId,
                    cancellationToken: default);

            // then
            actualVerdict.IsOffered.Should().BeTrue();
        }

        /// <summary>
        /// An id no approval carries answers <c>null</c> rather than a manufactured verdict. The
        /// distinction is the caller's whole fail-closed position (§8.4 rule 2): a
        /// <c>false</c> composed here would be indistinguishable from a switch somebody turned
        /// off, and the round it described could not be read in the first place.
        /// </summary>
        [Fact]
        public async Task ShouldAnswerNullWhenNoApprovalCarriesTheIdAsync()
        {
            // given: nothing stubs SelectApprovalByIdAsync for this id
            Guid unknownApprovalId = Guid.NewGuid();

            // when
            AIReviewerPolicyVerdict actualVerdict =
                await this.accessBroker.ResolveAIReviewerPolicyByIdAsync(
                    approvalId: unknownApprovalId,
                    cancellationToken: default);

            // then
            actualVerdict.Should().BeNull();

            // and the decision function was never troubled with a round that does not exist
            this.accessClientMock.Verify(client =>
                client.ResolveAIReviewerPolicyAsync(
                    It.IsAny<ResolveAIReviewerPolicyRequest>()),
                Times.Never);
        }

        /// <summary>
        /// The policy key is composed off the STORED approval and its stored entity, never off a
        /// caller's payload — the same discipline the §8.5 conditions beside it keep. Asserted on
        /// the request that crossed into the decision function, because that request IS the key.
        ///
        /// <para>The candidate list is asserted with it: §8.4's most-specific-wins can only be
        /// applied to tiers it was actually handed, so a gather that dropped the global row would
        /// resolve one tier too far and this member would answer the system default.</para>
        /// </summary>
        [Fact]
        public async Task ShouldKeyTheAIReviewerPolicyOnTheStoredApprovalsTargetAsync()
        {
            // given
            Guid approvalId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();
            ResolveAIReviewerPolicyRequest capturedRequest = null;

            Approval approval = CreateApproval(
                approvalId: approvalId,
                entityType: EntityType.ContentItem,
                entityId: entityId,
                approvalStatus: ApprovalStatus.Submitted);

            SetupApprovalById(approval);

            // The fixture's ContentItem arm stamps Testimony, which is what the narrow tier of
            // §8.4 keys on and what a scope-free resolution has to find for itself.
            SetupEntityAuthor(EntityType.ContentItem, entityId, createdBy: "the-entity-owner");

            var narrowSetting = new ApprovalSetting
            {
                Id = Guid.NewGuid(),
                EntityType = EntityType.ContentItem,
                ContentType = ContentType.Testimony,
                IsAIReviewerOffered = true,
                IsDeleted = false,
            };

            var globalSetting = new ApprovalSetting
            {
                Id = Guid.NewGuid(),
                EntityType = null,
                ContentType = null,
                IsDeleted = false,
            };

            var otherEntityTypeSetting = new ApprovalSetting
            {
                Id = Guid.NewGuid(),
                EntityType = EntityType.Tag,
                ContentType = null,
                IsDeleted = false,
            };

            SetupApprovalSettings(narrowSetting, globalSetting, otherEntityTypeSetting);

            this.accessClientMock.Setup(client =>
                client.ResolveAIReviewerPolicyAsync(
                    It.IsAny<ResolveAIReviewerPolicyRequest>()))
                        .Callback((ResolveAIReviewerPolicyRequest request) =>
                            capturedRequest = request)
                        .ReturnsAsync(new AIReviewerPolicyVerdict { IsOffered = true });

            // when
            await this.accessBroker.ResolveAIReviewerPolicyByIdAsync(
                approvalId: approvalId,
                cancellationToken: default);

            // then
            capturedRequest.EntityType.Should().Be("ContentItem");
            capturedRequest.ContentType.Should().Be("Testimony");

            // The Tag row is not a candidate for a ContentItem round; the global one is, because
            // it is the tier the entity-type default narrows.
            capturedRequest.CandidatePolicies.Should().HaveCount(2);

            capturedRequest.CandidatePolicies
                .Single(policy => policy.ContentType == "Testimony")
                .IsAIReviewerOffered.Should().BeTrue();

            capturedRequest.CandidatePolicies
                .Should().ContainSingle(policy => policy.EntityType == null);
        }

        /// <summary>
        /// Only <c>ContentItem</c> scopes its policies by content type. An association's key is its
        /// own type and personality, never an endpoint's (§8.4) — and an association HAS endpoint
        /// content types, so a resolution that reached for <c>roleSubjects[0]</c> unconditionally
        /// would find one and key the whole verdict on it.
        /// </summary>
        [Fact]
        public async Task ShouldNotCarryAnEndpointsContentTypeForANonContentItemRoundAsync()
        {
            // given
            Guid approvalId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();
            ResolveAIReviewerPolicyRequest capturedRequest = null;

            Approval approval = CreateApproval(
                approvalId: approvalId,
                entityType: EntityType.Association,
                entityId: entityId,
                approvalStatus: ApprovalStatus.Submitted);

            SetupApprovalById(approval);

            // The fixture's association arm gives endpoint A a content type of Testimony, which
            // is precisely the value that must NOT travel.
            SetupEntityAuthor(EntityType.Association, entityId, createdBy: "the-entity-owner");

            this.accessClientMock.Setup(client =>
                client.ResolveAIReviewerPolicyAsync(
                    It.IsAny<ResolveAIReviewerPolicyRequest>()))
                        .Callback((ResolveAIReviewerPolicyRequest request) =>
                            capturedRequest = request)
                        .ReturnsAsync(new AIReviewerPolicyVerdict { IsOffered = false });

            // when
            await this.accessBroker.ResolveAIReviewerPolicyByIdAsync(
                approvalId: approvalId,
                cancellationToken: default);

            // then
            capturedRequest.EntityType.Should().Be("Association");
            capturedRequest.ContentType.Should().BeNull();
        }

        /// <summary>
        /// WHAT THE SPLIT BOUGHT, stated as a cost rather than as prose. This member resolves one
        /// switch, so it reads the approval, the entity behind it and the settings — and none of
        /// the reviews, comments or invitation rows the reviewer-scope gather beside it collects.
        ///
        /// <para>The saving runs the other way too, and its own test sits on
        /// <c>RetrieveApprovalReviewerScopeByIdAsync</c>: that gather no longer scans
        /// <c>ApprovalSetting</c> at all.</para>
        /// </summary>
        [Fact]
        public async Task ShouldNotGatherTheRoundsReviewsWhenResolvingTheAIReviewerPolicyAsync()
        {
            // given
            Guid approvalId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();

            Approval approval = CreateApproval(
                approvalId: approvalId,
                entityType: EntityType.ContentItem,
                entityId: entityId,
                approvalStatus: ApprovalStatus.Submitted);

            SetupApprovalById(approval);
            SetupEntityAuthor(EntityType.ContentItem, entityId, createdBy: "the-entity-owner");

            // when
            await this.accessBroker.ResolveAIReviewerPolicyByIdAsync(
                approvalId: approvalId,
                cancellationToken: default);

            // then
            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalReviewsAsync(It.IsAny<CancellationToken>()),
                Times.Never);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalCommentsAsync(It.IsAny<CancellationToken>()),
                Times.Never);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalReviewRequestsAsync(It.IsAny<CancellationToken>()),
                Times.Never);

            // and the three reads it DOES make, so "cheap" cannot quietly become "did nothing"
            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalByIdAsync(approvalId, It.IsAny<CancellationToken>()),
                Times.Once);

            VerifyEntityAuthorRead(EntityType.ContentItem, entityId);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalSettingsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
        }
    }
}
