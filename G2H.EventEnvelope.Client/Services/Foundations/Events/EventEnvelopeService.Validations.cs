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

using G2H.EventEnvelope.Client.Models.Foundations;
using G2H.EventEnvelope.Client.Models.Foundations.Exceptions;

namespace G2H.EventEnvelope.Client.Services.Foundations.Events
{
    internal partial class EventEnvelopeService
    {
        virtual internal void ValidateOnCreate<T>(T content)
        {
            Validate((Rule: IsInvalid(content), Parameter: "Content"));
        }

        virtual internal void ValidateOnCreateNext<TSource, T>(
            EventEnvelope<TSource> sourceEnvelope,
            T content)
        {
            Validate(
                (Rule: IsInvalid(sourceEnvelope), Parameter: "SourceEnvelope"),
                (Rule: IsInvalidMetadata(sourceEnvelope), Parameter: "SourceEnvelope.Metadata"),
                (Rule: IsInvalid(content), Parameter: "Content"));
        }

        private static dynamic IsInvalid<T>(T subject) => new
        {
            Condition = subject is null,
            Message = "Value is required"
        };

        private static dynamic IsInvalidMetadata<TSource>(EventEnvelope<TSource>? sourceEnvelope) => new
        {
            Condition = sourceEnvelope is not null && sourceEnvelope.Metadata is null,
            Message = "Metadata is required"
        };

        private static void Validate(params (dynamic Rule, string Parameter)[] validations)
        {
            var invalidArgumentEventEnvelopeException =
                new InvalidArgumentEventEnvelopeException(
                    message: "Invalid event envelope argument(s), correct the errors and try again.");

            foreach ((dynamic rule, string parameter) in validations)
            {
                if (rule.Condition)
                {
                    invalidArgumentEventEnvelopeException.UpsertDataList(
                        key: parameter,
                        value: rule.Message);
                }
            }

            invalidArgumentEventEnvelopeException.ThrowIfContainsErrors();
        }
    }
}
