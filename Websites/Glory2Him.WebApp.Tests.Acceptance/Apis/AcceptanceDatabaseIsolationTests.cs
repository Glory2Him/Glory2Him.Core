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

using System.Collections.Generic;
using FluentAssertions;
using Glory2Him.WebApp.Tests.Acceptance.Brokers;
using Xunit;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis
{
    /// <summary>
    /// Proves this suite is running against its own per-run databases rather than the
    /// developer's (#302).
    ///
    /// <para>The failure it guards is silent by construction. If the connection-string
    /// overrides were dropped, the host would boot happily against <c>Glory2Him.Core</c>,
    /// <c>Glory2Him.Events</c> and <c>Glory2Him.Security</c>, every other test in this suite
    /// would still pass, and the only symptom would be an event ledger growing by ~180 rows a
    /// run in a database the developer also uses. Nothing else here would notice.</para>
    ///
    /// <para>It asserts the names off the <b>booted host's</b> configuration, not the test
    /// project's intent — what the request pipeline connects to is the only thing that
    /// matters.</para>
    /// </summary>
    [Collection(nameof(ApiTestCollection))]
    public class AcceptanceDatabaseIsolationTests
    {
        // Spelt out rather than referenced from AcceptanceDatabaseBroker on purpose: a test that
        // takes its expectation from the thing under test passes when both move together, which
        // is exactly the change this is here to catch.
        //
        // "Glory2Him." alone would also match the production names issue #351 gave the
        // normal-use databases (Glory2Him.Core, Glory2Him.Events, Glory2Him.Security), so the
        // segment checked here is the one only a per-run catalogue carries.
        private const string ExpectedDatabaseSegment = "_Acceptance_";

        private readonly ApiBroker apiBroker;

        public AcceptanceDatabaseIsolationTests(ApiBroker apiBroker) =>
            this.apiBroker = apiBroker;

        [Fact]
        public void ShouldRunEveryStoreAgainstItsOwnPerRunDatabase()
        {
            // given, when
            IReadOnlyDictionary<string, string> resolvedDatabaseNames =
                this.apiBroker.GetResolvedDatabaseNames();

            // then
            resolvedDatabaseNames.Should().HaveCount(3,
                because: "the acceptance host opens three stores — Core's schema, the "
                    + "EventHighway substrate and Identity — and all three have to be isolated "
                    + "for the suite to stop writing to the developer's own databases");

            foreach (KeyValuePair<string, string> resolvedDatabaseName in resolvedDatabaseNames)
            {
                resolvedDatabaseName.Value.Should().Contain(ExpectedDatabaseSegment,
                    because: $"'{resolvedDatabaseName.Key}' must resolve to a per-run catalogue. "
                        + "A name without this segment is a developer database: the suite would "
                        + "still pass, and would leave a growing event ledger behind it every "
                        + "run. It is also what AcceptanceDatabaseBroker refuses to drop, so a "
                        + "regression here silently disables teardown as well");
            }
        }
    }
}
