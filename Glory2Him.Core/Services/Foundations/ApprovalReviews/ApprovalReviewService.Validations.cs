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
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.ApprovalReviews.Exceptions;

namespace Glory2Him.Core.Services.Foundations.ApprovalReviews
{
    internal partial class ApprovalReviewService
    {
        private async ValueTask ValidateOnAddApprovalReviewAsync(
            ApprovalReview approvalReview,
            SecurityContext securityContext)
        {
            ValidateApprovalReviewIsNotNull(approvalReview);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Approval review is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalReview.Id), Parameter: nameof(ApprovalReview.Id)),
                (Rule: IsInvalid(approvalReview.ApprovalId), Parameter: nameof(ApprovalReview.ApprovalId)),
                (Rule: IsInvalid(approvalReview.ReviewerId), Parameter: nameof(ApprovalReview.ReviewerId)),
                (Rule: IsInvalid(approvalReview.CreatedBy), Parameter: nameof(ApprovalReview.CreatedBy)),
                (Rule: IsInvalid(approvalReview.UpdatedBy), Parameter: nameof(ApprovalReview.UpdatedBy)),
                (Rule: IsInvalid(approvalReview.CreatedWhen), Parameter: nameof(ApprovalReview.CreatedWhen)),
                (Rule: IsInvalid(approvalReview.UpdatedWhen), Parameter: nameof(ApprovalReview.UpdatedWhen)),

                (Rule: IsGreaterThan(approvalReview.ReviewerId, 450),
                    Parameter: nameof(ApprovalReview.ReviewerId)),

                (Rule: IsGreaterThan(approvalReview.CreatedBy, 255),
                    Parameter: nameof(ApprovalReview.CreatedBy)),

                (Rule: IsGreaterThan(approvalReview.UpdatedBy, 255),
                    Parameter: nameof(ApprovalReview.UpdatedBy)),

                (Rule: IsNotSame(
                        firstDate: approvalReview.UpdatedWhen,
                        secondDate: approvalReview.CreatedWhen,
                        secondDateName: nameof(ApprovalReview.CreatedWhen)),
                    Parameter: nameof(ApprovalReview.UpdatedWhen)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: approvalReview.CreatedBy),
                    Parameter: nameof(ApprovalReview.CreatedBy)),

                (Rule: IsNotSame(
                        first: approvalReview.UpdatedBy,
                        second: approvalReview.CreatedBy,
                        secondName: nameof(ApprovalReview.CreatedBy)),
                    Parameter: nameof(ApprovalReview.UpdatedBy)),

                (Rule: await IsNotRecentAsync(approvalReview.CreatedWhen),
                    Parameter: nameof(ApprovalReview.CreatedWhen)));
        }

        private async ValueTask ValidateOnModifyApprovalReviewAsync(
            ApprovalReview approvalReview,
            SecurityContext securityContext)
        {
            ValidateApprovalReviewIsNotNull(approvalReview);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Approval review is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalReview.Id), Parameter: nameof(ApprovalReview.Id)),
                (Rule: IsInvalid(approvalReview.ApprovalId), Parameter: nameof(ApprovalReview.ApprovalId)),
                (Rule: IsInvalid(approvalReview.ReviewerId), Parameter: nameof(ApprovalReview.ReviewerId)),
                (Rule: IsInvalid(approvalReview.CreatedBy), Parameter: nameof(ApprovalReview.CreatedBy)),
                (Rule: IsInvalid(approvalReview.UpdatedBy), Parameter: nameof(ApprovalReview.UpdatedBy)),
                (Rule: IsInvalid(approvalReview.CreatedWhen), Parameter: nameof(ApprovalReview.CreatedWhen)),
                (Rule: IsInvalid(approvalReview.UpdatedWhen), Parameter: nameof(ApprovalReview.UpdatedWhen)),

                (Rule: IsGreaterThan(approvalReview.ReviewerId, 450),
                    Parameter: nameof(ApprovalReview.ReviewerId)),

                (Rule: IsGreaterThan(approvalReview.CreatedBy, 255),
                    Parameter: nameof(ApprovalReview.CreatedBy)),

                (Rule: IsGreaterThan(approvalReview.UpdatedBy, 255),
                    Parameter: nameof(ApprovalReview.UpdatedBy)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: approvalReview.UpdatedBy),
                    Parameter: nameof(ApprovalReview.UpdatedBy)),

                (Rule: IsSame(
                        firstDate: approvalReview.UpdatedWhen,
                        secondDate: approvalReview.CreatedWhen,
                        secondDateName: nameof(ApprovalReview.CreatedWhen)),
                    Parameter: nameof(ApprovalReview.UpdatedWhen)),

                (Rule: await IsNotRecentAsync(approvalReview.UpdatedWhen),
                    Parameter: nameof(ApprovalReview.UpdatedWhen)));
        }

        private static void ValidateApprovalReviewEventEnvelope(EventEnvelope<ApprovalReview> envelope)
        {
            if (envelope is null || envelope.Content is null || envelope.Metadata is null)
            {
                throw new InvalidApprovalReviewEventException(
                    message: "Invalid approval review event. " +
                        "The event envelope, its content and metadata are required.");
            }
        }

        private static void ValidateAgainstStorageApprovalReviewOnModify(
            ApprovalReview inputApprovalReview,
            ApprovalReview storageApprovalReview)
        {
            Validate(
                message: "Approval review is invalid, fix the errors and try again.",
                (Rule: IsNotSame(
                        firstDate: inputApprovalReview.CreatedWhen,
                        secondDate: storageApprovalReview.CreatedWhen,
                        secondDateName: nameof(ApprovalReview.CreatedWhen)),
                    Parameter: nameof(ApprovalReview.CreatedWhen)),
                (Rule: IsNotSame(
                        first: inputApprovalReview.CreatedBy,
                        second: storageApprovalReview.CreatedBy,
                        secondName: nameof(ApprovalReview.CreatedBy)),
                    Parameter: nameof(ApprovalReview.CreatedBy)),
                (Rule: IsSame(
                        firstDate: inputApprovalReview.UpdatedWhen,
                        secondDate: storageApprovalReview.UpdatedWhen,
                        secondDateName: nameof(ApprovalReview.UpdatedWhen)),
                    Parameter: nameof(ApprovalReview.UpdatedWhen)));
        }

        private static void ValidateOnRetrieveApprovalReviewById(Guid approvalReviewId) =>
            Validate(
                message: "Approval review is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalReviewId), Parameter: nameof(ApprovalReview.Id)));

        private static void ValidateOnRemoveApprovalReviewById(Guid approvalReviewId) =>
            Validate(
                message: "Approval review is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalReviewId), Parameter: nameof(ApprovalReview.Id)));

        private static void ValidateOnHardRemoveApprovalReviewById(Guid approvalReviewId) =>
            Validate(
                message: "Approval review is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalReviewId), Parameter: nameof(ApprovalReview.Id)));

        private static void ValidateStorageApprovalReview(ApprovalReview maybeApprovalReview, Guid approvalReviewId)
        {
            if (maybeApprovalReview is null)
            {
                throw new NotFoundApprovalReviewException(
                    message: $"Approval review not found with id: {approvalReviewId}.");
            }
        }

        private static void ValidateApprovalReviewIsNotNull(ApprovalReview approvalReview)
        {
            if (approvalReview is null)
            {
                throw new NullApprovalReviewException(message: "Approval review is null.");
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
            var invalidApprovalReviewException = new InvalidApprovalReviewException(message);

            foreach ((dynamic rule, string parameter) in validations)
            {
                if (rule.Condition)
                {
                    invalidApprovalReviewException.UpsertDataList(
                        key: parameter,
                        value: rule.Message);
                }
            }

            invalidApprovalReviewException.ThrowIfContainsErrors();
        }
    }
}
