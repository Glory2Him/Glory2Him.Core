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
using System.Threading.Tasks;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests.Exceptions;
using Glory2Him.Core.Models.Securities;

namespace Glory2Him.Core.Services.Foundations.ApprovalReviewRequests
{
    internal partial class ApprovalReviewRequestService
    {
        // the §16.6 scoped-role suffixes; the entity prefix in front of them varies per
        // entity type, so only the suffix is a fixed part of the convention
        private const string ScopedReviewerRoleSuffix = Roles.ReviewerSuffix;
        private const string ScopedPublisherRoleSuffix = Roles.PublisherSuffix;

        // the foundation enforces the same security rules as the orchestration (design
        // §14.6): an exposer may bind to either service directly, so no layer may assume
        // an upstream layer already gated the caller

        private static void ValidateUserIsAllowedToContribute(SecurityContext securityContext)
        {
            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                throw new UnauthorizedApprovalReviewRequestException(
                    message: "The current user is not authenticated.");
            }

            // an approval review request is workflow bookkeeping rather than user-contributed
            // content, so no ApprovalReviewRequest-scoped ReadOnly role exists — only the global
            // block role applies here
            if (securityContext.Roles.Contains(Roles.ReadOnly))
            {
                throw new UnauthorizedApprovalReviewRequestException(
                    message: "The current user is blocked from contributing approval review requests.");
            }
        }

        // Inviting somebody to review is coordination of the round, so it is open to everyone
        // inside the round — the whole review tier, publishers and reviewers alike (§7.9 rule 2).
        // HR-3 does not narrow it: that rule bars a reviewer from SETTING an ApprovalStatus, and
        // an invitation sets nothing.
        //
        // The same gate covers withdrawal, which is the deliberate widening of §7.9 rule 5.
        private static void ValidateUserIsAllowedToRequestApprovalReviews(SecurityContext securityContext)
        {
            ValidateUserIsAllowedToContribute(securityContext);

            if (HasReviewRole(securityContext) is false)
            {
                throw new UnauthorizedApprovalReviewRequestException(
                    message: "The current user is not allowed to request approval reviews.");
            }
        }

        // The review roles that may issue, withdraw and read invitations: the global Reviewer,
        // Publisher and Admin roles plus — by the §16.6 naming convention — any entity-scoped
        // "%EntityType%-Reviewer"/"%EntityType%-Publisher" role, including the content-type-scoped
        // tier of §18.6 rule 5, which ends in the same suffix.
        //
        // The request row names no entity type, so this cannot tell a Tag-Reviewer from a
        // Link-Reviewer row-locally — the same limit its ApprovalReview sibling carries, and the
        // reason the orchestration re-asks the question against the entity behind the approval
        // (§16.7.4) before a request is ever written.
        private static bool HasReviewRole(SecurityContext securityContext) =>
            securityContext.Roles.Contains(Roles.Reviewer)
                || securityContext.Roles.Contains(Roles.Publisher)
                || securityContext.Roles.Contains(Roles.Admin)
                || securityContext.Roles.Any(role =>
                    role.EndsWith(ScopedReviewerRoleSuffix, StringComparison.Ordinal)
                        || role.EndsWith(ScopedPublisherRoleSuffix, StringComparison.Ordinal));

        // The two people an invitation is between. Used by the read posture only — neither
        // confers a write, because withdrawal is the tier's (§7.9 rule 5) and there is nothing
        // else to write.
        private static bool IsPartyToRequest(
            ApprovalReviewRequest approvalReviewRequest,
            string actorUserId) =>
            string.IsNullOrWhiteSpace(actorUserId) is false
                && (approvalReviewRequest.CreatedBy == actorUserId
                    || approvalReviewRequest.RequestedUserId == actorUserId);

        // a hard remove destroys the row and its audit trail — Admin only
        private static void ValidateUserCanHardRemoveApprovalReviewRequest(SecurityContext securityContext)
        {
            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                throw new UnauthorizedApprovalReviewRequestException(
                    message: "The current user is not authenticated.");
            }

            if (securityContext.Roles.Contains(Roles.ReadOnly))
            {
                throw new UnauthorizedApprovalReviewRequestException(
                    message: "The current user is blocked from contributing approval review requests.");
            }

            if (securityContext.Roles.Contains(Roles.Admin) is false)
            {
                throw new UnauthorizedApprovalReviewRequestException(
                    message: "The current user is not allowed to permanently remove this " +
                        "approval review request.");
            }
        }

        private async ValueTask ValidateOnAddApprovalReviewRequestAsync(
            ApprovalReviewRequest approvalReviewRequest,
            SecurityContext securityContext)
        {
            ValidateApprovalReviewRequestIsNotNull(approvalReviewRequest);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Approval review request is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalReviewRequest.Id),
                    Parameter: nameof(ApprovalReviewRequest.Id)),

                (Rule: IsInvalid(approvalReviewRequest.ApprovalId),
                    Parameter: nameof(ApprovalReviewRequest.ApprovalId)),

                // The invited person's identity — required, because a request naming nobody
                // invites nobody and would still occupy a uniqueness slot.
                (Rule: IsInvalid(approvalReviewRequest.RequestedUserId),
                    Parameter: nameof(ApprovalReviewRequest.RequestedUserId)),

                (Rule: IsGreaterThan(approvalReviewRequest.RequestedUserId, 255),
                    Parameter: nameof(ApprovalReviewRequest.RequestedUserId)),

                // Capped but NOT required: a display name can legitimately be blank in the
                // identity store, and refusing the invitation over a cosmetic field would block
                // a request the policy allows.
                (Rule: IsGreaterThan(approvalReviewRequest.RequestedUserDisplayName, 255),
                    Parameter: nameof(ApprovalReviewRequest.RequestedUserDisplayName)),

                (Rule: IsInvalid(approvalReviewRequest.CreatedBy),
                    Parameter: nameof(ApprovalReviewRequest.CreatedBy)),

                (Rule: IsInvalid(approvalReviewRequest.UpdatedBy),
                    Parameter: nameof(ApprovalReviewRequest.UpdatedBy)),

                (Rule: IsInvalid(approvalReviewRequest.CreatedWhen),
                    Parameter: nameof(ApprovalReviewRequest.CreatedWhen)),

                (Rule: IsInvalid(approvalReviewRequest.UpdatedWhen),
                    Parameter: nameof(ApprovalReviewRequest.UpdatedWhen)),

                (Rule: IsGreaterThan(approvalReviewRequest.CreatedBy, 255),
                    Parameter: nameof(ApprovalReviewRequest.CreatedBy)),

                (Rule: IsGreaterThan(approvalReviewRequest.UpdatedBy, 255),
                    Parameter: nameof(ApprovalReviewRequest.UpdatedBy)),

                (Rule: IsNotSame(
                        firstDate: approvalReviewRequest.UpdatedWhen,
                        secondDate: approvalReviewRequest.CreatedWhen,
                        secondDateName: nameof(ApprovalReviewRequest.CreatedWhen)),
                    Parameter: nameof(ApprovalReviewRequest.UpdatedWhen)),

                // CreatedBy is the REQUESTER and must be the acting user. This is what keeps the
                // entity's central claim true — that a request, unlike a placeholder review, names
                // its author honestly (§7.9).
                (Rule: IsNotSame(
                        first: currentUserId,
                        second: approvalReviewRequest.CreatedBy),
                    Parameter: nameof(ApprovalReviewRequest.CreatedBy)),

                (Rule: IsNotSame(
                        first: approvalReviewRequest.UpdatedBy,
                        second: approvalReviewRequest.CreatedBy,
                        secondName: nameof(ApprovalReviewRequest.CreatedBy)),
                    Parameter: nameof(ApprovalReviewRequest.UpdatedBy)),

                (Rule: await IsNotRecentAsync(approvalReviewRequest.CreatedWhen),
                    Parameter: nameof(ApprovalReviewRequest.CreatedWhen)));
        }

        // Null-check first (a malformed event), then verify the integrity signature against the
        // event name this handler serves and the request direction. The signature is what makes
        // the envelope's SecurityContext trustworthy on the event path: without it a caller who can
        // put a message on this address states their own identity and roles and is believed
        // (design §14.6 rule 4). Verification sits in the receiver, not the transport, because a
        // handler is reachable without going through the broker.
        private async ValueTask ValidateApprovalReviewRequestEventEnvelopeAsync(
            EventEnvelope<ApprovalReviewRequest> envelope,
            ApprovalReviewRequestEventOperation operation)
        {
            if (envelope is null || envelope.Content is null || envelope.Metadata is null)
            {
                throw new InvalidApprovalReviewRequestEventException(
                    message: "Invalid approval review request event. " +
                        "The event envelope, its content and metadata are required.");
            }

            string eventName = $"{nameof(ApprovalReviewRequest)}{operation}";

            bool isSignatureValid = await this.envelopeIntegrityBroker.VerifyAsync(
                envelope, eventName, EnvelopeDirection.Request);

            if (isSignatureValid is false)
            {
                throw new InvalidApprovalReviewRequestEventException(
                    message: "Invalid approval review request event. Integrity verification failed.");
            }
        }

        private static void ValidateOnRetrieveApprovalReviewRequestById(Guid approvalReviewRequestId) =>
            Validate(
                message: "Approval review request is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalReviewRequestId), Parameter: nameof(ApprovalReviewRequest.Id)));

        // the deletion reason is caller-supplied free text that lands on the row unchanged,
        // so its storage cap is enforced here rather than left to the column to reject
        private static void ValidateOnRemoveApprovalReviewRequestById(
            Guid approvalReviewRequestId,
            string? deletionReason) =>
            Validate(
                message: "Approval review request is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalReviewRequestId), Parameter: nameof(ApprovalReviewRequest.Id)),

                (Rule: IsGreaterThan(deletionReason, 500),
                    Parameter: nameof(ApprovalReviewRequest.DeletionReason)));

        private static void ValidateOnHardRemoveApprovalReviewRequestById(Guid approvalReviewRequestId) =>
            Validate(
                message: "Approval review request is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalReviewRequestId), Parameter: nameof(ApprovalReviewRequest.Id)));

        private static void ValidateStorageApprovalReviewRequest(
            ApprovalReviewRequest maybeApprovalReviewRequest,
            Guid approvalReviewRequestId)
        {
            if (maybeApprovalReviewRequest is null)
            {
                throw new NotFoundApprovalReviewRequestException(
                    message: $"Approval review request not found with id: {approvalReviewRequestId}.");
            }
        }

        private static void ValidateApprovalReviewRequestIsNotNull(
            ApprovalReviewRequest approvalReviewRequest)
        {
            if (approvalReviewRequest is null)
            {
                throw new NullApprovalReviewRequestException(
                    message: "Approval review request is null.");
            }
        }

        private static dynamic IsInvalid(Guid id) => new
        {
            Condition = id == Guid.Empty,
            Message = "Id is required"
        };

        private static dynamic IsInvalid(string text) => new
        {
            Condition = string.IsNullOrWhiteSpace(text),
            Message = "Text is required"
        };

        private static dynamic IsInvalid(DateTimeOffset date) => new
        {
            Condition = date == default,
            Message = "Date is required"
        };

        private static dynamic IsNotSame(
            string first,
            string second) => new
            {
                Condition = first != second,
                Message = $"Expected value to be '{first}' but found '{second}'."
            };

        private static dynamic IsNotSame(
            string first,
            string second,
            string secondName) => new
            {
                Condition = first != second,
                Message = $"Text is not the same as {secondName}"
            };

        private static dynamic IsNotSame(
            DateTimeOffset firstDate,
            DateTimeOffset secondDate,
            string secondDateName) => new
            {
                Condition = firstDate != secondDate,
                Message = $"Date is not the same as {secondDateName}"
            };

        private static dynamic IsGreaterThan(string? text, int maxLength) => new
        {
            Condition = (text ?? string.Empty).Length > maxLength,
            Message = $"Text exceed max length of {maxLength} characters"
        };

        private async ValueTask<dynamic> IsNotRecentAsync(DateTimeOffset date)
        {
            var (isNotRecent, startDate, endDate) = await IsDateNotRecentAsync(date);

            return new
            {
                Condition = isNotRecent,
                Message = $"Date is not recent. Expected a value between {startDate} and {endDate} but found {date}"
            };
        }

        private async ValueTask<(bool IsNotRecent, DateTimeOffset StartDate, DateTimeOffset EndDate)>
            IsDateNotRecentAsync(DateTimeOffset date)
        {
            int pastThreshold = 90;
            int futureThreshold = 0;
            DateTimeOffset currentDateTime = await this.dateTimeBroker.GetCurrentDateTimeOffsetAsync();
            DateTimeOffset startDate = currentDateTime.AddSeconds(-pastThreshold);
            DateTimeOffset endDate = currentDateTime.AddSeconds(futureThreshold);
            bool isNotRecent = date < startDate || date > endDate;

            return (isNotRecent, startDate, endDate);
        }

        private static void Validate(
            string message,
            params (dynamic Rule, string Parameter)[] validations)
        {
            var invalidApprovalReviewRequestException = new InvalidApprovalReviewRequestException(message);

            foreach ((dynamic rule, string parameter) in validations)
            {
                if (rule.Condition)
                {
                    invalidApprovalReviewRequestException.UpsertDataList(
                        key: parameter,
                        value: rule.Message);
                }
            }

            invalidApprovalReviewRequestException.ThrowIfContainsErrors();
        }
    }
}
