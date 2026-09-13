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
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Orchestrations.AIReviewers.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.AIReviewers
{
    /// <summary>
    /// §8.6.2.1's automatic assignment — a moderator opening a round that has entered review
    /// finds Berean already on it, with nobody having pressed anything.
    ///
    /// <para>Two subscriptions, <c>Approval-Added</c> and <c>Approval-Modified</c>, delegating to
    /// ONE private body: the gates, the write and the failure posture are literally the same
    /// code, so a rule cannot be fixed on one address and left broken on the other. What differs
    /// is the accepted event name alone.</para>
    ///
    /// <para>There is no caller to authorise here and that is the point — the identity on the
    /// inbound envelope belongs to whoever moved the round, usually an author with no review role
    /// at all. What binds instead is the ENVELOPE, verified before a single field is read from
    /// it.</para>
    /// </summary>
    public partial class AIReviewerOrchestrationServiceTests
    {
        /// <summary>
        /// Criterion 1, and criterion 7's whole assertion: the write goes through the WORKFLOW
        /// seam with this round's id, and never through the caller-facing foundation whose gate
        /// asks for a review-tier role the system identity does not hold.
        /// </summary>
        [Fact]
        public async Task ShouldAssignBereanWhenARoundOpensAlreadySubmittedAsync()
        {
            // given: a round created at Submitted under a policy that asks for Berean
            Guid approvalId = Guid.NewGuid();

            SetupAutomaticAIReviewerPolicy(approvalId, isAutomaticallyRequested: true);
            SetupAIReviewerEverAssigned(approvalId, isEverAssigned: false);
            SetupAutomaticAIReviewerAssignmentWrite();

            // when
            await this.aiReviewerOrchestrationService.OnApprovalAddedAsync(
                CreateApprovalFactEnvelope(approvalId, ApprovalStatus.Submitted),
                TestContext.Current.CancellationToken);

            // then: the seam, handed the act and no context — it mints the system identity for
            // itself, which is what makes the flag unforgeable by construction (#534)
            this.aiReviewerAssignmentWorkflowServiceMock.Verify(service =>
                service.AddAutomaticAIReviewerAssignmentAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // NOT the caller-facing door. Its gate asks for a tier nobody on this path holds, and
            // a round assigned through it would record the wrong author.
            this.aiReviewerAssignmentServiceMock.Verify(service =>
                service.AddAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// Criterion 2. The second subscription, covering both ways a round reaches
        /// <c>Submitted</c> after it opened — the submission of a round opened at <c>Draft</c>,
        /// and §8.6 HR-4's reset re-opening a decided one. One address hears every route,
        /// because all of them write through <c>ModifyApprovalAsync</c>.
        /// </summary>
        [Fact]
        public async Task ShouldAssignBereanWhenARoundReachesSubmittedLaterAsync()
        {
            // given
            Guid approvalId = Guid.NewGuid();

            SetupAutomaticAIReviewerPolicy(approvalId, isAutomaticallyRequested: true);
            SetupAIReviewerEverAssigned(approvalId, isEverAssigned: false);
            SetupAutomaticAIReviewerAssignmentWrite();

            // when
            await this.aiReviewerOrchestrationService.OnApprovalModifiedAsync(
                CreateApprovalFactEnvelope(approvalId, ApprovalStatus.Submitted),
                TestContext.Current.CancellationToken);

            // then
            this.aiReviewerAssignmentWorkflowServiceMock.Verify(service =>
                service.AddAutomaticAIReviewerAssignmentAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.aiReviewerAssignmentServiceMock.Verify(service =>
                service.AddAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// Gate 1, and criterion 12's silent failure made loud. The event name is bound INTO the
        /// HMAC, so a handler expecting the wrong one refuses a genuine envelope it was correctly
        /// delivered — no misroute, no error, just a reaction that stops happening. The DIRECTION
        /// is the same trap: <c>EventBroker</c> signs the publish leg as <c>Request</c>, so a
        /// receiver asking for <c>Reply</c> would fail every verification with nothing to show
        /// for it.
        ///
        /// <para>The literals are what every existing subscriber writes — <c>EventBroker</c>
        /// composes the name at publish time and exposes that composition to nobody (#286).</para>
        /// </summary>
        [Theory]
        [InlineData(AddedOperation, "ApprovalAdded")]
        [InlineData(ModifiedOperation, "ApprovalModified")]
        public async Task ShouldVerifyEachApprovalFactEnvelopeUnderItsPublishedNameAsync(
            string approvalEventOperation,
            string expectedSignedEventName)
        {
            // given
            Guid approvalId = Guid.NewGuid();

            SetupAutomaticAIReviewerPolicy(approvalId, isAutomaticallyRequested: true);
            SetupAIReviewerEverAssigned(approvalId, isEverAssigned: false);
            SetupAutomaticAIReviewerAssignmentWrite();

            // when
            await DeliverApprovalFactAsync(
                approvalEventOperation,
                CreateApprovalFactEnvelope(approvalId, ApprovalStatus.Submitted),
                TestContext.Current.CancellationToken);

            // then
            this.envelopeIntegrityBrokerMock.Verify(broker =>
                broker.VerifyAsync(
                    It.IsAny<EventEnvelope<Approval>>(),
                    expectedSignedEventName,
                    EnvelopeDirection.Request),
                Times.Once);
        }

        /// <summary>
        /// Gate 1's refusal. Every gate after it reads the envelope's own signed content, so an
        /// unverifiable envelope must be refused before a single field of it is trusted —
        /// otherwise the status gate is reading whatever the sender felt like writing.
        /// </summary>
        [Theory]
        [InlineData(AddedOperation)]
        [InlineData(ModifiedOperation)]
        public async Task ShouldNotAssignBereanWhenTheEnvelopeFailsVerificationAsync(
            string approvalEventOperation)
        {
            // given: an otherwise perfect delivery — every other gate would pass
            Guid approvalId = Guid.NewGuid();

            SetupAutomaticAIReviewerPolicy(approvalId, isAutomaticallyRequested: true);
            SetupAIReviewerEverAssigned(approvalId, isEverAssigned: false);
            SetupAutomaticAIReviewerAssignmentWrite();

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    It.IsAny<EventEnvelope<It.IsAnyType>>(),
                    It.IsAny<string>(),
                    It.IsAny<EnvelopeDirection>()))
                        .ReturnsAsync(false);

            // when
            await Assert.ThrowsAnyAsync<Exception>(() =>
                DeliverApprovalFactAsync(
                    approvalEventOperation,
                    CreateApprovalFactEnvelope(approvalId, ApprovalStatus.Submitted),
                    TestContext.Current.CancellationToken).AsTask());

            // then: no further BROKER CALL and no write, which is what "the first gate that
            // refuses ends the delivery" means
            this.accessBrokerMock.Verify(broker =>
                broker.ResolveAIReviewerPolicyByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.accessBrokerMock.Verify(broker =>
                broker.IsAIReviewerEverAssignedAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            VerifyNoAutomaticAssignmentWasMade();
        }

        /// <summary>
        /// Gate 2. A soft-deleted round gets no reviewer, and the flag is read off the SIGNED
        /// content rather than re-read from storage — it is the row the foundation itself
        /// published.
        /// </summary>
        [Theory]
        [InlineData(AddedOperation)]
        [InlineData(ModifiedOperation)]
        public async Task ShouldNotAssignBereanWhenTheRoundIsSoftDeletedAsync(
            string approvalEventOperation)
        {
            // given: a round that is Submitted and offered, and removed
            Guid approvalId = Guid.NewGuid();

            SetupAutomaticAIReviewerPolicy(approvalId, isAutomaticallyRequested: true);
            SetupAIReviewerEverAssigned(approvalId, isEverAssigned: false);
            SetupAutomaticAIReviewerAssignmentWrite();

            // when
            await DeliverApprovalFactAsync(
                approvalEventOperation,
                CreateApprovalFactEnvelope(
                    approvalId, ApprovalStatus.Submitted, isDeleted: true),
                TestContext.Current.CancellationToken);

            // then: no verdict read, no presence read, no write
            this.accessBrokerMock.Verify(broker =>
                broker.ResolveAIReviewerPolicyByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            VerifyNoAutomaticAssignmentWasMade();
        }

        /// <summary>
        /// Gate 3, and EVERY status the gate must refuse rather than a sample of them. Three
        /// separate cases sit behind one gate:
        ///
        /// <list type="bullet">
        /// <item><c>Draft</c> is excluded HERE and not by a rule of its own (§9.2) — a round
        /// opened at Draft has not entered review, and Berean must not read content its author
        /// has not offered. It is picked up later by the <c>-Modified</c> its submission
        /// publishes.</item>
        /// <item><c>Approved</c> and <c>Rejected</c> are excluded by the same gate for §7.9 rule
        /// 7's reason: an assignment on a closed round could never be answered.</item>
        /// <item><c>Dismissed</c> is reachable and would otherwise be the one value of the enum
        /// nothing here exercises.</item>
        /// </list>
        /// </summary>
        [Theory]
        [InlineData(AddedOperation, ApprovalStatus.Draft)]
        [InlineData(AddedOperation, ApprovalStatus.Approved)]
        [InlineData(AddedOperation, ApprovalStatus.Rejected)]
        [InlineData(AddedOperation, ApprovalStatus.Dismissed)]
        [InlineData(ModifiedOperation, ApprovalStatus.Draft)]
        [InlineData(ModifiedOperation, ApprovalStatus.Approved)]
        [InlineData(ModifiedOperation, ApprovalStatus.Rejected)]
        [InlineData(ModifiedOperation, ApprovalStatus.Dismissed)]
        public async Task ShouldNotAssignBereanWhileTheRoundIsNotSubmittedAsync(
            string approvalEventOperation,
            ApprovalStatus refusedStatus)
        {
            // given
            Guid approvalId = Guid.NewGuid();

            SetupAutomaticAIReviewerPolicy(approvalId, isAutomaticallyRequested: true);
            SetupAIReviewerEverAssigned(approvalId, isEverAssigned: false);
            SetupAutomaticAIReviewerAssignmentWrite();

            // when
            await DeliverApprovalFactAsync(
                approvalEventOperation,
                CreateApprovalFactEnvelope(approvalId, refusedStatus),
                TestContext.Current.CancellationToken);

            // then: the gate runs before the broker is reached, so a fact about a round that has
            // not entered review costs one comparison and no round trip
            this.accessBrokerMock.Verify(broker =>
                broker.ResolveAIReviewerPolicyByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            VerifyNoAutomaticAssignmentWasMade();
        }

        /// <summary>
        /// Gate 4, in its two separate cases. A verdict that says <c>false</c> refuses — the
        /// realistic tier that offers Berean and leaves the asking to a person — and a round the
        /// broker cannot resolve at all answers <c>null</c>, which COLLAPSES to false exactly as
        /// the manual offer already treats it (§8.4 rule 2). A policy nobody could read is not a
        /// permission.
        ///
        /// <para>The handler reads the ONE composed field. §8.6.1 rule 4 keeps the composition
        /// — <c>IsAIReviewerOffered &amp;&amp; IsAIReviewerAutomaticallyRequested</c> — inside
        /// the decision function and nowhere else, which is also why this service holds no
        /// <c>IApprovalSettingService</c> to re-derive it from.</para>
        /// </summary>
        [Theory]
        [InlineData(AddedOperation, true)]
        [InlineData(AddedOperation, false)]
        [InlineData(ModifiedOperation, true)]
        [InlineData(ModifiedOperation, false)]
        public async Task ShouldNotAssignBereanWhenTheResolvedPolicyDoesNotAskForItAsync(
            string approvalEventOperation,
            bool isPolicyResolvable)
        {
            // given
            Guid approvalId = Guid.NewGuid();

            if (isPolicyResolvable)
            {
                SetupAutomaticAIReviewerPolicy(approvalId, isAutomaticallyRequested: false);
            }
            else
            {
                SetupUnresolvableAIReviewerPolicy(approvalId);
            }

            SetupAIReviewerEverAssigned(approvalId, isEverAssigned: false);
            SetupAutomaticAIReviewerAssignmentWrite();

            // when
            await DeliverApprovalFactAsync(
                approvalEventOperation,
                CreateApprovalFactEnvelope(approvalId, ApprovalStatus.Submitted),
                TestContext.Current.CancellationToken);

            // then: read for THIS round, and refused before the presence check costs anything
            this.accessBrokerMock.Verify(broker =>
                broker.ResolveAIReviewerPolicyByIdAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.accessBrokerMock.Verify(broker =>
                broker.IsAIReviewerEverAssignedAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            VerifyNoAutomaticAssignmentWasMade();
        }

        /// <summary>
        /// §8.6.2.1's gate 4 — the SUBJECT is visible. A takedown leaves the approval record and
        /// the entity's denormalised <c>ApprovalStatus</c> alone (§9.7.6), so a taken-down row
        /// still looks open to every status-shaped gate above: the round's own
        /// <c>IsDeleted</c> is false, its status still says <c>Submitted</c>, and the policy
        /// still resolves. Without this gate a <c>-Modified</c> on such a round would set an AI
        /// pass running over content nobody may see.
        ///
        /// <para>Keyed on the envelope's signed <c>EntityType</c> and <c>EntityId</c>, which is
        /// the same probe §16.7.2's repair and §12.5.4 business rule 2 hold themselves to.</para>
        /// </summary>
        [Theory]
        [InlineData(AddedOperation)]
        [InlineData(ModifiedOperation)]
        public async Task ShouldNotAssignBereanWhenTheSubjectIsNoLongerVisibleAsync(
            string approvalEventOperation)
        {
            // given: an open, offered round whose entity has been taken down
            Guid approvalId = Guid.NewGuid();

            SetupAutomaticAIReviewerPolicy(approvalId, isAutomaticallyRequested: true);
            SetupAIReviewerEverAssigned(approvalId, isEverAssigned: false);
            SetupAutomaticAIReviewerAssignmentWrite();
            SetupEntityVisibility(isEntityVisible: false);

            EventEnvelope<Approval> envelope =
                CreateApprovalFactEnvelope(approvalId, ApprovalStatus.Submitted);

            // when
            await DeliverApprovalFactAsync(
                approvalEventOperation,
                envelope,
                TestContext.Current.CancellationToken);

            // then: probed on the SIGNED key, and refused before the presence check
            this.accessBrokerMock.Verify(broker =>
                broker.IsEntityVisibleAsync(
                    envelope.Content.EntityType,
                    envelope.Content.EntityId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.accessBrokerMock.Verify(broker =>
                broker.IsAIReviewerEverAssignedAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            VerifyNoAutomaticAssignmentWasMade();
        }

        /// <summary>
        /// Criterion 4, and it is named for the WITHDRAWAL rather than for the gate on purpose.
        /// A moderator takes Berean off a round, the round is modified again, and nothing is
        /// written — however many times that round is edited afterwards.
        ///
        /// <para><b>This is the criterion most likely to be "fixed" later by somebody reading
        /// gate 5 as over-strict. It is not.</b> <c>WithdrawAIReviewerAsync</c> soft-deletes the
        /// row, so a gate that looked only at live rows would put Berean straight back on the
        /// next <c>Approval-Modified</c>, in a loop the moderator cannot win. A withdrawal is a
        /// decision; asking Berean again after one stays their explicit act, and
        /// <c>POST api/AIReviewers/...</c> is theirs to press.</para>
        ///
        /// <para>The same gate is what carries TERMINATION and what answers REDELIVERY. Once any
        /// row exists the handler is a no-op for that round forever, so no cycle can be sustained
        /// through it however many hops arrive — which is strictly more than a
        /// <c>ProcessedEvent</c> row would give, that being keyed on <c>EventId</c>: two
        /// DIFFERENT <c>Approval-Modified</c> facts about one round would each pass it, and this
        /// gate stops both.</para>
        ///
        /// <para>That the unfiltered read really does answer <c>true</c> for a withdrawn row is
        /// settled where the predicate lives —
        /// <c>AccessBrokerTests.IsAIReviewerEverAssigned.Logic</c> and the integration test over
        /// a real catalogue. Here it is a mock, so what this pins is that the handler asks the
        /// UNFILTERED question about THIS round and stands down on a yes.</para>
        /// </summary>
        [Theory]
        [InlineData(AddedOperation)]
        [InlineData(ModifiedOperation)]
        public async Task ShouldNotAssignBereanAfterAModeratorHasWithdrawnItAsync(
            string approvalEventOperation)
        {
            // given: an open, offered, visible round that has had an assignment on it before
            Guid approvalId = Guid.NewGuid();

            SetupAutomaticAIReviewerPolicy(approvalId, isAutomaticallyRequested: true);
            SetupAIReviewerEverAssigned(approvalId, isEverAssigned: true);
            SetupAutomaticAIReviewerAssignmentWrite();

            // when
            await DeliverApprovalFactAsync(
                approvalEventOperation,
                CreateApprovalFactEnvelope(approvalId, ApprovalStatus.Submitted),
                TestContext.Current.CancellationToken);

            // then: asked about THIS round, and nothing written
            this.accessBrokerMock.Verify(broker =>
                broker.IsAIReviewerEverAssignedAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            VerifyNoAutomaticAssignmentWasMade();
        }

        /// <summary>
        /// Criterion 6, first half. <c>ValidateUserMayRequestAIReviewer</c> is deliberately NOT
        /// called here, and its absence is a ruling rather than an omission: there is no caller
        /// whose tier it could ask about. The identity on the inbound envelope belongs to whoever
        /// moved the round — ordinarily the AUTHOR revising their own submission, who holds no
        /// review role at all (HR-1 forbids reviewing your own content) — and a gate that has to
        /// be handed a forged context to pass is not a gate.
        /// </summary>
        [Theory]
        [InlineData(AddedOperation)]
        [InlineData(ModifiedOperation)]
        public async Task ShouldAssignBereanWhenTheFactCarriesAnAuthorWithNoReviewRoleAsync(
            string approvalEventOperation)
        {
            // given
            Guid approvalId = Guid.NewGuid();

            SetupAutomaticAIReviewerPolicy(approvalId, isAutomaticallyRequested: true);
            SetupAIReviewerEverAssigned(approvalId, isEverAssigned: false);
            SetupAutomaticAIReviewerAssignmentWrite();

            // when
            await DeliverApprovalFactAsync(
                approvalEventOperation,
                CreateApprovalFactEnvelope(
                    approvalId,
                    ApprovalStatus.Submitted,
                    securityContext: CreateAuthenticatedSecurityContext()),
                TestContext.Current.CancellationToken);

            // then
            this.aiReviewerAssignmentWorkflowServiceMock.Verify(service =>
                service.AddAutomaticAIReviewerAssignmentAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        /// <summary>
        /// Criterion 6, second half. Unlike <c>ApprovalOrchestrationService</c>'s entity
        /// handlers, this one does NOT stand down on
        /// <c>envelope.SecurityContext.IsSystemIdentity</c>. Those handlers bind facts that
        /// describe something a PERSON did; a round reaches <c>Submitted</c> through the
        /// workflow's own write, so the same guard here would mean this never fires at all.
        ///
        /// <para>Termination is carried by gate 5 instead, which is strictly stronger: an
        /// identity test bounds one hop, that gate bounds the round forever.</para>
        /// </summary>
        [Theory]
        [InlineData(AddedOperation)]
        [InlineData(ModifiedOperation)]
        public async Task ShouldAssignBereanWhenTheFactCarriesTheSystemIdentityAsync(
            string approvalEventOperation)
        {
            // given
            Guid approvalId = Guid.NewGuid();

            SetupAutomaticAIReviewerPolicy(approvalId, isAutomaticallyRequested: true);
            SetupAIReviewerEverAssigned(approvalId, isEverAssigned: false);
            SetupAutomaticAIReviewerAssignmentWrite();

            // when
            await DeliverApprovalFactAsync(
                approvalEventOperation,
                CreateApprovalFactEnvelope(
                    approvalId,
                    ApprovalStatus.Submitted,
                    securityContext: new SecurityContext { IsSystemIdentity = true }),
                TestContext.Current.CancellationToken);

            // then
            this.aiReviewerAssignmentWorkflowServiceMock.Verify(service =>
                service.AddAutomaticAIReviewerAssignmentAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        /// <summary>
        /// GATE 1's first clause. A null envelope is not an empty fact but a broken delivery —
        /// there is nothing to verify, nothing to gate on, and no content id to read.
        ///
        /// <para>It was refused before this guard existed only by ACCIDENT:
        /// <c>EnvelopeIntegrityBroker</c> reads <c>envelope?.Integrity</c>, so verification
        /// answered false for it. That is the broker's null tolerance standing in for the
        /// receiver's own gate, and the moment the clause is trimmed the validator dereferences
        /// the null itself — a broken delivery then surfaces as a service exception telling the
        /// operator to contact support. #545 criterion 1 puts the refusal here, where the two
        /// sibling receivers put theirs.</para>
        /// </summary>
        [Theory]
        [InlineData(AddedOperation)]
        [InlineData(ModifiedOperation)]
        public async Task ShouldRefuseANullApprovalFactEnvelopeAsync(
            string approvalEventOperation)
        {
            // given: no delivery at all
            EventEnvelope<Approval> nullEnvelope = null;

            // when
            AIReviewerOrchestrationValidationException
                actualAIReviewerOrchestrationValidationException =
                    await Assert.ThrowsAsync<AIReviewerOrchestrationValidationException>(() =>
                        DeliverApprovalFactAsync(
                            approvalEventOperation,
                            nullEnvelope,
                            TestContext.Current.CancellationToken).AsTask());

            // then: the validation family, not the service family a dereferenced null produces
            actualAIReviewerOrchestrationValidationException.InnerException
                .Should().BeOfType<InvalidAIReviewerOrchestrationException>();

            // and refused ahead of the gates, so no broker was asked anything
            this.accessBrokerMock.Verify(broker =>
                broker.ResolveAIReviewerPolicyByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.accessBrokerMock.Verify(broker =>
                broker.IsAIReviewerEverAssignedAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            VerifyNoAutomaticAssignmentWasMade();
        }

        /// <summary>
        /// GATE 1's other half. Criterion 3 names ApprovalOrchestrationService's verifier as the
        /// pattern to follow, and that pattern opens by refusing a null Content or Metadata
        /// BEFORE it verifies anything. Without it a signed envelope carrying no content passes
        /// verification and then dereferences into the gates, so a malformed fact surfaces as a
        /// service exception telling the operator to contact support rather than as the
        /// validation refusal both sibling receivers produce for the same input.
        ///
        /// Not reachable from our own publisher today, which is why it is a mis-categorisation
        /// rather than a hole — but a receiver is reachable without going through the broker, and
        /// that is the whole reason §14.6 rule 4 puts verification here in the first place.
        /// </summary>
        [Theory]
        [InlineData(AddedOperation)]
        [InlineData(ModifiedOperation)]
        public async Task ShouldRefuseAnApprovalFactEnvelopeWithNoContentAsync(
            string approvalEventOperation)
        {
            // given: an envelope that VERIFIES — the integrity broker is left saying true — and
            // still carries nothing to gate on
            var contentlessEnvelope = new EventEnvelope<Approval>
            {
                Content = null,
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            // when
            AIReviewerOrchestrationValidationException
                actualAIReviewerOrchestrationValidationException =
                    await Assert.ThrowsAsync<AIReviewerOrchestrationValidationException>(() =>
                        DeliverApprovalFactAsync(
                            approvalEventOperation,
                            contentlessEnvelope,
                            TestContext.Current.CancellationToken).AsTask());

            // then: the validation family, not the service family
            actualAIReviewerOrchestrationValidationException.InnerException
                .Should().BeOfType<InvalidAIReviewerOrchestrationException>();

            // and refused ahead of the gates, so no broker was asked anything
            this.accessBrokerMock.Verify(broker =>
                broker.ResolveAIReviewerPolicyByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.accessBrokerMock.Verify(broker =>
                broker.IsAIReviewerEverAssignedAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            VerifyNoAutomaticAssignmentWasMade();
        }

        // Both handlers, driven through one switch so every GATE below can be a theory over the
        // pair rather than a test written twice. That is criterion 2's "one private body" made
        // observable: the two differ only in the accepted event name, so a rule fixed on one
        // address and left broken on the other fails here rather than shipping.
        private ValueTask<EventEnvelope<Approval>?> DeliverApprovalFactAsync(
            string approvalEventOperation,
            EventEnvelope<Approval> envelope,
            CancellationToken cancellationToken) =>
            approvalEventOperation switch
            {
                AddedOperation =>
                    this.aiReviewerOrchestrationService.OnApprovalAddedAsync(
                        envelope, cancellationToken),

                ModifiedOperation =>
                    this.aiReviewerOrchestrationService.OnApprovalModifiedAsync(
                        envelope, cancellationToken),

                _ => throw new ArgumentOutOfRangeException(
                    nameof(approvalEventOperation),
                    approvalEventOperation,
                    "This service subscribes to no other Approval fact address."),
            };

        // Repeated at the end of every refusal below, because "the gate refused" and "the write
        // did not happen" are two claims and only the second is the one that matters.
        private void VerifyNoAutomaticAssignmentWasMade() =>
            this.aiReviewerAssignmentWorkflowServiceMock.Verify(service =>
                service.AddAutomaticAIReviewerAssignmentAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

        private const string AddedOperation = "Added";
        private const string ModifiedOperation = "Modified";

        // The signed fact the foundation publishes about a round. Every gate but the verdict and
        // the presence check reads this envelope and nothing else — it is inside the HMAC, which
        // is what makes a status gate a gate rather than a reading of whatever the sender felt
        // like writing.
        private static EventEnvelope<Approval> CreateApprovalFactEnvelope(
            Guid approvalId,
            ApprovalStatus approvalStatus,
            bool isDeleted = false,
            SecurityContext securityContext = null) =>
            new EventEnvelope<Approval>
            {
                Content = new Approval
                {
                    Id = approvalId,
                    EntityType = EntityType.Tag,
                    EntityId = Guid.NewGuid(),
                    ApprovalStatus = approvalStatus,
                    IsDeleted = isDeleted,
                },

                SecurityContext = securityContext
                    ?? new SecurityContext { IsAuthenticated = true },

                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

        // The ONE composed field §8.6.1 rule 4 allows this path to read. IsOffered is OFFERED
        // throughout, so the refusal a test arranges is the realistic one — a tier that offers
        // Berean and leaves the asking to a person — rather than a state the decision function
        // could not produce: it composes IsAutomaticallyRequested as
        // IsOffered && IsAIReviewerAutomaticallyRequested, so a verdict answering true here has
        // already answered the offer.
        //
        // Keyed on the approval id rather than It.IsAny, so a test cannot pass by answering a
        // question about a different round.
        private void SetupAutomaticAIReviewerPolicy(
            Guid approvalId,
            bool isAutomaticallyRequested) =>
            this.accessBrokerMock.Setup(broker =>
                broker.ResolveAIReviewerPolicyByIdAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new AIReviewerPolicyVerdict
                        {
                            IsOffered = true,
                            IsAutomaticallyRequested = isAutomaticallyRequested,
                        });

        // A round the broker cannot resolve a policy for. Answered as null rather than as a
        // manufactured false, so the handler is the thing that has to fail closed.
        private void SetupUnresolvableAIReviewerPolicy(Guid approvalId) =>
            this.accessBrokerMock.Setup(broker =>
                broker.ResolveAIReviewerPolicyByIdAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((AIReviewerPolicyVerdict)null);

        private void SetupAIReviewerEverAssigned(Guid approvalId, bool isEverAssigned) =>
            this.accessBrokerMock.Setup(broker =>
                broker.IsAIReviewerEverAssignedAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(isEverAssigned);

        // The seam echoes back the row it wrote, so a test can assert on the argument and on what
        // came back and know they are the same round.
        private void SetupAutomaticAIReviewerAssignmentWrite() =>
            this.aiReviewerAssignmentWorkflowServiceMock.Setup(service =>
                service.AddAutomaticAIReviewerAssignmentAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Guid approvalId, CancellationToken _) =>
                            new AIReviewerAssignment
                            {
                                Id = Guid.NewGuid(),
                                ApprovalId = approvalId,
                                IsAIReviewCompleted = false,
                                IsAIReviewCommentsPresent = false,
                            });
    }
}
