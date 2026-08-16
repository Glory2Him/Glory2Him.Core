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
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Foundations.Approvals.Exceptions;
using Glory2Him.Core.Models.Securities;

namespace Glory2Him.Core.Services.Foundations.Approvals
{
    internal partial class ApprovalService
    {
        // the §16.6 scoped-role suffixes, built from the global role names so the
        // convention has a single source of truth
        private const string ScopedReviewerRoleSuffix = Roles.ReviewerSuffix;
        private const string ScopedPublisherRoleSuffix = Roles.PublisherSuffix;

        // the foundation enforces the same security rules as the orchestration (design
        // §14.6): an exposer may bind to either service directly, so no layer may assume
        // an upstream layer already gated the caller

        private static void ValidateUserIsAllowedToContribute(SecurityContext securityContext)
        {
            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                throw new UnauthorizedApprovalException(
                    message: "The current user is not authenticated.");
            }

            // no Approval-scoped ReadOnly role exists — approvals are workflow records,
            // not entity-scoped content, so only the global block role applies
            if (securityContext.Roles.Contains(Roles.ReadOnly))
            {
                throw new UnauthorizedApprovalException(
                    message: "The current user is blocked from contributing approvals.");
            }
        }

        // the review roles that may act on and read approval workflow records: the global
        // Tier 1, row-local: Reviewer/Publisher/Admin, plus — by the §16.6 convention — any
        // entity-scoped "{Entity}-Reviewer"/"{Entity}-Publisher" role.
        //
        // This check only ever sees the caller, so it cannot know which entity type an approval
        // targets: a Tag-Reviewer passes it for a Link's approval. Narrowing to the approval's
        // own entity type was once described as an orchestration concern; it is not, and there
        // is no orchestration to defer it to (§12.3.1). It lives in the foundation, one tier
        // down, through IAccessBroker — which can read the entity behind the approval where this
        // cannot. Both tiers run: §14.6 rule 2 and §8.6.1 make the duplicate intentional, since
        // a defect in the gathering can only ever make the pair stricter.
        private static bool HasReviewRole(SecurityContext securityContext) =>
            securityContext.Roles.Contains(Roles.Reviewer)
                || securityContext.Roles.Contains(Roles.Publisher)
                || securityContext.Roles.Contains(Roles.Admin)
                || securityContext.Roles.Any(role =>
                    role.EndsWith(ScopedReviewerRoleSuffix, StringComparison.Ordinal)
                        || role.EndsWith(ScopedPublisherRoleSuffix, StringComparison.Ordinal));

        // row-level write permission: the submitter who opened the approval may amend it
        // and a review role may act on it — the narrower workflow rules (which status
        // transitions are legal, who may bypass) stay in the orchestration
        private async ValueTask ValidateUserCanModifyStorageApprovalAsync(
            Approval storageApproval,
            SecurityContext securityContext)
        {
            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && storageApproval.CreatedBy == actorUserId;

            if (isOwner is false && HasReviewRole(securityContext) is false)
            {
                throw new UnauthorizedApprovalException(
                    message: "The current user is not allowed to modify this approval.");
            }
        }

        // Tier 2, cross-entity. HasReviewRole above matches ANY "-Reviewer"/"-Publisher" suffix,
        // so a bare Tag-Reviewer clears it for a ContentItem↔BibleReference association's
        // approval. The broker resolves the entity behind the approval — for an association,
        // both of its endpoints, either of which is enough (§14.7 posture A′ rule 2).
        //
        // The REVIEW tier, not the publisher tier: §14.7 posture D rule 3 has reviewers move an
        // approval's status through this path, so narrowing to publishers would refuse the very
        // callers the rule admits.
        private async ValueTask ValidateUserMayAmendStorageApprovalAsync(
            Approval storageApproval,
            SecurityContext securityContext,
            CancellationToken cancellationToken)
        {
            AccessVerdict verdict = await this.accessBroker.MayAmendApprovalAsync(
                approvalId: storageApproval.Id,
                securityContext: securityContext,
                cancellationToken: cancellationToken);

            if (verdict.IsPermitted is false)
            {
                // §14.5: the true reason server-side, nothing about the policy to the caller.
                await this.loggingBroker.LogWarningAsync(
                    $"Approval modification denied for approval {storageApproval.Id}. "
                        + $"{verdict.DenialReason}: {verdict.Explanation} "
                        + "Reported to the caller as unauthorized.");

                throw new UnauthorizedApprovalException(
                    message: "The current user is not allowed to modify this approval.");
            }
        }

        // removing an approval retracts the workflow record itself — the owner may
        // withdraw their own and an Admin may remove anyone's; Reviewers and Publishers
        // act through the approval's status instead
        private async ValueTask ValidateUserCanRemoveStorageApprovalAsync(
            Approval storageApproval,
            SecurityContext securityContext)
        {
            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && storageApproval.CreatedBy == actorUserId;

            if (isOwner is false && securityContext.Roles.Contains(Roles.Admin) is false)
            {
                throw new UnauthorizedApprovalException(
                    message: "The current user is not allowed to remove this approval.");
            }
        }

        // a hard remove destroys the row and its audit trail — Admin only
        private static void ValidateUserCanHardRemoveApproval(SecurityContext securityContext)
        {
            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                throw new UnauthorizedApprovalException(
                    message: "The current user is not authenticated.");
            }

            if (securityContext.Roles.Contains(Roles.ReadOnly))
            {
                throw new UnauthorizedApprovalException(
                    message: "The current user is blocked from contributing approvals.");
            }

            if (securityContext.Roles.Contains(Roles.Admin) is false)
            {
                throw new UnauthorizedApprovalException(
                    message: "The current user is not allowed to permanently remove this approval.");
            }
        }

        private async ValueTask ValidateOnAddApprovalAsync(
            Approval approval,
            SecurityContext securityContext)
        {
            ValidateApprovalIsNotNull(approval);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Approval is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approval.Id), Parameter: nameof(Approval.Id)),
                (Rule: IsInvalid(approval.EntityId), Parameter: nameof(Approval.EntityId)),
                (Rule: IsInvalid(approval.CreatedBy), Parameter: nameof(Approval.CreatedBy)),
                (Rule: IsInvalid(approval.UpdatedBy), Parameter: nameof(Approval.UpdatedBy)),
                (Rule: IsInvalid(approval.CreatedWhen), Parameter: nameof(Approval.CreatedWhen)),
                (Rule: IsInvalid(approval.UpdatedWhen), Parameter: nameof(Approval.UpdatedWhen)),

                (Rule: IsGreaterThan(approval.CreatedBy, 255),
                    Parameter: nameof(Approval.CreatedBy)),

                (Rule: IsGreaterThan(approval.UpdatedBy, 255),
                    Parameter: nameof(Approval.UpdatedBy)),

                (Rule: IsNotSame(
                        firstDate: approval.UpdatedWhen,
                        secondDate: approval.CreatedWhen,
                        secondDateName: nameof(Approval.CreatedWhen)),
                    Parameter: nameof(Approval.UpdatedWhen)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: approval.CreatedBy),
                    Parameter: nameof(Approval.CreatedBy)),

                (Rule: IsNotSame(
                        first: approval.UpdatedBy,
                        second: approval.CreatedBy,
                        secondName: nameof(Approval.CreatedBy)),
                    Parameter: nameof(Approval.UpdatedBy)),

                (Rule: await IsNotRecentAsync(approval.CreatedWhen),
                    Parameter: nameof(Approval.CreatedWhen)));
        }

        private async ValueTask ValidateOnModifyApprovalAsync(
            Approval approval,
            SecurityContext securityContext)
        {
            ValidateApprovalIsNotNull(approval);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Approval is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approval.Id), Parameter: nameof(Approval.Id)),
                (Rule: IsInvalid(approval.EntityId), Parameter: nameof(Approval.EntityId)),
                (Rule: IsInvalid(approval.CreatedBy), Parameter: nameof(Approval.CreatedBy)),
                (Rule: IsInvalid(approval.UpdatedBy), Parameter: nameof(Approval.UpdatedBy)),
                (Rule: IsInvalid(approval.CreatedWhen), Parameter: nameof(Approval.CreatedWhen)),
                (Rule: IsInvalid(approval.UpdatedWhen), Parameter: nameof(Approval.UpdatedWhen)),

                (Rule: IsGreaterThan(approval.CreatedBy, 255),
                    Parameter: nameof(Approval.CreatedBy)),

                (Rule: IsGreaterThan(approval.UpdatedBy, 255),
                    Parameter: nameof(Approval.UpdatedBy)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: approval.UpdatedBy),
                    Parameter: nameof(Approval.UpdatedBy)),

                (Rule: IsSame(
                        firstDate: approval.UpdatedWhen,
                        secondDate: approval.CreatedWhen,
                        secondDateName: nameof(Approval.CreatedWhen)),
                    Parameter: nameof(Approval.UpdatedWhen)),

                (Rule: await IsNotRecentAsync(approval.UpdatedWhen),
                    Parameter: nameof(Approval.UpdatedWhen)));
        }

        // Null-check first (a malformed event), then verify the integrity signature against the
        // event name this handler serves and the request direction. The signature is what makes
        // the envelope's SecurityContext trustworthy on the event path: without it a caller who can
        // put a message on this address states their own identity and roles and is believed
        // (design §14.6 rule 4). Verification sits in the receiver, not the transport, because a
        // handler is reachable without going through the broker.
        private async ValueTask ValidateApprovalEventEnvelopeAsync(
            EventEnvelope<Approval> envelope,
            ApprovalEventOperation operation)
        {
            if (envelope is null || envelope.Content is null || envelope.Metadata is null)
            {
                throw new InvalidApprovalEventException(
                    message: "Invalid approval event. " +
                        "The event envelope, its content and metadata are required.");
            }

            string eventName = $"{nameof(Approval)}{operation}";

            bool isSignatureValid = await this.envelopeIntegrityBroker.VerifyAsync(
                envelope, eventName, EnvelopeDirection.Request);

            if (isSignatureValid is false)
            {
                throw new InvalidApprovalEventException(
                    message: "Invalid approval event. Integrity verification failed.");
            }
        }

        private static void ValidateAgainstStorageApprovalOnModify(
            Approval inputApproval,
            Approval storageApproval)
        {
            Validate(
                message: "Approval is invalid, fix the errors and try again.",
                (Rule: IsNotSame(
                        firstDate: inputApproval.CreatedWhen,
                        secondDate: storageApproval.CreatedWhen,
                        secondDateName: nameof(Approval.CreatedWhen)),
                    Parameter: nameof(Approval.CreatedWhen)),
                (Rule: IsNotSame(
                        first: inputApproval.CreatedBy,
                        second: storageApproval.CreatedBy,
                        secondName: nameof(Approval.CreatedBy)),
                    Parameter: nameof(Approval.CreatedBy)),
                (Rule: IsSame(
                        firstDate: inputApproval.UpdatedWhen,
                        secondDate: storageApproval.UpdatedWhen,
                        secondDateName: nameof(Approval.UpdatedWhen)),
                    Parameter: nameof(Approval.UpdatedWhen)),

                // EntityType and EntityId are IDENTITY, not content: they say which row this
                // approval is about, and an approval must not be repointable at a different
                // entity. Unpinned, a caller authorized for the approval as it stands could
                // move it onto something else in the same write — and the tier-2 gate above,
                // which asks about the STORED row, would have answered for the old target.
                //
                // ApprovalStatus is deliberately NOT pinned. §14.7 posture D rule 3 has
                // reviewers move the status through this very path; pinning it would refuse the
                // operation's purpose. What narrows that is the authorization gate, not a pin.
                (Rule: IsNotSame(
                        first: inputApproval.EntityType,
                        second: storageApproval.EntityType,
                        secondName: nameof(Approval.EntityType)),
                    Parameter: nameof(Approval.EntityType)),

                (Rule: IsNotSame(
                        first: inputApproval.EntityId,
                        second: storageApproval.EntityId,
                        secondName: nameof(Approval.EntityId)),
                    Parameter: nameof(Approval.EntityId)));
        }

        private static void ValidateOnRetrieveApprovalById(Guid approvalId) =>
            Validate(
                message: "Approval is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalId), Parameter: nameof(Approval.Id)));

        // the deletion reason is caller-supplied free text that lands on the row unchanged,
        // so its storage cap is enforced here rather than left to the column to reject
        private static void ValidateOnRemoveApprovalById(Guid approvalId, string? deletionReason) =>
            Validate(
                message: "Approval is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalId), Parameter: nameof(Approval.Id)),

                (Rule: IsGreaterThan(deletionReason, 500),
                    Parameter: nameof(Approval.DeletionReason)));

        private static void ValidateOnHardRemoveApprovalById(Guid approvalId) =>
            Validate(
                message: "Approval is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalId), Parameter: nameof(Approval.Id)));

        private static void ValidateStorageApproval(Approval maybeApproval, Guid approvalId)
        {
            if (maybeApproval is null)
            {
                throw new NotFoundApprovalException(
                    message: $"Approval not found with id: {approvalId}.");
            }
        }

        private static void ValidateApprovalIsNotNull(Approval approval)
        {
            if (approval is null)
            {
                throw new NullApprovalException(message: "Approval is null.");
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
            Guid first,
            Guid second,
            string secondName) => new
            {
                Condition = first != second,
                Message = $"Id is not the same as {secondName}"
            };

        private static dynamic IsNotSame(
            EntityType first,
            EntityType second,
            string secondName) => new
            {
                Condition = first != second,
                Message = $"Value is not the same as {secondName}"
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

        private static dynamic IsSame(
            DateTimeOffset firstDate,
            DateTimeOffset secondDate,
            string secondDateName) => new
            {
                Condition = firstDate == secondDate,
                Message = $"Date is the same as {secondDateName}"
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
            var invalidApprovalException = new InvalidApprovalException(message);

            foreach ((dynamic rule, string parameter) in validations)
            {
                if (rule.Condition)
                {
                    invalidApprovalException.UpsertDataList(
                        key: parameter,
                        value: rule.Message);
                }
            }

            invalidApprovalException.ThrowIfContainsErrors();
        }
    }
}
