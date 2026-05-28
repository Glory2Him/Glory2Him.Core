// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// https://mark.bible/mark-16-15
// ────────────────────────────────────────────────────────────────────────────────

using System;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;

namespace Glory2Him.Core.Services.Foundations.ContentItems
{
    public partial class ContentItemService
    {
        private static void ValidateOnAddContentItem(ContentItem contentItem)
        {
            ValidateContentItemIsNotNull(contentItem);

            Validate(
                message: "Content item is invalid, fix the errors and try again.",
                (Rule: IsInvalid(contentItem.Id), Parameter: nameof(ContentItem.Id)),
                (Rule: IsInvalid(contentItem.ContentTypeId), Parameter: nameof(ContentItem.ContentTypeId)),
                (Rule: IsInvalid(contentItem.ContentItemGroupId), Parameter: nameof(ContentItem.ContentItemGroupId)),
                (Rule: IsInvalid(contentItem.Content), Parameter: nameof(ContentItem.Content)),
                (Rule: IsInvalid(contentItem.CreatedBy), Parameter: nameof(ContentItem.CreatedBy)),
                (Rule: IsInvalid(contentItem.UpdatedBy), Parameter: nameof(ContentItem.UpdatedBy)),
                (Rule: IsInvalid(contentItem.CreatedWhen), Parameter: nameof(ContentItem.CreatedWhen)),
                (Rule: IsInvalid(contentItem.UpdatedWhen), Parameter: nameof(ContentItem.UpdatedWhen)));
        }

        private static void ValidateOnModifyContentItem(ContentItem contentItem)
        {
            ValidateContentItemIsNotNull(contentItem);

            Validate(
                message: "Content item is invalid, fix the errors and try again.",
                (Rule: IsInvalid(contentItem.Id), Parameter: nameof(ContentItem.Id)),
                (Rule: IsInvalid(contentItem.ContentTypeId), Parameter: nameof(ContentItem.ContentTypeId)),
                (Rule: IsInvalid(contentItem.ContentItemGroupId), Parameter: nameof(ContentItem.ContentItemGroupId)),
                (Rule: IsInvalid(contentItem.Content), Parameter: nameof(ContentItem.Content)),
                (Rule: IsInvalid(contentItem.CreatedBy), Parameter: nameof(ContentItem.CreatedBy)),
                (Rule: IsInvalid(contentItem.UpdatedBy), Parameter: nameof(ContentItem.UpdatedBy)),
                (Rule: IsInvalid(contentItem.CreatedWhen), Parameter: nameof(ContentItem.CreatedWhen)),
                (Rule: IsInvalid(contentItem.UpdatedWhen), Parameter: nameof(ContentItem.UpdatedWhen)));
        }

        private static void ValidateOnRetrieveContentItemById(Guid contentItemId) =>
            Validate(
                message: "Content item is invalid, fix the errors and try again.",
                (Rule: IsInvalid(contentItemId), Parameter: nameof(ContentItem.Id)));

        private static void ValidateOnRemoveContentItemById(Guid contentItemId) =>
            Validate(
                message: "Content item is invalid, fix the errors and try again.",
                (Rule: IsInvalid(contentItemId), Parameter: nameof(ContentItem.Id)));

        private static void ValidateContentItemIsNotNull(ContentItem contentItem)
        {
            if (contentItem is null)
            {
                throw new NullContentItemException(message: "Content item is null.");
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

        private static void Validate(
            string message,
            params (dynamic Rule, string Parameter)[] validations)
        {
            var invalidContentItemException = new InvalidContentItemException(message);

            foreach ((dynamic rule, string parameter) in validations)
            {
                if (rule.Condition)
                {
                    invalidContentItemException.UpsertDataList(
                        key: parameter,
                        value: rule.Message);
                }
            }

            invalidContentItemException.ThrowIfContainsErrors();
        }
    }
}
