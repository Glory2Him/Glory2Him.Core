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
    /// <para><b>One composition rather than a projection per surface.</b> The panel needs names
    /// for reviewers and for invited people; a display name hung off the review read would have
    /// answered the first and left the next to invent its own. Candidates carry their names on
    /// their own read, because eligibility and naming are one question there - and both surfaces
    /// compose through ComposeDisplayName, which is what actually stops one person rendering
    /// under two names, rather than the number of round trips.</para>
    ///
    /// <para><b>Keyed on the ROUND, like every other operation on the controller.</b> The tier
    /// gate no longer stands alone: it composes with an entity gate, so a Tag-Reviewer can name
    /// only the people a tag round involves rather than any account id in the directory. The
    /// posture the unscoped form used to borrow from the candidates read now actually holds.</para>
    ///
    /// <para><b>The round, and never the tier.</b> The answer is exactly the people the round
    /// involved - everybody with a review row on it, dismissed and soft-deleted included, plus
    /// everybody still invited - resolved in ONE identity read that applies no role filter and
    /// no disabled filter. Who COULD be invited is a different question with its own route, and
    /// answering it here would only put every global moderator into a Tag-Reviewer's
    /// response.</para>
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

                // THE ROUND'S OWN PEOPLE, and nobody else. The review rows are taken RECORDED
                // rather than active: a dismissed or withdrawn verdict is still shown, so its
                // author still needs a name, and the reviewer who voted and then lost the role
                // is exactly the case this resolver exists for. The outstanding invitations
                // join them because a panel draws an invited person before they have answered
                // anything.
                //
                // THE REVIEW TIER IS DELIBERATELY NOT READ HERE. It belonged to the id-keyed
                // form, where the caller supplied the ids and the tier decided which of them
                // were legal to answer. Keyed on the round the caller supplies none, so a tier
                // read admits nobody - its only remaining effect is to add every global
                // Publisher, Reviewer and Administrator to the answer, which is what
                // GET .../ReviewerCandidates already returns, display names included, to the
                // same panel. Dropping it also finishes what re-keying this route began: the
                // people a Tag-Reviewer can name are the ones the round involved, not the whole
                // moderator directory.
                var roundUserIds = new HashSet<string>(
                    scope.RecordedReviewerUserIds,
                    StringComparer.Ordinal);

                roundUserIds.UnionWith(
                    scope.ActiveRequests.Select(activeRequest => activeRequest.RequestedUserId));

                // The broker drops blank CreatedBy values as it gathers, but RequestedUserId
                // arrives off the invitation row exactly as stored, so the blank filter belongs
                // here rather than being left to the identity read's own parse: what is handed
                // over should be the round's statement of who it holds, not a set that is
                // non-empty while naming nobody.
                List<string> roundReviewerUserIds = roundUserIds
                    .Where(roundUserId => string.IsNullOrWhiteSpace(roundUserId) is false)
                    .ToList();

                // ONE identity read, always, asked for exactly these ids. It applies no role
                // filter and no disabled filter, because every id came off a row this round
                // already stores - the account is part of the record whatever has happened to
                // it since, and filtering by the tier is precisely what left a departed
                // reviewer with no name at all. An empty set is not an invitation to name
                // everybody: the foundation fails closed and answers nothing with nothing.
                IReadOnlyList<IdentityUser> roundUsers =
                    await this.identityUserService.RetrieveIdentityUsersByIdsAsync(
                        userIds: roundReviewerUserIds,
                        cancellationToken: cancellationToken);

                // Ids naming nobody are simply absent - a deleted account leaves a shorter list
                // and the surface renders its own fallback, where an error would let one
                // departed person break a whole panel. They fall out here rather than being
                // filtered for: an id that resolved to no account comes back from no read.
                //
                // Ordered by the rendered name, culture-aware like the candidates read so the
                // two sit together in the picker, then by id - two accounts sharing a display
                // name is ordinary, and without the tiebreak their order would be whatever the
                // store happened to return.
                return roundUsers
                    .Select(roundUser => new ReviewerDisplayName
                    {
                        UserId = roundUser.Id.ToString(),
                        DisplayName = ComposeDisplayName(roundUser),
                    })
                    .OrderBy(
                        reviewerDisplayName => reviewerDisplayName.DisplayName,
                        StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(
                        reviewerDisplayName => reviewerDisplayName.UserId,
                        StringComparer.Ordinal)
                    .ToList();
            });
    }
}
