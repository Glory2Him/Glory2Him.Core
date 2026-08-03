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
using Glory2Him.Core.Models.Foundations.Reactions;
using Glory2Him.Core.Models.Foundations.Reactions.Exceptions;
using Glory2Him.Core.Models.Securities;

namespace Glory2Him.Core.Services.Foundations.Reactions
{
    internal partial class ReactionService
    {
        // the foundation enforces the same security rules as the orchestration (design
        // §14.6): an exposer may bind to either service directly, so no layer may assume
        // an upstream layer already gated the caller

        private static void ValidateUserIsAllowedToContribute(SecurityContext securityContext)
        {
            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                throw new UnauthorizedReactionException(
                    message: "The current user is not authenticated.");
            }

            bool isBlocked =
                securityContext.Roles.Contains(Roles.ReadOnly)
                    || securityContext.Roles.Contains(Roles.ReactionReadOnly);

            if (isBlocked)
            {
                throw new UnauthorizedReactionException(
                    message: "The current user is blocked from contributing reactions.");
            }
        }

        // the moderation roles that may act on and read non-public versions for review and
        // audit (Reviewer, Publisher, Admin — global or Reaction-scoped, §16.6)
        private static bool HasReviewRole(SecurityContext securityContext) =>
            securityContext.Roles.Contains(Roles.Reviewer)
                || securityContext.Roles.Contains(Roles.ReactionReviewer)
                || securityContext.Roles.Contains(Roles.Publisher)
                || securityContext.Roles.Contains(Roles.ReactionPublisher)
                || securityContext.Roles.Contains(Roles.Admin);

        // row-level write permission: the owner or a review role may write the row — the
        // narrower process rules stay in the orchestration
        private async ValueTask ValidateUserCanModifyStorageReactionAsync(
            Reaction storageReaction,
            SecurityContext securityContext)
        {
            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && storageReaction.CreatedBy == actorUserId;

            if (isOwner is false && HasReviewRole(securityContext) is false)
            {
                throw new UnauthorizedReactionException(
                    message: "The current user is not allowed to modify this reaction.");
            }
        }

        // removing content is a takedown, not a moderation step — the owner may remove
        // their own reaction and an Admin may remove anyone's; Reviewers and Publishers
        // moderate through the approval workflow instead
        private async ValueTask ValidateUserCanRemoveStorageReactionAsync(
            Reaction storageReaction,
            SecurityContext securityContext)
        {
            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && storageReaction.CreatedBy == actorUserId;

            if (isOwner is false && securityContext.Roles.Contains(Roles.Admin) is false)
            {
                throw new UnauthorizedReactionException(
                    message: "The current user is not allowed to remove this reaction.");
            }
        }

        // a hard remove destroys the row and its audit trail — Admin only
        private static void ValidateUserCanHardRemoveReaction(SecurityContext securityContext)
        {
            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                throw new UnauthorizedReactionException(
                    message: "The current user is not authenticated.");
            }

            if (securityContext.Roles.Contains(Roles.Admin) is false)
            {
                throw new UnauthorizedReactionException(
                    message: "The current user is not allowed to permanently remove this reaction.");
            }
        }

        private async ValueTask ValidateOnAddReactionAsync(
            Reaction reaction,
            SecurityContext securityContext)
        {
            ValidateReactionIsNotNull(reaction);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Reaction is invalid, fix the errors and try again.",
                (Rule: IsInvalid(reaction.Id), Parameter: nameof(Reaction.Id)),
                (Rule: IsInvalid(reaction.Name), Parameter: nameof(Reaction.Name)),
                (Rule: IsInvalid(reaction.UnicodeEmoji), Parameter: nameof(Reaction.UnicodeEmoji)),
                (Rule: IsInvalid(reaction.CreatedBy), Parameter: nameof(Reaction.CreatedBy)),
                (Rule: IsInvalid(reaction.UpdatedBy), Parameter: nameof(Reaction.UpdatedBy)),
                (Rule: IsInvalid(reaction.CreatedWhen), Parameter: nameof(Reaction.CreatedWhen)),
                (Rule: IsInvalid(reaction.UpdatedWhen), Parameter: nameof(Reaction.UpdatedWhen)),

                (Rule: IsGreaterThan(reaction.Name, 30),
                    Parameter: nameof(Reaction.Name)),

                (Rule: IsGreaterThan(reaction.UnicodeEmoji, 16),
                    Parameter: nameof(Reaction.UnicodeEmoji)),

                (Rule: IsGreaterThan(reaction.CreatedBy, 255),
                    Parameter: nameof(Reaction.CreatedBy)),

                (Rule: IsGreaterThan(reaction.UpdatedBy, 255),
                    Parameter: nameof(Reaction.UpdatedBy)),

                (Rule: IsNotSame(
                        firstDate: reaction.UpdatedWhen,
                        secondDate: reaction.CreatedWhen,
                        secondDateName: nameof(Reaction.CreatedWhen)),
                    Parameter: nameof(Reaction.UpdatedWhen)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: reaction.CreatedBy),
                    Parameter: nameof(Reaction.CreatedBy)),

                (Rule: IsNotSame(
                        first: reaction.UpdatedBy,
                        second: reaction.CreatedBy,
                        secondName: nameof(Reaction.CreatedBy)),
                    Parameter: nameof(Reaction.UpdatedBy)),

                (Rule: await IsNotRecentAsync(reaction.CreatedWhen),
                    Parameter: nameof(Reaction.CreatedWhen)));
        }

        private async ValueTask ValidateOnModifyReactionAsync(
            Reaction reaction,
            SecurityContext securityContext)
        {
            ValidateReactionIsNotNull(reaction);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Reaction is invalid, fix the errors and try again.",
                (Rule: IsInvalid(reaction.Id), Parameter: nameof(Reaction.Id)),
                (Rule: IsInvalid(reaction.Name), Parameter: nameof(Reaction.Name)),
                (Rule: IsInvalid(reaction.UnicodeEmoji), Parameter: nameof(Reaction.UnicodeEmoji)),
                (Rule: IsInvalid(reaction.CreatedBy), Parameter: nameof(Reaction.CreatedBy)),
                (Rule: IsInvalid(reaction.UpdatedBy), Parameter: nameof(Reaction.UpdatedBy)),
                (Rule: IsInvalid(reaction.CreatedWhen), Parameter: nameof(Reaction.CreatedWhen)),
                (Rule: IsInvalid(reaction.UpdatedWhen), Parameter: nameof(Reaction.UpdatedWhen)),

                (Rule: IsGreaterThan(reaction.Name, 30),
                    Parameter: nameof(Reaction.Name)),

                (Rule: IsGreaterThan(reaction.UnicodeEmoji, 16),
                    Parameter: nameof(Reaction.UnicodeEmoji)),

                (Rule: IsGreaterThan(reaction.CreatedBy, 255),
                    Parameter: nameof(Reaction.CreatedBy)),

                (Rule: IsGreaterThan(reaction.UpdatedBy, 255),
                    Parameter: nameof(Reaction.UpdatedBy)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: reaction.UpdatedBy),
                    Parameter: nameof(Reaction.UpdatedBy)),

                (Rule: IsSame(
                        firstDate: reaction.UpdatedWhen,
                        secondDate: reaction.CreatedWhen,
                        secondDateName: nameof(Reaction.CreatedWhen)),
                    Parameter: nameof(Reaction.UpdatedWhen)),

                (Rule: await IsNotRecentAsync(reaction.UpdatedWhen),
                    Parameter: nameof(Reaction.UpdatedWhen)));
        }

        private static void ValidateReactionEventEnvelope(EventEnvelope<Reaction> envelope)
        {
            if (envelope is null || envelope.Content is null || envelope.Metadata is null)
            {
                throw new InvalidReactionEventException(
                    message: "Invalid reaction event. " +
                        "The event envelope, its content and metadata are required.");
            }
        }

        private static void ValidateAgainstStorageReactionOnModify(
            Reaction inputReaction,
            Reaction storageReaction)
        {
            Validate(
                message: "Reaction is invalid, fix the errors and try again.",
                (Rule: IsNotSame(
                        firstDate: inputReaction.CreatedWhen,
                        secondDate: storageReaction.CreatedWhen,
                        secondDateName: nameof(Reaction.CreatedWhen)),
                    Parameter: nameof(Reaction.CreatedWhen)),
                (Rule: IsNotSame(
                        first: inputReaction.CreatedBy,
                        second: storageReaction.CreatedBy,
                        secondName: nameof(Reaction.CreatedBy)),
                    Parameter: nameof(Reaction.CreatedBy)),
                (Rule: IsSame(
                        firstDate: inputReaction.UpdatedWhen,
                        secondDate: storageReaction.UpdatedWhen,
                        secondDateName: nameof(Reaction.UpdatedWhen)),
                    Parameter: nameof(Reaction.UpdatedWhen)));
        }

        private static void ValidateOnRetrieveReactionById(Guid reactionId) =>
            Validate(
                message: "Reaction is invalid, fix the errors and try again.",
                (Rule: IsInvalid(reactionId), Parameter: nameof(Reaction.Id)));

        private static void ValidateOnRemoveReactionById(Guid reactionId) =>
            Validate(
                message: "Reaction is invalid, fix the errors and try again.",
                (Rule: IsInvalid(reactionId), Parameter: nameof(Reaction.Id)));

        private static void ValidateOnHardRemoveReactionById(Guid reactionId) =>
            Validate(
                message: "Reaction is invalid, fix the errors and try again.",
                (Rule: IsInvalid(reactionId), Parameter: nameof(Reaction.Id)));

        private static void ValidateStorageReaction(Reaction maybeReaction, Guid reactionId)
        {
            if (maybeReaction is null)
            {
                throw new NotFoundReactionException(
                    message: $"Reaction not found with id: {reactionId}.");
            }
        }

        private static void ValidateReactionIsNotNull(Reaction reaction)
        {
            if (reaction is null)
            {
                throw new NullReactionException(message: "Reaction is null.");
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

        private static dynamic IsGreaterThan(string text, int maxLength) => new
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
            var invalidReactionException = new InvalidReactionException(message);

            foreach ((dynamic rule, string parameter) in validations)
            {
                if (rule.Condition)
                {
                    invalidReactionException.UpsertDataList(
                        key: parameter,
                        value: rule.Message);
                }
            }

            invalidReactionException.ThrowIfContainsErrors();
        }
    }
}
