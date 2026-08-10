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
using System.Collections.Generic;
using G2H.Security.Client.Models.Foundations.Access;
using G2H.Security.Client.Models.Foundations.Access.Exceptions;

namespace G2H.Security.Client.Services.Foundations.Access
{
    internal partial class AccessService
    {
        // These validate the SHAPE of a request, never the policy it expresses. A structurally
        // invalid request is a caller bug and throws; a well-formed request that fails a rule is
        // a refusal and returns a verdict. Conflating the two would make every denial an
        // exception, and an exception message carries outward (§14.5).
        //
        // The `required` modifier on the request properties already forces the caller to supply
        // every section, so what is left to check is the values inside them — an empty entity
        // type, an actor with no id, a null element inside a list the compiler saw as non-null.
        virtual internal void ValidateOnEvaluateApprovalConditions(
            ApprovalConditionsRequest approvalConditionsRequest)
        {
            ValidateRequestIsNotNull(approvalConditionsRequest);

            Validate(
                (Rule: IsInvalid(approvalConditionsRequest.EntityType),
                    Parameter: nameof(ApprovalConditionsRequest.EntityType)),

                (Rule: IsInvalid(approvalConditionsRequest.CandidatePolicies),
                    Parameter: nameof(ApprovalConditionsRequest.CandidatePolicies)),

                (Rule: IsInvalid(approvalConditionsRequest.Reviews),
                    Parameter: nameof(ApprovalConditionsRequest.Reviews)),

                (Rule: IsInvalid(approvalConditionsRequest.Comments),
                    Parameter: nameof(ApprovalConditionsRequest.Comments)));
        }

        virtual internal void ValidateOnRecordReview(RecordReviewRequest recordReviewRequest)
        {
            ValidateRequestIsNotNull(recordReviewRequest);

            Validate(
                (Rule: IsInvalid(recordReviewRequest.Actor),
                    Parameter: nameof(RecordReviewRequest.Actor)),

                (Rule: IsInvalid(recordReviewRequest.RoleSubjects),
                    Parameter: nameof(RecordReviewRequest.RoleSubjects)),

                (Rule: IsInvalid(recordReviewRequest.ExistingReviews),
                    Parameter: nameof(RecordReviewRequest.ExistingReviews)));
        }

        virtual internal void ValidateOnDecideApproval(DecideApprovalRequest decideApprovalRequest)
        {
            ValidateRequestIsNotNull(decideApprovalRequest);

            Validate(
                (Rule: IsInvalid(decideApprovalRequest.Actor),
                    Parameter: nameof(DecideApprovalRequest.Actor)),

                (Rule: IsInvalid(decideApprovalRequest.EntityType),
                    Parameter: nameof(DecideApprovalRequest.EntityType)),

                (Rule: IsInvalid(decideApprovalRequest.RoleSubjects),
                    Parameter: nameof(DecideApprovalRequest.RoleSubjects)),

                (Rule: IsInvalid(decideApprovalRequest.CandidatePolicies),
                    Parameter: nameof(DecideApprovalRequest.CandidatePolicies)),

                (Rule: IsInvalid(decideApprovalRequest.Reviews),
                    Parameter: nameof(DecideApprovalRequest.Reviews)),

                (Rule: IsInvalid(decideApprovalRequest.Comments),
                    Parameter: nameof(DecideApprovalRequest.Comments)));
        }

        private static void ValidateRequestIsNotNull(object? request)
        {
            if (request is null)
            {
                throw new InvalidArgumentAccessException(
                    message: "Invalid access argument. Please correct the error and try again.");
            }
        }

        private static dynamic IsInvalid(string? text) => new
        {
            Condition = String.IsNullOrWhiteSpace(text),
            Message = "Text is required"
        };

        private static dynamic IsInvalid(AccessActor? accessActor) => new
        {
            Condition = accessActor is null || accessActor.Roles is null,
            Message = "Actor is required"
        };

        // The list itself may legitimately be EMPTY — an approval with no reviews yet, an
        // environment with no policy rows. What must never reach the decision is a null list,
        // because `required` guarantees the property was set and says nothing about what it was
        // set to, and a null would be a NullReferenceException inside a security rule.
        private static dynamic IsInvalid<T>(IReadOnlyList<T>? items) => new
        {
            Condition = items is null,
            Message = "List is required"
        };

        private static void Validate(params (dynamic Rule, string Parameter)[] validations)
        {
            var invalidArgumentAccessException =
                new InvalidArgumentAccessException(
                    message: "Invalid access argument. Please correct the error and try again.");

            foreach ((dynamic rule, string parameter) in validations)
            {
                if (rule.Condition)
                {
                    invalidArgumentAccessException.UpsertDataList(
                        key: parameter,
                        value: rule.Message);
                }
            }

            invalidArgumentAccessException.ThrowIfContainsErrors();
        }
    }
}
