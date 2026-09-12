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
using G2H.Security.Client.Models.Foundations.Access;
using G2H.Security.Client.Models.Securities;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Models.Foundations.IdentityUsers;
using Glory2Him.Core.Models.Orchestrations.Approvals;
using Glory2Him.Core.Models.Orchestrations.Approvals.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Approvals
{
    /// <summary>
    /// How the <c>ReadOnly</c> veto reaches the approval SURFACE (design §18.6 rule 2, §16.7.2,
    /// §16.7.4, §7.9). An approval has no role vocabulary of its own — its scope is derived from
    /// the attached entity — so a block in scope stops the holder voting on or deciding it, not
    /// merely writing the content.
    ///
    /// <para>Two of the three surfaces get the answer for free, and are asserted here so that
    /// stays true: the verdict's per-caller <c>CanApprove</c> IS the decision verdict, so a panel
    /// taking it verbatim (§20.6.1) reports the block instead of offering a control the server
    /// would then refuse. The third — the candidate list and the invitation — has to subtract
    /// explicitly, because inviting somebody who cannot vote stalls the round for no reason.
    /// </para>
    /// </summary>
    public partial class ApprovalOrchestrationServiceTests
    {
        [Fact]
        public async Task ShouldReportTheBlockInTheVerdictWhenTheCallerIsReadOnlyForTheEntityAsync()
        {
            // given: nothing about the approval blocks it — the conditions are met — and the
            // caller is still refused. Without the reason travelling into the reason set, a
            // panel would render "nothing is blocking this" beside a disabled approve button,
            // which is the one outcome guaranteed to look like a bug.
            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted));
            SetupConditions(CreateMetConditions());

            SetupAccessDecisions(
                decisionVerdict: RefusedVerdict(AccessDenialReason.BlockedByReadOnlyRole),
                bypassVerdict: RefusedVerdict(AccessDenialReason.BlockedByReadOnlyRole));

            // when
            ApprovalVerdict actualVerdict =
                await this.approvalOrchestrationService.RetrieveApprovalVerdictAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            // then
            actualVerdict.CanApprove.Should().BeFalse();

            // The bypass is closed too: what a bypass waives are the §8.5 conditions, never the
            // veto, so a route that survived it would make the block advisory.
            actualVerdict.IsBypassAllowedForCurrentUser.Should().BeFalse();

            actualVerdict.BlockReasons.Select(reason => reason.Code)
                .Should().ContainSingle()
                .Which.Should().Be(AccessDenialReason.BlockedByReadOnlyRole);

            // Composed in Core, and it says the sanction applies without naming which scope of
            // it fired — no scope of it is appealable through this surface.
            actualVerdict.BlockReasons.Single().Message.Should().Be(
                "Your account is restricted to read-only for this content, so you cannot "
                    + "review or approve it.");
        }
    }
}
