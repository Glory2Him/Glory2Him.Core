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
using Glory2Him.Core.Models.Foundations.LiveUpdates;
using Glory2Him.Core.Models.Foundations.LiveUpdates.Exceptions;

namespace Glory2Him.Core.Services.Foundations.LiveUpdates
{
    internal partial class LiveUpdateService
    {
        private static void ValidateLiveUpdateOnSend(LiveUpdate liveUpdate)
        {
            ValidateLiveUpdateIsNotNull(liveUpdate);

            Validate(
                message: "Live update is invalid, fix the errors and try again.",
                (Rule: IsInvalid(liveUpdate.Type), Parameter: nameof(LiveUpdate.Type)),

                (Rule: IsSetOnASettingChange(liveUpdate.Type, liveUpdate.ContentItemId),
                    Parameter: nameof(LiveUpdate.ContentItemId)));
        }

        private static void ValidateLiveUpdateIsNotNull(LiveUpdate liveUpdate)
        {
            if (liveUpdate is null)
            {
                throw new NullLiveUpdateException(message: "Live update is null.");
            }
        }

        private static dynamic IsInvalid(LiveUpdateType liveUpdateType) => new
        {
            Condition = Enum.IsDefined(liveUpdateType) is false,
            Message = "Value is not a supported live update type"
        };

        // a setting message names nothing else: a page reads every setting it shows again
        private static dynamic IsSetOnASettingChange(
            LiveUpdateType liveUpdateType,
            Guid? contentItemId) => new
            {
                Condition = liveUpdateType == LiveUpdateType.ContentItemSettingChanged
                    && contentItemId is not null,

                Message = "Value is not allowed on a content item setting change"
            };

        private static void Validate(
            string message,
            params (dynamic Rule, string Parameter)[] validations)
        {
            var invalidLiveUpdateException = new InvalidLiveUpdateException(message);

            foreach ((dynamic rule, string parameter) in validations)
            {
                if (rule.Condition)
                {
                    invalidLiveUpdateException.UpsertDataList(
                        key: parameter,
                        value: rule.Message);
                }
            }

            invalidLiveUpdateException.ThrowIfContainsErrors();
        }
    }
}
