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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.IdentityUsers;
using Glory2Him.Core.Models.Orchestrations.Approvals;
using Glory2Him.Core.Models.Securities;

namespace Glory2Him.Core.Services.Orchestrations.Approvals
{
    /// <summary>
    /// The name resolver every review surface asks (design 16.7.4).
    ///
    /// <para>ApprovalReview carries CreatedBy, which is an account id, and until this existed the
    /// only route that named other people was /api/admin/users behind the Administrators role. A
    /// Publisher who is not an administrator - precisely the tier the review panel exists for -
    /// could render their own name and nobody else's.</para>
    ///
    /// <para><b>One resolver rather than a projection per surface.</b> The panel needs names for
    /// reviewers, for invited people and for candidates; a display name hung off the review read
    /// would have answered the first and left the next to invent its own, and three lookups are
    /// three chances to render one person under two names. Everything here composes the name
    /// through ComposeDisplayName, the same method the candidates read uses.</para>
    ///
    /// <para><b>Keyed on the ROUND, like every other operation on the controller.</b> The tier
    /// gate no longer stands alone: it composes with an entity gate, so a Tag-Reviewer can name
    /// only the people a tag round involves rather than any account id in the directory. The
    /// posture the unscoped form used to borrow from the candidates read now actually holds.</para>
    ///
    /// <para>It sits beside the invitation operations rather than in a foundation for the reason
    /// they do: WHO may enumerate users is an approval-workflow decision (7.9 rule 2), and the
    /// identity foundation deliberately takes no caller identity at all.</para>
    /// </summary>
    internal partial class ApprovalOrchestrationService
    {
        public ValueTask<IReadOnlyList<ReviewerDisplayName>> RetrieveReviewerDisplayNamesAsync(
            EntityType entityType,
            Guid entityId,
            CancellationToken cancellationToken = default) =>
            TryCatch<IReadOnlyList<ReviewerDisplayName>>(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateOnRetrieveReviewerDisplayNames(entityType, entityId);

                ApprovalReviewerScope scope = await ResolveReviewerScopeAsync(
                    entityType: entityType,
                    entityId: entityId,
                    onSecurityContext: ValidateUserMayRequestApprovalReviews,
                    cancellationToken: cancellationToken);

                // The tier members are read FIRST, and they arrive as whole accounts rather than
                // as ids. That is what keeps the round-scoped form at one identity read in the
                // ordinary case: the same rows that admit a candidate to the answer also carry
                // the name, so nothing is looked up twice.
                IReadOnlyList<IdentityUser> tierMembers =
                    await this.identityUserService.RetrieveIdentityUsersInRolesAsync(
                        roleNames: ComposeReviewTierRoleNames(scope.RoleSubjects),
                        cancellationToken: cancellationToken);

                var namedUsers = new Dictionary<string, IdentityUser>(StringComparer.Ordinal);

                foreach (IdentityUser tierMember in tierMembers)
                {
                    namedUsers[tierMember.Id.ToString()] = tierMember;
                }

                // The round's own people, which is the half the tier read cannot supply. The
                // review rows are taken RECORDED rather than active: a dismissed or withdrawn
                // verdict is still rendered, so its author still needs a name, and it is exactly
                // the reviewer who voted and then lost the role that this resolver exists for.
                var roundUserIds = new HashSet<string>(StringComparer.Ordinal);

                foreach (string reviewerUserId in scope.RecordedReviewerUserIds)
                {
                    roundUserIds.Add(reviewerUserId);
                }

                foreach (ActiveReviewRequest activeRequest in scope.ActiveRequests)
                {
                    roundUserIds.Add(activeRequest.RequestedUserId);
                }

                // A second identity read, and ONLY for the people the tier read could not name -
                // somebody who has left the tier or been disabled since they took part. It
                // applies no role filter and no disabled filter, because their id came off a row
                // this round already stores, so the account is part of the record whatever has
                // happened to it since. Filtering by the tier is precisely what left a departed
                // reviewer with no name at all.
                List<string> unnamedRoundUserIds = roundUserIds
                    .Where(roundUserId =>
                        string.IsNullOrWhiteSpace(roundUserId) is false
                            && namedUsers.ContainsKey(roundUserId) is false)
                    .ToList();

                if (unnamedRoundUserIds.Count > 0)
                {
                    IReadOnlyList<IdentityUser> departedUsers =
                        await this.identityUserService.RetrieveIdentityUsersByIdsAsync(
                            userIds: unnamedRoundUserIds,
                            cancellationToken: cancellationToken);

                    foreach (IdentityUser departedUser in departedUsers)
                    {
                        namedUsers[departedUser.Id.ToString()] = departedUser;
                    }
                }

                // Ids naming nobody are simply absent - a deleted account leaves a shorter list
                // and the surface renders its own fallback, where an error would let one departed
                // person break a whole panel. They fall out here rather than being filtered for:
                // an id that resolved to no account never entered the dictionary.
                return namedUsers
                    .Select(namedUser => new ReviewerDisplayName
                    {
                        UserId = namedUser.Value.Id.ToString(),
                        DisplayName = ComposeDisplayName(namedUser.Value),
                    })
                    .OrderBy(
                        reviewerDisplayName => reviewerDisplayName.DisplayName,
                        StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
            });
    }
}
