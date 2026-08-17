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
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Foundations.Approvals;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.ApprovalReviews
{
    /// <summary>
    /// The endpoint binds the Core persistence entity, which carries an <c>Approval</c> navigation.
    /// The host suppresses MVC's inferred <c>[Required]</c> host-wide (#237), which is what makes a
    /// valid POST possible at all — but suppressing the requirement also leaves the navigation
    /// <b>bindable</b>, and the storage broker inserts through an EF graph add, which marks every
    /// reachable untracked entity <c>Added</c>.
    ///
    /// <para>So the question is whether a caller can smuggle a parent round in beside their verdict
    /// and have it written. Nothing in the foundation's rule set mentions the navigation, and the
    /// exposer skill §1.8 names this exact property as the thing to verify on review — and says a
    /// unit test cannot catch it, because a unit test calls the action directly and never goes
    /// through model binding. Only a request through the real pipeline settles it, which is what
    /// this does.</para>
    ///
    /// <para><b>Result: the smuggled round is not written.</b> The post is ACCEPTED (201) and the
    /// verdict lands against its legitimate parent, while the nested object is inert. This test
    /// pins that behaviour rather than the mechanism, deliberately — the entity is handed to the
    /// storage broker un-rebuilt, so the protection comes from somewhere below the service and is
    /// not stated by any rule the foundation writes. An implicit protection is exactly the kind
    /// that a later change to the persistence client could remove silently, which is why it is
    /// asserted here at all.</para>
    /// </summary>
    public partial class ApprovalReviewApiTests
    {
        [Fact]
        public async Task ShouldNotWriteANestedApprovalSmuggledBesideTheVerdictAsync()
        {
            // given: a legitimate parent round, and a SECOND one that exists only in the payload
            Approval randomApproval =
                await this.apiBroker.InsertOpenApprovalAsync(Guid.NewGuid().ToString());

            var smuggledApprovalId = Guid.NewGuid();
            var reviewId = Guid.NewGuid();
            string now = DateTimeOffset.UtcNow.ToString("o");

            string json =
                $@"{{
                    ""id"": ""{reviewId}"",
                    ""approvalId"": ""{randomApproval.Id}"",
                    ""statusId"": 2,
                    ""comment"": ""smuggling a parent round in beside the verdict"",
                    ""isDeleted"": false,
                    ""approval"": {{
                        ""id"": ""{smuggledApprovalId}"",
                        ""entityType"": 1,
                        ""entityId"": ""{Guid.NewGuid()}"",
                        ""approvalStatus"": 2,
                        ""isApprovedByBypass"": true,
                        ""approvedByBypassReason"": ""forged through a navigation property"",
                        ""isDeleted"": false,
                        ""createdBy"": ""attacker"",
                        ""createdWhen"": ""{now}"",
                        ""updatedBy"": ""attacker"",
                        ""updatedWhen"": ""{now}""
                    }}
                }}";

            try
            {
                // when
                HttpStatusCode actualStatusCode =
                    await this.apiBroker.PostApprovalReviewRawAsync(json);

                // then: whatever the endpoint answers, the smuggled round must NOT exist. An
                // approval carrying IsApprovedByBypass = true that no decision ever made is the
                // forgery the whole §9.7.5 derivation exists to prevent, and it must not be
                // reachable by writing through a navigation property.
                Approval smuggledApproval =
                    await this.apiBroker.GetCoreApprovalByIdAsync(smuggledApprovalId);

                smuggledApproval.Should().BeNull(
                    because: "a nested navigation must never write a parent row; status was "
                        + actualStatusCode);
            }
            finally
            {
                await this.apiBroker.RemoveCoreApprovalReviewByIdAsync(reviewId);
                await this.apiBroker.RemoveApprovalByIdAsync(smuggledApprovalId);
                await this.apiBroker.RemoveApprovalByIdAsync(randomApproval.Id);
            }
        }
    }
}
