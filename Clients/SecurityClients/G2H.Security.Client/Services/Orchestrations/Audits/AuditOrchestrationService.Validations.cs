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

using System.Security.Claims;
using G2H.Security.Client.Models.Clients;
using G2H.Security.Client.Models.Orchestrations.Audits.Exceptions;

namespace G2H.Security.Client.Services.Foundations.Audits
{
    internal partial class AuditOrchestrationService
    {
        private static void ValidateInputs<T>(
            T entity,
            ClaimsPrincipal claimsPrincipal,
            SecurityConfigurations securityConfigurations)
        {
            Validate(
                (Rule: IsInvalid(entity), Parameter: nameof(entity)),
                (Rule: IsInvalid(claimsPrincipal), Parameter: nameof(claimsPrincipal)),
                (Rule: IsInvalid(securityConfigurations), Parameter: nameof(securityConfigurations)));
        }

        private static void ValidateInputs<T>(
            T entity,
            T storageEntity,
            SecurityConfigurations securityConfigurations)
        {
            Validate(
                (Rule: IsInvalid(entity), Parameter: nameof(entity)),
                (Rule: IsInvalid(storageEntity), Parameter: nameof(storageEntity)),
                (Rule: IsInvalid(securityConfigurations), Parameter: nameof(securityConfigurations)));
        }

        private static void ValidateOnGetCurrentUserId(ClaimsPrincipal claimsPrincipal) =>
            Validate((Rule: IsInvalid(claimsPrincipal), Parameter: nameof(claimsPrincipal)));

        private static dynamic IsInvalid(ClaimsPrincipal claimsPrincipal) => new
        {
            Condition = claimsPrincipal == null,
            Message = "Claims principal is required"
        };

        private static dynamic IsInvalid<T>(T entity) => new
        {
            Condition = entity == null,
            Message = "Entity is required"
        };

        private static void Validate(params (dynamic Rule, string Parameter)[] validations)
        {
            var invalidArgumentAuditOrchestrationException =
                new InvalidArgumentAuditOrchestrationException(
                    message: "Invalid audit orchestration argument(s), correct the errors and try again.");

            foreach ((dynamic rule, string parameter) in validations)
            {
                if (rule.Condition)
                {
                    invalidArgumentAuditOrchestrationException.UpsertDataList(
                        key: parameter,
                        value: rule.Message);
                }
            }

            invalidArgumentAuditOrchestrationException.ThrowIfContainsErrors();
        }
    }
}
