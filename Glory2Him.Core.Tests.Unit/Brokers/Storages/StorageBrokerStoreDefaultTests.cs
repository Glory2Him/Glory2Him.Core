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
using System.Linq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Glory2Him.Core.Tests.Unit.Brokers.Storages
{
    /// <summary>
    /// A guard on every column that carries a store default — that configuring one never
    /// costs a caller the value it asked to write.
    ///
    /// <para>A property with <c>HasDefaultValue</c> is marked <see cref="ValueGenerated.OnAdd"/>,
    /// and on insert EF OMITS any value equal to that property's SENTINEL, letting the database
    /// default win instead. The sentinel is the CLR default of the type unless something moves
    /// it — EF moves it to the configured default for a <c>bool</c>, which is why
    /// <c>HasDefaultValue(true)</c> columns still store a deliberate <c>false</c>, and does NOT
    /// move it for an <c>int</c>, which is why a seeded <c>SortOrder</c> of 0 was silently
    /// stored as 1000 (#395).</para>
    ///
    /// <para>No test above this layer can catch it. The storage broker is mocked everywhere in
    /// the suite, so the whole layer where the value is dropped is stubbed out, and the seed
    /// that loses the value is idempotent — it never runs again to lose it twice. The fault
    /// reaches a fresh database and stays there.</para>
    ///
    /// <para>The rule below is the general form rather than a list of the columns that have
    /// been caught: a store default is safe when EF can never mistake a real value for an
    /// absent one, which means either the property is never database-generated, or its
    /// sentinel IS the default and so the omission writes the same value anyway.</para>
    /// </summary>
    public class StorageBrokerStoreDefaultTests
    {
        [Fact]
        public void ShouldNeverLetAColumnDefaultOverwriteAValueTheCallerSet()
        {
            // given
            IEnumerable<IProperty> propertiesWithStoreDefaults =
                StorageBrokerModelSource.Model
                    .GetEntityTypes()
                    .SelectMany(entityType => entityType.GetProperties())
                    .Where(property => property.GetDefaultValue() is not null);

            // when
            List<string> unsafeProperties = propertiesWithStoreDefaults
                .Where(IsAtRiskOfLosingAValue)
                .Select(property =>
                    $"{property.DeclaringType.ClrType.Name}.{property.Name} " +
                    $"(default {property.GetDefaultValue()}, sentinel {property.Sentinel ?? "null"})")
                .ToList();

            // then
            unsafeProperties.Should().BeEmpty(
                because: "a store default must not replace a value the caller set — add " +
                    "ValueGeneratedNever() to the property, which keeps the column default for " +
                    "raw-SQL inserts while making EF always send what the entity holds (#395)");
        }

        // Safe either because EF always sends the value, or because the value EF omits is the
        // one the database would write anyway.
        private static bool IsAtRiskOfLosingAValue(IProperty property)
        {
            if (property.ValueGenerated == ValueGenerated.Never)
            {
                return false;
            }

            object defaultValue = property.GetDefaultValue();
            object sentinel = property.Sentinel;

            return Equals(defaultValue, sentinel) is false;
        }
    }
}
