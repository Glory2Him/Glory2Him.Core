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
using System.Threading.Tasks;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Comments;
using Glory2Him.Core.Models.Foundations.Comments.Exceptions;

namespace Glory2Him.Core.Services.Foundations.Comments
{
    internal partial class CommentService
    {
        private async ValueTask ValidateOnAddCommentAsync(
            Comment comment,
            SecurityContext securityContext)
        {
            ValidateCommentIsNotNull(comment);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Comment is invalid, fix the errors and try again.",
                (Rule: IsInvalid(comment.Id), Parameter: nameof(Comment.Id)),
                (Rule: IsInvalid(comment.Content), Parameter: nameof(Comment.Content)),
                (Rule: IsInvalid(comment.CreatedBy), Parameter: nameof(Comment.CreatedBy)),
                (Rule: IsInvalid(comment.UpdatedBy), Parameter: nameof(Comment.UpdatedBy)),
                (Rule: IsInvalid(comment.CreatedWhen), Parameter: nameof(Comment.CreatedWhen)),
                (Rule: IsInvalid(comment.UpdatedWhen), Parameter: nameof(Comment.UpdatedWhen)),

                (Rule: IsGreaterThan(comment.CreatedBy, 255),
                    Parameter: nameof(Comment.CreatedBy)),

                (Rule: IsGreaterThan(comment.UpdatedBy, 255),
                    Parameter: nameof(Comment.UpdatedBy)),

                (Rule: IsNotSame(
                        firstDate: comment.UpdatedWhen,
                        secondDate: comment.CreatedWhen,
                        secondDateName: nameof(Comment.CreatedWhen)),
                    Parameter: nameof(Comment.UpdatedWhen)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: comment.CreatedBy),
                    Parameter: nameof(Comment.CreatedBy)),

                (Rule: IsNotSame(
                        first: comment.UpdatedBy,
                        second: comment.CreatedBy,
                        secondName: nameof(Comment.CreatedBy)),
                    Parameter: nameof(Comment.UpdatedBy)),

                (Rule: await IsNotRecentAsync(comment.CreatedWhen),
                    Parameter: nameof(Comment.CreatedWhen)));
        }

        private async ValueTask ValidateOnModifyCommentAsync(
            Comment comment,
            SecurityContext securityContext)
        {
            ValidateCommentIsNotNull(comment);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Comment is invalid, fix the errors and try again.",
                (Rule: IsInvalid(comment.Id), Parameter: nameof(Comment.Id)),
                (Rule: IsInvalid(comment.Content), Parameter: nameof(Comment.Content)),
                (Rule: IsInvalid(comment.CreatedBy), Parameter: nameof(Comment.CreatedBy)),
                (Rule: IsInvalid(comment.UpdatedBy), Parameter: nameof(Comment.UpdatedBy)),
                (Rule: IsInvalid(comment.CreatedWhen), Parameter: nameof(Comment.CreatedWhen)),
                (Rule: IsInvalid(comment.UpdatedWhen), Parameter: nameof(Comment.UpdatedWhen)),

                (Rule: IsGreaterThan(comment.CreatedBy, 255),
                    Parameter: nameof(Comment.CreatedBy)),

                (Rule: IsGreaterThan(comment.UpdatedBy, 255),
                    Parameter: nameof(Comment.UpdatedBy)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: comment.UpdatedBy),
                    Parameter: nameof(Comment.UpdatedBy)),

                (Rule: IsSame(
                        firstDate: comment.UpdatedWhen,
                        secondDate: comment.CreatedWhen,
                        secondDateName: nameof(Comment.CreatedWhen)),
                    Parameter: nameof(Comment.UpdatedWhen)),

                (Rule: await IsNotRecentAsync(comment.UpdatedWhen),
                    Parameter: nameof(Comment.UpdatedWhen)));
        }

        private static void ValidateCommentEventEnvelope(EventEnvelope<Comment> envelope)
        {
            if (envelope is null || envelope.Content is null || envelope.Metadata is null)
            {
                throw new InvalidCommentEventException(
                    message: "Invalid comment event. " +
                        "The event envelope, its content and metadata are required.");
            }
        }

        private static void ValidateAgainstStorageCommentOnModify(
            Comment inputComment,
            Comment storageComment)
        {
            Validate(
                message: "Comment is invalid, fix the errors and try again.",
                (Rule: IsNotSame(
                        firstDate: inputComment.CreatedWhen,
                        secondDate: storageComment.CreatedWhen,
                        secondDateName: nameof(Comment.CreatedWhen)),
                    Parameter: nameof(Comment.CreatedWhen)),
                (Rule: IsNotSame(
                        first: inputComment.CreatedBy,
                        second: storageComment.CreatedBy,
                        secondName: nameof(Comment.CreatedBy)),
                    Parameter: nameof(Comment.CreatedBy)),
                (Rule: IsSame(
                        firstDate: inputComment.UpdatedWhen,
                        secondDate: storageComment.UpdatedWhen,
                        secondDateName: nameof(Comment.UpdatedWhen)),
                    Parameter: nameof(Comment.UpdatedWhen)));
        }

        private static void ValidateOnRetrieveCommentById(Guid commentId) =>
            Validate(
                message: "Comment is invalid, fix the errors and try again.",
                (Rule: IsInvalid(commentId), Parameter: nameof(Comment.Id)));

        private static void ValidateOnRemoveCommentById(Guid commentId) =>
            Validate(
                message: "Comment is invalid, fix the errors and try again.",
                (Rule: IsInvalid(commentId), Parameter: nameof(Comment.Id)));

        private static void ValidateOnHardRemoveCommentById(Guid commentId) =>
            Validate(
                message: "Comment is invalid, fix the errors and try again.",
                (Rule: IsInvalid(commentId), Parameter: nameof(Comment.Id)));

        private static void ValidateStorageComment(Comment maybeComment, Guid commentId)
        {
            if (maybeComment is null)
            {
                throw new NotFoundCommentException(
                    message: $"Comment not found with id: {commentId}.");
            }
        }

        private static void ValidateCommentIsNotNull(Comment comment)
        {
            if (comment is null)
            {
                throw new NullCommentException(message: "Comment is null.");
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
            var invalidCommentException = new InvalidCommentException(message);

            foreach ((dynamic rule, string parameter) in validations)
            {
                if (rule.Condition)
                {
                    invalidCommentException.UpsertDataList(
                        key: parameter,
                        value: rule.Message);
                }
            }

            invalidCommentException.ThrowIfContainsErrors();
        }
    }
}
