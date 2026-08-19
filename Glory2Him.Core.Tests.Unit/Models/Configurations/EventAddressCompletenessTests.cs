// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// John 14:6 (NIV) "Jesus answered, 'I am the way and the truth and the life.
//                  No one comes to the Father except through me.'"
// https://john.bible/john-14-6
// If Jesus is who He said He is, what does that mean for you, today?
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Glory2Him.Core.Models.Configurations;

namespace Glory2Him.Core.Tests.Unit.Models.Configurations
{
    /// <summary>
    /// Every event operation must have an address, and every address a name.
    ///
    /// <para>These are reflection tests on purpose. The lookup that consumes these maps is a raw
    /// indexer — <c>eventAddressIds[operation]</c> in <c>EventBroker</c> — so an operation added to
    /// an enum without a matching entry does not fail to compile and does not fail any mocked test.
    /// It throws <see cref="KeyNotFoundException"/> the first time something real publishes or
    /// subscribes, and the first such call is inside startup registration, which takes the whole
    /// substrate down with it.</para>
    ///
    /// <para>That is exactly what happened: the publication swap's <c>Approving</c> and
    /// <c>Approved</c> operations were declared on both processing enums and mapped in neither,
    /// and 3,900 passing unit tests could not see it because they all mock
    /// <c>IEventBroker</c>. Enumerating the enums rather than listing expectations is the point —
    /// a test that names the operations it expects would have to be updated by the same person who
    /// forgot to update the map.</para>
    /// </summary>
    public class EventAddressCompletenessTests
    {
        public static TheoryData<string> OperationEnumNames()
        {
            var data = new TheoryData<string>();

            foreach (Type enumType in FindOperationEnums())
            {
                data.Add(enumType.FullName!);
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(OperationEnumNames))]
        public void EveryEventOperationShouldHaveAnAddress(string enumTypeName)
        {
            // given
            Type enumType = FindOperationEnums()
                .Single(candidate => candidate.FullName == enumTypeName);

            IDictionary addressIds = FindAddressMapFor(enumType);

            addressIds.Should().NotBeNull(
                because: $"{enumType.Name} must have an operation-to-address map; without one "
                    + "nothing on that enum can be published or subscribed");

            // when
            var unmapped = Enum.GetValues(enumType)
                .Cast<object>()
                .Where(operation => addressIds!.Contains(operation) is false)
                .Select(operation => operation.ToString())
                .ToList();

            // then
            unmapped.Should().BeEmpty(
                because: $"every member of {enumType.Name} is looked up with a raw indexer, so an "
                    + "unmapped one throws KeyNotFoundException at the first real publish or "
                    + "subscribe — and the first of those is startup registration");
        }

        [Theory]
        [MemberData(nameof(OperationEnumNames))]
        public void EveryMappedAddressShouldCarryAName(string enumTypeName)
        {
            // given: the address-to-name map is what registers the address on the substrate, so an
            // address present in one map and absent from the other is an address nothing can reach.
            Type enumType = FindOperationEnums()
                .Single(candidate => candidate.FullName == enumTypeName);

            IDictionary addressIds = FindAddressMapFor(enumType);
            IDictionary addressNames = FindNameMapFor(enumType);

            addressNames.Should().NotBeNull(
                because: $"{enumType.Name}'s addresses must be named to be registered");

            // when
            var unnamed = addressIds!.Values
                .Cast<Guid>()
                .Where(addressId => addressNames!.Contains(addressId) is false)
                .ToList();

            // then
            unnamed.Should().BeEmpty(
                because: "an address with no name is never registered on the substrate, so "
                    + "publishing to it reaches nobody");
        }

        [Fact]
        public void NoEventAddressShouldBeSharedBetweenTwoEntities()
        {
            // given: sharing WITHIN one entity is a documented convention — HardRemoved is
            // published to the same address as Removed so consumers subscribe to one removal
            // address and tell the two apart by the composed event name. Sharing ACROSS entities
            // is never intentional: it would deliver one entity's events to another's
            // subscribers, and the receiving handler would deserialize a payload of the wrong
            // type.
            var ownersByAddress = new Dictionary<Guid, HashSet<string>>();

            foreach (Type enumType in FindOperationEnums())
            {
                IDictionary map = FindAddressMapFor(enumType);

                if (map is null)
                {
                    continue;
                }

                string owner = enumType.Name.Replace(
                    "EventOperation", string.Empty, StringComparison.Ordinal);

                foreach (Guid addressId in map.Values.Cast<Guid>())
                {
                    if (ownersByAddress.TryGetValue(addressId, out HashSet<string> owners) is false)
                    {
                        owners = new HashSet<string>(StringComparer.Ordinal);
                        ownersByAddress[addressId] = owners;
                    }

                    owners.Add(owner);
                }
            }

            // when
            var shared = ownersByAddress
                .Where(entry => entry.Value.Count > 1)
                .Select(entry => $"{entry.Key} shared by {string.Join(", ", entry.Value.OrderBy(o => o))}")
                .ToList();

            // then
            shared.Should().BeEmpty(
                because: "an address owned by two entities delivers one entity's events to the "
                    + "other's subscribers, which then deserialize the wrong payload type");
        }

        private static IEnumerable<Type> FindOperationEnums() =>
            typeof(EventBrokerIdentifiers).Assembly
                .GetTypes()
                .Where(type => type.IsEnum && type.Name.EndsWith("EventOperation", StringComparison.Ordinal))
                .OrderBy(type => type.FullName, StringComparer.Ordinal);

        // The maps are internal statics named after the enum: <Enum-minus-EventOperation> +
        // "EventAddressIds" / "EventAddresses". Found by convention rather than listed, so a new
        // entity's maps are covered the moment they exist.
        private static IDictionary FindAddressMapFor(Type enumType) =>
            FindMap(enumType, "EventAddressIds");

        private static IDictionary FindNameMapFor(Type enumType) =>
            FindMap(enumType, "EventAddresses");

        private static IDictionary FindMap(Type enumType, string suffix)
        {
            string prefix = enumType.Name.Replace("EventOperation", string.Empty, StringComparison.Ordinal);

            FieldInfo field = typeof(EventBrokerIdentifiers)
                .GetField(
                    prefix + suffix,
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

            return field?.GetValue(null) as IDictionary;
        }
    }
}
