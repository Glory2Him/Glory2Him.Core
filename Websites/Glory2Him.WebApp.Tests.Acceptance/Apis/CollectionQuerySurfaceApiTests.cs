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
using FluentAssertions;
using Glory2Him.WebApp.Infrastructure;
using Glory2Him.WebApp.Tests.Acceptance.Brokers;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis
{
    /// <summary>
    /// What a caller's query options may hold on every <c>[EnableQuery]</c> read, on the running
    /// host (GHSA-3m43-v266-m6qf, Fix task C). The exposers keep writing a bare
    /// <c>[EnableQuery]</c>, and the host narrows every one through
    /// <c>ODataQuerySurfaceConvention</c>.
    /// </summary>
    [Collection(nameof(ApiTestCollection))]
    public partial class CollectionQuerySurfaceApiTests
    {
        private readonly ApiBroker apiBroker;

        public CollectionQuerySurfaceApiTests(ApiBroker apiBroker)
        {
            this.apiBroker = apiBroker;

            // The acting caller is shared client state, so it is reset here rather than left to
            // whichever test ran last.
            this.apiBroker.ActAsSeededAdministrator();
        }

        /// <summary>
        /// C1. Every query attribute the host runs is the narrowed one. Nothing else here fails
        /// on a read the convention missed, because each probe names one route.
        /// </summary>
        [Fact]
        public void ShouldNarrowEveryQueryableCollectionReadsQueryAttribute()
        {
            // given . when
            IReadOnlyList<Type> actualQueryAttributeTypes =
                this.apiBroker.GetQueryableCollectionQueryAttributeTypes();

            // then
            actualQueryAttributeTypes.Should().NotBeEmpty();

            // Asserted over the types rather than over the attributes, so a failure prints the
            // type that was not narrowed.
            actualQueryAttributeTypes.Should().OnlyContain(
                queryAttributeType => queryAttributeType == typeof(NarrowedEnableQueryAttribute));
        }
    }
}
