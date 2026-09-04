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
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Orchestrations.Approvals;
using Glory2Him.Core.Models.Orchestrations.Approvals.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Approvals
{
    public partial class ApprovalOrchestrationServiceTests
    {
        // Every caller the tier gate must turn away before it looks at roles at all. The third
        // case is the one that matters: it carries Administrators while UNAUTHENTICATED, so a gate that
        // read only the role list — and took the presence of a name as proof of a signed-in
        // caller — would admit an anonymous request carrying a forged claim.
        public static TheoryData<SecurityContext> UnauthenticatedSecurityContexts() =>
            new TheoryData<SecurityContext>
            {
                null,
                new SecurityContext { IsAuthenticated = false, Roles = Array.Empty<string>() },
                new SecurityContext { IsAuthenticated = false, Roles = new[] { Roles.Administrators } },
            };

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveVerdictIfEntityIdIsInvalidAndLogItAsync()
        {
            // given: an empty id names no row, so the (EntityType, EntityId) pair the unfiltered
            // probe keys on cannot be occupied by anything. The shape check runs before the
            // envelope is created, which is what keeps a malformed request off the storage path
            // entirely rather than turning it into a not-found a moderator has to interpret.
            EntityType inputEntityType = EntityType.Tag;
            var invalidEntityId = Guid.Empty;

            var invalidApprovalOrchestrationException =
                new InvalidApprovalOrchestrationException(
                    message: "Approval is invalid, fix the errors and try again.");

            invalidApprovalOrchestrationException.UpsertDataList(
                key: nameof(Approval.EntityId),
                value: "Id is required");

            var expectedValidationException =
                new ApprovalOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidApprovalOrchestrationException);

            // when
            ValueTask<ApprovalVerdict> retrieveVerdictTask =
                this.approvalOrchestrationService.RetrieveApprovalVerdictAsync(
                    inputEntityType,
                    invalidEntityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    retrieveVerdictTask.AsTask);

            // then: nothing was read at all — not the caller, not the approval. The envelope
            // broker is silent too, which is the proof the check precedes it.
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.approvalServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveVerdictIfEntityTypeIsUndefinedAndLogItAsync()
        {
            // given: an integer outside the enum. EntityType is the other half of the key AND
            // what the not-found message names, so an unrecognized member is refused rather than
            // probed for — no stored row can carry it, and letting it through would produce a
            // not-found sentence naming a type that does not exist.
            var undefinedEntityType = (EntityType)97;
            Guid inputEntityId = Guid.NewGuid();

            var invalidApprovalOrchestrationException =
                new InvalidApprovalOrchestrationException(
                    message: "Approval is invalid, fix the errors and try again.");

            invalidApprovalOrchestrationException.UpsertDataList(
                key: nameof(Approval.EntityType),
                value: "Value is not a recognized entity type");

            var expectedValidationException =
                new ApprovalOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidApprovalOrchestrationException);

            // when
            ValueTask<ApprovalVerdict> retrieveVerdictTask =
                this.approvalOrchestrationService.RetrieveApprovalVerdictAsync(
                    undefinedEntityType,
                    inputEntityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    retrieveVerdictTask.AsTask);

            // then: the id was perfectly good, so only the type is reported — a validation that
            // named both would be reporting a failure that did not happen.
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.approvalServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReportEveryInvalidVerdictInputInOnePassAndLogItAsync()
        {
            // given: BOTH halves of the key are malformed. Both are reported, for the same
            // reason the verdict itself returns every block reason rather than the first
            // (§16.7.2) — a caller told only about the type fixes it, retries, and only then
            // learns about the id it could have corrected in the same visit.
            var undefinedEntityType = (EntityType)97;
            var invalidEntityId = Guid.Empty;

            var invalidApprovalOrchestrationException =
                new InvalidApprovalOrchestrationException(
                    message: "Approval is invalid, fix the errors and try again.");

            invalidApprovalOrchestrationException.UpsertDataList(
                key: nameof(Approval.EntityType),
                value: "Value is not a recognized entity type");

            invalidApprovalOrchestrationException.UpsertDataList(
                key: nameof(Approval.EntityId),
                value: "Id is required");

            var expectedValidationException =
                new ApprovalOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidApprovalOrchestrationException);

            // when
            ValueTask<ApprovalVerdict> retrieveVerdictTask =
                this.approvalOrchestrationService.RetrieveApprovalVerdictAsync(
                    undefinedEntityType,
                    invalidEntityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    retrieveVerdictTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.approvalServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowValidationExceptionOnRetrieveVerdictIfUserIsNotAuthenticatedAndLogItAsync(
            SecurityContext unauthenticatedSecurityContext)
        {
            // given: the verdict names resolved policy — the threshold, which block fired, how
            // far off the count is — so it is the moderation view rather than a public one
            // (§16.7.2). An anonymous caller is refused before the approval is looked up, so the
            // refusal cannot double as an existence oracle for the key (§14.5).
            this.ambientSecurityContext = unauthenticatedSecurityContext;

            EntityType inputEntityType = EntityType.ContentItem;
            Guid inputEntityId = Guid.NewGuid();

            var unauthorizedApprovalOrchestrationException =
                new UnauthorizedApprovalOrchestrationException(
                    message: "The current user is not authenticated.");

            var expectedValidationException =
                new ApprovalOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: unauthorizedApprovalOrchestrationException);

            // when
            ValueTask<ApprovalVerdict> retrieveVerdictTask =
                this.approvalOrchestrationService.RetrieveApprovalVerdictAsync(
                    inputEntityType,
                    inputEntityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    retrieveVerdictTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.approvalServiceMock.Verify(service =>
                service.FindApprovalByEntityAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.approvalServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(NonModerationRoleSets))]
        public async Task ShouldThrowValidationExceptionOnRetrieveVerdictIfCallerIsOutsideTheModerationTierAndLogItAsync(
            string[] nonModerationRoles)
        {
            // given: a signed-in caller with no review standing. Being authenticated is not the
            // qualification — the verdict discloses resolved policy, so it is addressed to the
            // party the policy is FOR (§16.7.2), which excludes a contributor asking about their
            // own submission. The refusal must land before the read for the same reason as the
            // anonymous one: an authorized-then-not-found pair of answers would let an
            // unprivileged caller enumerate which keys carry approvals (§14.5).
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(nonModerationRoles);

            EntityType inputEntityType = EntityType.ContentItem;
            Guid inputEntityId = Guid.NewGuid();

            var unauthorizedApprovalOrchestrationException =
                new UnauthorizedApprovalOrchestrationException(
                    message: "The current user is not allowed to view this approval verdict.");

            var expectedValidationException =
                new ApprovalOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: unauthorizedApprovalOrchestrationException);

            // when
            ValueTask<ApprovalVerdict> retrieveVerdictTask =
                this.approvalOrchestrationService.RetrieveApprovalVerdictAsync(
                    inputEntityType,
                    inputEntityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    retrieveVerdictTask.AsTask);

            // then: the gate runs BEFORE any read — the probe was never issued
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.approvalServiceMock.Verify(service =>
                service.FindApprovalByEntityAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.approvalServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData("Publisher-Backup")]
        [InlineData("Reviewer-Trainee")]
        [InlineData("ContentItem-publishers")]
        public async Task ShouldThrowValidationExceptionOnRetrieveVerdictIfARoleOnlyResemblesAModerationRoleAndLogItAsync(
            string nearMissRole)
        {
            // given: names that a substring or case-insensitive match would wave through. The
            // capability segment is plural and always LAST (§18.6) — `ContentItem-Publishers`,
            // never `Publisher-Backup` — and the suffix test is Ordinal, so `-publishers` is a
            // different role name, not the same one spelled loosely. That third case differs
            // from the real role by CASE ALONE, which is what makes it the Ordinal check's
            // witness rather than just another near-miss. A gate that matched
            // otherwise would hand the moderation view to whoever could get a role minted with
            // the right word somewhere in it.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(nearMissRole);

            EntityType inputEntityType = EntityType.ContentItem;
            Guid inputEntityId = Guid.NewGuid();

            var unauthorizedApprovalOrchestrationException =
                new UnauthorizedApprovalOrchestrationException(
                    message: "The current user is not allowed to view this approval verdict.");

            var expectedValidationException =
                new ApprovalOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: unauthorizedApprovalOrchestrationException);

            // when
            ValueTask<ApprovalVerdict> retrieveVerdictTask =
                this.approvalOrchestrationService.RetrieveApprovalVerdictAsync(
                    inputEntityType,
                    inputEntityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    retrieveVerdictTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.approvalServiceMock.Verify(service =>
                service.FindApprovalByEntityAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.approvalServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(Roles.Administrators)]
        [InlineData(Roles.Publishers)]
        [InlineData(Roles.Reviewers)]
        [InlineData(Roles.ContentItemPublishers)]
        [InlineData(Roles.TagReviewers)]
        [InlineData("ContentItem-Story-Reviewers")]
        public async Task ShouldAdmitEveryModerationRoleToTheVerdictReadAsync(string admittedRole)
        {
            // given: the four global names plus the granular `%EntityType%-*` and
            // `%EntityType%-%ContentType%-*` forms, which are admitted by their trailing
            // capability segment rather than by being enumerated (§18.6) — otherwise every new
            // scoped role minted would have to be added here to become visible.
            //
            // The entity type is deliberately NOT the enum's zero member and the id is a fresh
            // one, so a probe issued with defaulted or hard-coded arguments would be visible.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(admittedRole);

            EntityType inputEntityType = EntityType.Link;
            Guid inputEntityId = Guid.NewGuid();
            var storedApprovalId = Guid.NewGuid();

            SetupApprovalProbe(CreateApprovalMatch(approvalId: storedApprovalId));
            SetupConditions(CreateMetConditions());

            SetupAccessDecisions(
                decisionVerdict: PermittedVerdict(),
                bypassVerdict: PermittedVerdict());

            // when
            ApprovalVerdict actualVerdict =
                await this.approvalOrchestrationService.RetrieveApprovalVerdictAsync(
                    inputEntityType,
                    inputEntityId,
                    TestContext.Current.CancellationToken);

            // then: the gate let them through to the read, and the probe was issued against the
            // key they asked about rather than anything the service invented.
            actualVerdict.Should().NotBeNull();
            actualVerdict.ApprovalId.Should().Be(storedApprovalId);
            actualVerdict.EntityType.Should().Be(inputEntityType);
            actualVerdict.EntityId.Should().Be(inputEntityId);

            this.approvalServiceMock.Verify(service =>
                service.FindApprovalByEntityAsync(
                    inputEntityType,
                    inputEntityId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldAdmitAReviewerWhoMayNotDecideRatherThanRefuseThemTheVerdictAsync()
        {
            // given: a reviewer, whom HR-3 bars from DECIDING — and the decision function says so
            // on both questions. That is a different question from whether they may SEE the
            // verdict: the verdict is how a reviewer learns whether their own review completed
            // the round (§8.6 regardless-rule 1), and they can already read the reviews and
            // comments individually, so the view gate admits them.
            //
            // The refusal must therefore surface as CanApprove false — a disabled button with a
            // reason — and never as an exception. Conflating the two gates would blind exactly
            // the people who record the verdicts to whether the round is finished.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Reviewers);

            EntityType inputEntityType = EntityType.Link;
            Guid inputEntityId = Guid.NewGuid();

            SetupApprovalProbe(CreateApprovalMatch());
            SetupConditions(CreateMetConditions());

            SetupAccessDecisions(
                decisionVerdict: RefusedVerdict(AccessDenialReason.ReviewerMayNotDecide),
                bypassVerdict: RefusedVerdict(AccessDenialReason.ReviewerMayNotDecide));

            // when
            ApprovalVerdict actualVerdict =
                await this.approvalOrchestrationService.RetrieveApprovalVerdictAsync(
                    inputEntityType,
                    inputEntityId,
                    TestContext.Current.CancellationToken);

            // then
            actualVerdict.Should().NotBeNull();
            actualVerdict.CanApprove.Should().BeFalse();
            actualVerdict.IsBypassAllowedForCurrentUser.Should().BeFalse();

            this.approvalServiceMock.Verify(service =>
                service.FindApprovalByEntityAsync(
                    inputEntityType,
                    inputEntityId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        /// THE ROUND THAT WAS NEVER OPENED, repaired rather than reported. Two ways in leave an
        /// entity without an approval: seed data written straight to the storage broker, which
        /// publishes no fact at all, and a fact that does not land. Both leave every read here
        /// answering NotFound to a caller who can do nothing about it.
        ///
        /// The added flow is RE-RUN rather than the row inserted, because the row alone is not
        /// what was lost — §9.7.3 resolves the approval and evaluates it, so a round that should
        /// already have auto-approved does so now.
        [Fact]
        public async Task ShouldOpenTheRoundOnRetrieveVerdictIfTheEntityHasNoApprovalYetAsync()
        {
            // given: no approval occupies the key, but the entity is real
            EntityType inputEntityType = EntityType.ContentItem;
            Guid inputEntityId = Guid.NewGuid();
            string entityAuthorUserId = GetRandomString();

            SetupApprovalProbe(null);

            this.accessBrokerMock.Setup(broker =>
                broker.RetrieveEntityAuthorAsync(
                    inputEntityType,
                    inputEntityId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(entityAuthorUserId);

            // Born Draft, so the added flow stops there (§9.7.3 rule 1) — which is the behaviour
            // under test, not the round's own outcome.
            var openedApproval = new Approval
            {
                Id = Guid.NewGuid(),
                EntityType = inputEntityType,
                EntityId = inputEntityId,
                ApprovalStatus = ApprovalStatus.Draft
            };

            this.approvalServiceMock.Setup(service =>
                service.AddApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(openedApproval);

            // when
            ValueTask<ApprovalVerdict> retrieveVerdictTask =
                this.approvalOrchestrationService.RetrieveApprovalVerdictAsync(
                    inputEntityType,
                    inputEntityId,
                    TestContext.Current.CancellationToken);

            // then: the probe is still mocked empty, so the read ends honestly — what matters
            // is that it TRIED to open the round first.
            await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                retrieveVerdictTask.AsTask);

            // then: the added flow ran, which is what opens the round — the approval is added
            // rather than merely probed for a second time
            this.approvalServiceMock.Verify(service =>
                service.AddApprovalAsync(
                    It.Is<Approval>(approval =>
                        approval.EntityType == inputEntityType
                            && approval.EntityId == inputEntityId),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveVerdictIfNoApprovalOccupiesTheKeyAndLogItAsync()
        {
            // given: the unfiltered probe finds the pair unoccupied, and the ENTITY does not
            // exist either — so there is nothing to repair and the read gives up honestly.
            // Reported as not-found rather than as an empty verdict, because a caller that
            // cannot tell "no approval exists" from "an approval exists and nothing blocks it"
            // would render an enabled approve button for a row with no approval behind it.
            //
            // The type is BibleReference and the id fresh, so the message is proved to name the
            // key that was asked about rather than a default.
            EntityType inputEntityType = EntityType.BibleReference;
            Guid inputEntityId = Guid.NewGuid();

            SetupApprovalProbe(null);

            var notFoundApprovalOrchestrationException =
                new NotFoundApprovalOrchestrationException(
                    message: $"Approval not found for {inputEntityType} with id: {inputEntityId}.");

            var expectedValidationException =
                new ApprovalOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: notFoundApprovalOrchestrationException);

            // when
            ValueTask<ApprovalVerdict> retrieveVerdictTask =
                this.approvalOrchestrationService.RetrieveApprovalVerdictAsync(
                    inputEntityType,
                    inputEntityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    retrieveVerdictTask.AsTask);

            // then: no policy question is asked at all — there is no approval to ask it about,
            // and an evaluation on a null id would be a lookup the broker cannot answer.
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            // The entity probe IS asked — it is what decides whether a missing round is worth
            // repairing — and answers with nothing, so the added flow is never run.
            this.accessBrokerMock.Verify(broker =>
                broker.RetrieveEntityAuthorAsync(
                    inputEntityType,
                    inputEntityId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.accessBrokerMock.Verify(broker =>
                broker.EvaluateApprovalConditionsByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.accessBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveVerdictIfTheConditionsVerdictIsNullAndLogItAsync()
        {
            // given: the probe found the approval, but by the time the conditions were evaluated
            // it was gone — a concurrent hard removal between the two reads. The broker reports
            // that as null rather than as an empty verdict precisely so this can be told apart
            // from "nothing is blocking", and it is answered with the SAME not-found sentence a
            // missing approval gives, rather than dereferenced into a service exception.
            EntityType inputEntityType = EntityType.Comment;
            Guid inputEntityId = Guid.NewGuid();

            SetupApprovalProbe(CreateApprovalMatch());
            SetupConditions(null);

            var notFoundApprovalOrchestrationException =
                new NotFoundApprovalOrchestrationException(
                    message: $"Approval not found for {inputEntityType} with id: {inputEntityId}.");

            var expectedValidationException =
                new ApprovalOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: notFoundApprovalOrchestrationException);

            // when
            ValueTask<ApprovalVerdict> retrieveVerdictTask =
                this.approvalOrchestrationService.RetrieveApprovalVerdictAsync(
                    inputEntityType,
                    inputEntityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    retrieveVerdictTask.AsTask);

            // then: neither decision question is asked — there is nothing left to decide about,
            // and the composition that consumes the conditions is never reached.
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.accessBrokerMock.Verify(broker =>
                broker.MayDecideApprovalByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<ApprovalDecision>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>(),
                    It.IsAny<SecurityContext>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
