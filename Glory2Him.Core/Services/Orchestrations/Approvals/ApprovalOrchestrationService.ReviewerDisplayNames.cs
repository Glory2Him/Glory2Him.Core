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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.IdentityUsers;
using Glory2Him.Core.Models.Orchestrations.Approvals;

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
    /// <para>It sits beside the invitation operations rather than in a foundation for the reason
    /// they do: WHO may enumerate users is an approval-workflow decision (7.9 rule 2), and the
    /// identity foundation deliberately takes no caller identity at all.</para>
    /// </summary>
    internal partial class ApprovalOrchestrationService
    {
        // Bounded, and refused rather than truncated: truncating silently would hand a surface a
        // shorter answer than it asked for and let it render blanks it could not explain. 200
        // comfortably covers a round's reviewers, invitations and candidates together, which is
        // the largest thing any one surface holds.
        //
        // <b>It bounds a RESPONSE, not a caller.</b> Nothing here counts requests, so a permitted
        // caller can page through as many batches as it likes - the cap is a shape rule, and the
        // thing that actually decides how much of the directory is reachable is the tier gate
        // below. Do not read this constant as the enumeration control; 16.7.4's posture is.
        private const int MaximumReviewerDisplayNameBatch = 200;

        public ValueTask<IReadOnlyList<ReviewerDisplayName>> RetrieveReviewerDisplayNamesAsync(
            IEnumerable<string> userIds,
            CancellationToken cancellationToken = default) =>
            TryCatch<IReadOnlyList<ReviewerDisplayName>>(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // PARSED before it is counted, and that ordering is the rule rather than an
                // implementation detail. A GUID has several equal spellings - case, braces,
                // hyphens - so counting the raw strings would let one account arrive as two and
                // refuse a caller who asked about 200 people using 201 spellings. The cap counts
                // ACCOUNTS. Canonicalising here also makes the UserId echoed back in the answer
                // identical to what the caller sent, so a surface can join on it whatever form it
                // holds ids in.
                //
                // Unparseable ids fall out before the count too. They can never name an account,
                // so charging them against a ceiling that exists to bound a query would refuse
                // work nobody asked for.
                List<Guid> requestedUserIds = (userIds ?? Enumerable.Empty<string>())
                    .Where(userId => string.IsNullOrWhiteSpace(userId) is false)
                    .Select(userId =>
                        Guid.TryParse(userId.Trim(), out Guid parsedUserId)
                            ? parsedUserId
                            : Guid.Empty)
                    .Where(parsedUserId => parsedUserId != Guid.Empty)
                    .Distinct()
                    .ToList();

                ValidateOnRetrieveReviewerDisplayNames(requestedUserIds);

                List<string> canonicalUserIds = requestedUserIds
                    .Select(requestedUserId => requestedUserId.ToString())
                    .ToList();

                // The envelope is minted for its SECURITY CONTEXT alone - this operation reads no
                // approval, so there is no entity to hang one off. Its content is the id list
                // itself rather than a borrowed model, because that is what the request is about
                // and a stand-in Approval would describe a row nobody asked for.
                EventEnvelope<IReadOnlyList<string>> envelope =
                    await this.eventEnvelopeBroker.CreateAsync<IReadOnlyList<string>>(
                        content: canonicalUserIds);

                ValidateUserMayRequestApprovalReviews(envelope.SecurityContext);

                // No role filter and no disabled filter beneath this, deliberately. Every id here
                // came off a row the caller already holds, so the account is part of the record
                // whatever has happened to it since - and filtering by the review tier is exactly
                // what left a reviewer who had lost their role with no name at all.
                IReadOnlyList<IdentityUser> resolvedUsers =
                    await this.identityUserService.RetrieveIdentityUsersByIdsAsync(
                        userIds: canonicalUserIds,
                        cancellationToken: cancellationToken);

                // Ids naming nobody are simply absent. A caller asking about somebody whose
                // account has been deleted gets a shorter list and renders its own fallback; an
                // error would make one departed account break a whole panel.
                return resolvedUsers
                    .Select(resolvedUser => new ReviewerDisplayName
                    {
                        UserId = resolvedUser.Id.ToString(),
                        DisplayName = ComposeDisplayName(resolvedUser),
                    })
                    .OrderBy(
                        reviewerDisplayName => reviewerDisplayName.DisplayName,
                        StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
            });
    }
}
