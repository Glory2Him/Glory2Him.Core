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
using Glory2Him.Core.Models.Securities;
using Glory2Him.WebApp.Tests.Acceptance.Brokers;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis
{
    /// <summary>
    /// The callers and the carrying options the query-surface probes run over
    /// (GHSA-3m43-v266-m6qf, Fix task C). A probe's term is written once, as its left operand
    /// and the rest of it, and each carrying option sends it in its own shape.
    /// </summary>
    internal static class QuerySurfaceProbes
    {
        internal const string Contributor = "contributor";
        internal const string Administrator = "administrator";
        internal const string Anonymous = "anonymous";

        // A signed-in contributor outside the review tier, an Administrators caller and an
        // anonymous caller.
        internal static readonly string[] Callers = { Contributor, Administrator, Anonymous };

        // The seven carrying options: three options, in their spellings.
        internal static readonly string[] CarryingOptions =
        {
            "$filter",
            "filter",
            "$apply",
            "apply",
            "$APPLY",
            "$compute",
            "compute"
        };

        internal static void ActAs(ApiBroker apiBroker, string caller)
        {
            switch (caller)
            {
                case Contributor:
                    apiBroker.ActAsContributor();
                    break;

                case Administrator:
                    apiBroker.ActAs(Guid.NewGuid().ToString(), Roles.Administrators);
                    break;

                case Anonymous:
                    apiBroker.ActAsAnonymous();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(caller), caller, null);
            }
        }

        internal static string Carry(string carryingOption, string leftOperand, string rest)
        {
            string term = $"{leftOperand} {rest}";

            return carryingOption switch
            {
                "$filter" or "filter" =>
                    $"{carryingOption}={Uri.EscapeDataString(term)}",

                "$apply" or "apply" or "$APPLY" =>
                    $"{carryingOption}={Uri.EscapeDataString($"filter({term})")}",

                "$compute" =>
                    $"$compute={Uri.EscapeDataString($"{leftOperand} as c")}"
                        + $"&$filter={Uri.EscapeDataString($"c {rest}")}",

                "compute" =>
                    $"compute={Uri.EscapeDataString($"{leftOperand} as c")}"
                        + $"&filter={Uri.EscapeDataString($"c {rest}")}",

                _ => throw new ArgumentOutOfRangeException(nameof(carryingOption), carryingOption, null)
            };
        }
    }
}
