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
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Orchestrations.ContentItems.Exceptions;
using Glory2Him.Core.Models.Securities;

namespace Glory2Him.Core.Services.Orchestrations.ContentItems
{
    internal partial class ContentItemOrchestrationService
    {
        private async ValueTask ValidateOnAddContentItemAsync(ContentItem contentItem)
        {
            await ValidateUserIsAllowedToContributeAsync();
            ValidateContentItemIsNotNull(contentItem);
            ValidateContentItem(contentItem);
        }

        private async ValueTask ValidateUserIsAllowedToContributeAsync()
        {
            bool isAuthenticated = await this.securityBroker.IsCurrentUserAuthenticatedAsync();

            if (isAuthenticated is false)
            {
                throw new UnauthorizedContentItemOrchestrationException(
                    message: "The current user is not authenticated.");
            }

            bool isBlocked =
                await this.securityBroker.IsInRoleAsync(Roles.ReadOnly)
                    || await this.securityBroker.IsInRoleAsync(Roles.ContentItemReadOnly);

            if (isBlocked)
            {
                throw new UnauthorizedContentItemOrchestrationException(
                    message: "The current user is blocked from contributing content items.");
            }
        }

        private static void ValidateContentItemIsNotNull(ContentItem contentItem)
        {
            if (contentItem is null)
            {
                throw new NullContentItemOrchestrationException(message: "Content item is null.");
            }
        }

        private static void ValidateContentItem(ContentItem contentItem) =>
            Validate(
                message: "Content item is invalid, fix the errors and try again.",
                (Rule: IsInvalid(contentItem.ContentTypeId), Parameter: nameof(ContentItem.ContentTypeId)),
                (Rule: IsInvalid(contentItem.Content), Parameter: nameof(ContentItem.Content)));

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

        private static void Validate(
            string message,
            params (dynamic Rule, string Parameter)[] validations)
        {
            var invalidContentItemOrchestrationException =
                new InvalidContentItemOrchestrationException(message);

            foreach ((dynamic rule, string parameter) in validations)
            {
                if (rule.Condition)
                {
                    invalidContentItemOrchestrationException.UpsertDataList(
                        key: parameter,
                        value: rule.Message);
                }
            }

            invalidContentItemOrchestrationException.ThrowIfContainsErrors();
        }
    }
}
