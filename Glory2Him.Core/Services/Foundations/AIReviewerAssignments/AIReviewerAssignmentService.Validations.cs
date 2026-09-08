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
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments;
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments.Exceptions;
using Glory2Him.Core.Models.Securities;

namespace Glory2Him.Core.Services.Foundations.AIReviewerAssignments
{
    internal partial class AIReviewerAssignmentService
    {
        // the §16.6 scoped-role suffixes; the entity prefix in front of them varies per
        // entity type, so only the suffix is a fixed part of the convention
        private const string ScopedReviewerRoleSuffix = Roles.ReviewersSuffix;
        private const string ScopedPublisherRoleSuffix = Roles.PublishersSuffix;

        // the foundation enforces the same security rules as the orchestration (design
        // §14.6): an exposer may bind to either service directly, so no layer may assume
        // an upstream layer already gated the caller

        // The half of the gate below that is NOT about the review tier: authenticated, and not
        // under the block role. Split out because the workflow's return-to-pending transition
        // reuses exactly this half and deliberately skips the other one — the system identity it
        // runs under holds no roles, so a tier test would refuse the only caller that verb has
        // (see AIReviewerAssignmentService.Transitions.cs).
        //
        // The ReadOnly message still says "managing", and stays word for word what it was:
        // managing is the only kind of write this entity has, so it is the sentence the caller
        // already receives, and re-wording it for the sake of the split would change an answer
        // nobody's request had changed.
        private static void ValidateUserIsAllowedToContribute(SecurityContext securityContext)
        {
            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                throw new UnauthorizedAIReviewerAssignmentException(
                    message: "The current user is not authenticated.");
            }

            if (securityContext.Roles.Contains(Roles.ReadOnly))
            {
                throw new UnauthorizedAIReviewerAssignmentException(
                    message: "The current user is blocked from managing AI reviewer assignments.");
            }
        }

        // ONE gate for Add, Modify and Remove — unlike ApprovalReviewRequest, which narrows
        // withdrawal to the review tier but leaves the door open in principle for a future
        // owner-only nuance. There is no owner nuance to reserve room for here: Berean has no
        // "requester" a different rule could ever apply to, so one gate serves every write.
        private static void ValidateUserIsAllowedToManageAIReviewerAssignments(
            SecurityContext securityContext)
        {
            ValidateUserIsAllowedToContribute(securityContext);

            if (HasReviewRole(securityContext) is false)
            {
                throw new UnauthorizedAIReviewerAssignmentException(
                    message: "The current user is not allowed to manage AI reviewer assignments.");
            }
        }

        // The review roles that may assign, amend, remove and read Berean's assignment: the
        // global Reviewers, Publishers and Administrators roles plus — by the §16.6 naming
        // convention — any entity-scoped "%EntityType%-Reviewers"/"%EntityType%-Publishers" role.
        // Copied from ApprovalReviewRequestService rather than shared, because the two entities
        // are not permitted to depend on one another (§14.6 rule 3: a foundation calls only
        // brokers) — see the-standard-foundations skill rule ts-foundations-003.
        private static bool HasReviewRole(SecurityContext securityContext) =>
            securityContext.Roles.Contains(Roles.Reviewers)
                || securityContext.Roles.Contains(Roles.Publishers)
                || securityContext.Roles.Contains(Roles.Administrators)
                || securityContext.Roles.Any(role =>
                    role.EndsWith(ScopedReviewerRoleSuffix, StringComparison.Ordinal)
                        || role.EndsWith(ScopedPublisherRoleSuffix, StringComparison.Ordinal));

        private async ValueTask ValidateOnAddAIReviewerAssignmentAsync(
            AIReviewerAssignment aiReviewerAssignment,
            SecurityContext securityContext)
        {
            ValidateAIReviewerAssignmentIsNotNull(aiReviewerAssignment);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "AI reviewer assignment is invalid, fix the errors and try again.",
                (Rule: IsInvalid(aiReviewerAssignment.Id),
                    Parameter: nameof(AIReviewerAssignment.Id)),

                (Rule: IsInvalid(aiReviewerAssignment.ApprovalId),
                    Parameter: nameof(AIReviewerAssignment.ApprovalId)),

                (Rule: IsInvalid(aiReviewerAssignment.CreatedBy),
                    Parameter: nameof(AIReviewerAssignment.CreatedBy)),

                (Rule: IsInvalid(aiReviewerAssignment.UpdatedBy),
                    Parameter: nameof(AIReviewerAssignment.UpdatedBy)),

                (Rule: IsInvalid(aiReviewerAssignment.CreatedWhen),
                    Parameter: nameof(AIReviewerAssignment.CreatedWhen)),

                (Rule: IsInvalid(aiReviewerAssignment.UpdatedWhen),
                    Parameter: nameof(AIReviewerAssignment.UpdatedWhen)),

                (Rule: IsGreaterThan(aiReviewerAssignment.CreatedBy, 255),
                    Parameter: nameof(AIReviewerAssignment.CreatedBy)),

                (Rule: IsGreaterThan(aiReviewerAssignment.UpdatedBy, 255),
                    Parameter: nameof(AIReviewerAssignment.UpdatedBy)),

                (Rule: IsNotSame(
                        firstDate: aiReviewerAssignment.UpdatedWhen,
                        secondDate: aiReviewerAssignment.CreatedWhen,
                        secondDateName: nameof(AIReviewerAssignment.CreatedWhen)),
                    Parameter: nameof(AIReviewerAssignment.UpdatedWhen)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: aiReviewerAssignment.CreatedBy),
                    Parameter: nameof(AIReviewerAssignment.CreatedBy)),

                (Rule: IsNotSame(
                        first: aiReviewerAssignment.UpdatedBy,
                        second: aiReviewerAssignment.CreatedBy,
                        secondName: nameof(AIReviewerAssignment.CreatedBy)),
                    Parameter: nameof(AIReviewerAssignment.UpdatedBy)),

                // A NEW ASSIGNMENT IS PENDING, FULL STOP. Both flags are system-set-only and start
                // false (§8.6.2: they record what Berean's pass eventually did, and it has not run
                // yet), so a row arriving already completed claims work that never happened and one
                // arriving already flagged claims comments Berean never filed. The orchestration's
                // upsert creates with neither set, but it is not the gate: this operation has its
                // own event address (§14.6).
                (Rule: IsSetOnAdd(aiReviewerAssignment.IsAIReviewCompleted),
                    Parameter: nameof(AIReviewerAssignment.IsAIReviewCompleted)),

                (Rule: IsSetOnAdd(aiReviewerAssignment.IsAIReviewCommentsPresent),
                    Parameter: nameof(AIReviewerAssignment.IsAIReviewCommentsPresent)),

                // The pairing invariant below, restated on the add path. Strictly redundant today —
                // the two rules above already refuse either flag on its own, so nothing can reach
                // this one — and kept anyway because it is an invariant of the ROW rather than of
                // the operation: it must hold wherever the row is written, and it is the half that
                // would still have to stand if the birth-state rules were ever relaxed for a system
                // re-import. A caller who sends the impossible pair is told both things.
                (Rule: IsCommentsPresentWithoutCompletion(
                        isAIReviewCompleted: aiReviewerAssignment.IsAIReviewCompleted,
                        isAIReviewCommentsPresent: aiReviewerAssignment.IsAIReviewCommentsPresent),
                    Parameter: nameof(AIReviewerAssignment.IsAIReviewCommentsPresent)),

                (Rule: await IsNotRecentAsync(aiReviewerAssignment.CreatedWhen),
                    Parameter: nameof(AIReviewerAssignment.CreatedWhen)));
        }

        private async ValueTask ValidateOnModifyAIReviewerAssignmentAsync(
            AIReviewerAssignment aiReviewerAssignment,
            SecurityContext securityContext)
        {
            ValidateAIReviewerAssignmentIsNotNull(aiReviewerAssignment);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "AI reviewer assignment is invalid, fix the errors and try again.",
                (Rule: IsInvalid(aiReviewerAssignment.Id),
                    Parameter: nameof(AIReviewerAssignment.Id)),

                (Rule: IsInvalid(aiReviewerAssignment.ApprovalId),
                    Parameter: nameof(AIReviewerAssignment.ApprovalId)),

                (Rule: IsInvalid(aiReviewerAssignment.CreatedBy),
                    Parameter: nameof(AIReviewerAssignment.CreatedBy)),

                (Rule: IsInvalid(aiReviewerAssignment.UpdatedBy),
                    Parameter: nameof(AIReviewerAssignment.UpdatedBy)),

                (Rule: IsInvalid(aiReviewerAssignment.CreatedWhen),
                    Parameter: nameof(AIReviewerAssignment.CreatedWhen)),

                (Rule: IsInvalid(aiReviewerAssignment.UpdatedWhen),
                    Parameter: nameof(AIReviewerAssignment.UpdatedWhen)),

                (Rule: IsGreaterThan(aiReviewerAssignment.CreatedBy, 255),
                    Parameter: nameof(AIReviewerAssignment.CreatedBy)),

                (Rule: IsGreaterThan(aiReviewerAssignment.UpdatedBy, 255),
                    Parameter: nameof(AIReviewerAssignment.UpdatedBy)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: aiReviewerAssignment.UpdatedBy),
                    Parameter: nameof(AIReviewerAssignment.UpdatedBy)),

                (Rule: IsSame(
                        firstDate: aiReviewerAssignment.UpdatedWhen,
                        secondDate: aiReviewerAssignment.CreatedWhen,
                        secondDateName: nameof(AIReviewerAssignment.CreatedWhen)),
                    Parameter: nameof(AIReviewerAssignment.UpdatedWhen)),

                // IsAIReviewCommentsPresent RECORDS SOMETHING BEREAN LEFT BEHIND, so it cannot
                // stand on a row that says the pass never finished — which is why the
                // orchestration's re-request clears the two together rather than one at a time.
                // Asked of the input alone rather than against storage: a modify writes the whole
                // row, so the pairing the caller sends is the pairing that lands, and pinning it to
                // the stored pair would refuse the legitimate both-to-false reset.
                (Rule: IsCommentsPresentWithoutCompletion(
                        isAIReviewCompleted: aiReviewerAssignment.IsAIReviewCompleted,
                        isAIReviewCommentsPresent: aiReviewerAssignment.IsAIReviewCommentsPresent),
                    Parameter: nameof(AIReviewerAssignment.IsAIReviewCommentsPresent)),

                (Rule: await IsNotRecentAsync(aiReviewerAssignment.UpdatedWhen),
                    Parameter: nameof(AIReviewerAssignment.UpdatedWhen)));
        }

        // A ROW'S SCOPE IS FIXED AT CREATION — which approval it was assigned against. Pinned
        // against storage rather than accepted from the caller: the filtered unique index allows
        // one live row per approval, so moving a row to another approval is authoring a
        // different assignment, not amending this one. Only the two system-facing bools and the
        // audit trail may change on a modify.
        private static void ValidateAgainstStorageAIReviewerAssignmentOnModify(
            AIReviewerAssignment inputAIReviewerAssignment,
            AIReviewerAssignment storageAIReviewerAssignment)
        {
            Validate(
                message: "AI reviewer assignment is invalid, fix the errors and try again.",
                (Rule: IsNotSame(
                        first: inputAIReviewerAssignment.ApprovalId,
                        second: storageAIReviewerAssignment.ApprovalId,
                        secondName: nameof(AIReviewerAssignment.ApprovalId)),
                    Parameter: nameof(AIReviewerAssignment.ApprovalId)),

                (Rule: IsNotSame(
                        firstDate: inputAIReviewerAssignment.CreatedWhen,
                        secondDate: storageAIReviewerAssignment.CreatedWhen,
                        secondDateName: nameof(AIReviewerAssignment.CreatedWhen)),
                    Parameter: nameof(AIReviewerAssignment.CreatedWhen)),

                (Rule: IsNotSame(
                        first: inputAIReviewerAssignment.CreatedBy,
                        second: storageAIReviewerAssignment.CreatedBy,
                        secondName: nameof(AIReviewerAssignment.CreatedBy)),
                    Parameter: nameof(AIReviewerAssignment.CreatedBy)),

                (Rule: IsSame(
                        firstDate: inputAIReviewerAssignment.UpdatedWhen,
                        secondDate: storageAIReviewerAssignment.UpdatedWhen,
                        secondDateName: nameof(AIReviewerAssignment.UpdatedWhen)),
                    Parameter: nameof(AIReviewerAssignment.UpdatedWhen)));
        }

        // Null-check first (a malformed event), then verify the integrity signature against the
        // event name this handler serves and the request direction. The signature is what makes
        // the envelope's SecurityContext trustworthy on the event path: without it a caller who can
        // put a message on this address states their own identity and roles and is believed
        // (design §14.6 rule 4). Verification sits in the receiver, not the transport, because a
        // handler is reachable without going through the broker.
        private async ValueTask ValidateAIReviewerAssignmentEventEnvelopeAsync(
            EventEnvelope<AIReviewerAssignment> envelope,
            AIReviewerAssignmentEventOperation operation)
        {
            if (envelope is null || envelope.Content is null || envelope.Metadata is null)
            {
                throw new InvalidAIReviewerAssignmentEventException(
                    message: "Invalid AI reviewer assignment event. " +
                        "The event envelope, its content and metadata are required.");
            }

            string eventName = $"{nameof(AIReviewerAssignment)}{operation}";

            bool isSignatureValid = await this.envelopeIntegrityBroker.VerifyAsync(
                envelope, eventName, EnvelopeDirection.Request);

            if (isSignatureValid is false)
            {
                throw new InvalidAIReviewerAssignmentEventException(
                    message: "Invalid AI reviewer assignment event. Integrity verification failed.");
            }
        }

        private static void ValidateOnRetrieveAIReviewerAssignmentById(Guid aiReviewerAssignmentId) =>
            Validate(
                message: "AI reviewer assignment is invalid, fix the errors and try again.",
                (Rule: IsInvalid(aiReviewerAssignmentId), Parameter: nameof(AIReviewerAssignment.Id)));

        // the deletion reason is caller-supplied free text that lands on the row unchanged,
        // so its storage cap is enforced here rather than left to the column to reject
        private static void ValidateOnRemoveAIReviewerAssignmentById(
            Guid aiReviewerAssignmentId,
            string? deletionReason) =>
            Validate(
                message: "AI reviewer assignment is invalid, fix the errors and try again.",
                (Rule: IsInvalid(aiReviewerAssignmentId), Parameter: nameof(AIReviewerAssignment.Id)),

                (Rule: IsGreaterThan(deletionReason, 500),
                    Parameter: nameof(AIReviewerAssignment.DeletionReason)));

        private static void ValidateStorageAIReviewerAssignment(
            AIReviewerAssignment maybeAIReviewerAssignment,
            Guid aiReviewerAssignmentId)
        {
            if (maybeAIReviewerAssignment is null)
            {
                throw new NotFoundAIReviewerAssignmentException(
                    message: $"AI reviewer assignment not found with id: {aiReviewerAssignmentId}.");
            }
        }

        // Reported as not-found rather than as a distinct "deleted" error, matching the read
        // posture above: a removed id must not be distinguishable from one that never existed, or
        // a modify becomes a probe for which assignments used to exist. Silent, unlike the read
        // path's guard — a not-found answer to a READ hides a real denial reason that has to stay
        // recoverable server-side, while a refused write hides nothing.
        private static void ValidateStorageAIReviewerAssignmentIsNotDeleted(
            AIReviewerAssignment storageAIReviewerAssignment,
            Guid aiReviewerAssignmentId)
        {
            if (storageAIReviewerAssignment.IsDeleted)
            {
                throw new NotFoundAIReviewerAssignmentException(
                    message: $"AI reviewer assignment not found with id: {aiReviewerAssignmentId}.");
            }
        }

        private static void ValidateAIReviewerAssignmentIsNotNull(
            AIReviewerAssignment aiReviewerAssignment)
        {
            if (aiReviewerAssignment is null)
            {
                throw new NullAIReviewerAssignmentException(
                    message: "AI reviewer assignment is null.");
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
            Guid first,
            Guid second,
            string secondName) => new
            {
                Condition = first != second,
                Message = $"Value is not the same as {secondName}"
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

        private static dynamic IsSame(
            DateTimeOffset firstDate,
            DateTimeOffset secondDate,
            string secondDateName) => new
            {
                Condition = firstDate == secondDate,
                Message = $"Date is the same as {secondDateName}"
            };

        private static dynamic IsGreaterThan(string? text, int maxLength) => new
        {
            Condition = (text ?? string.Empty).Length > maxLength,
            Message = $"Text exceed max length of {maxLength} characters"
        };

        private static dynamic IsSetOnAdd(bool value) => new
        {
            Condition = value,
            Message = "Value is not allowed on add"
        };

        private static dynamic IsCommentsPresentWithoutCompletion(
            bool isAIReviewCompleted,
            bool isAIReviewCommentsPresent) => new
            {
                Condition = isAIReviewCommentsPresent && isAIReviewCompleted is false,
                Message = "Comments present requires a completed AI review."
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
            var invalidAIReviewerAssignmentException = new InvalidAIReviewerAssignmentException(message);

            foreach ((dynamic rule, string parameter) in validations)
            {
                if (rule.Condition)
                {
                    invalidAIReviewerAssignmentException.UpsertDataList(
                        key: parameter,
                        value: rule.Message);
                }
            }

            invalidAIReviewerAssignmentException.ThrowIfContainsErrors();
        }
    }
}
