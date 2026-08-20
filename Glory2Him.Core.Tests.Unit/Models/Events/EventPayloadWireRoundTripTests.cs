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
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Glory2Him.Core.Models.Bases;
using Glory2Him.Core.Models.Foundations.Associations;

namespace Glory2Him.Core.Tests.Unit.Models.Events
{
    /// <summary>
    /// Guards the seam between a fact's publisher and its receiver.
    ///
    /// <para><c>EventBroker</c> signs an envelope over the live object, serializes it to store
    /// the event, and deserializes it again to hand to a handler — which re-computes the
    /// signature over the REBUILT object. Publisher and receiver therefore have to agree on
    /// every byte, and any property that does not survive the round trip breaks a genuine
    /// envelope's signature. The receiver refuses a fact its own publisher sent, and nothing
    /// else in the system watches for that.</para>
    ///
    /// <para><c>EnvelopeIntegrityBrokerTests</c> cannot see this: it signs and verifies the SAME
    /// object instance, so no serialization ever happens between the two. That is why an
    /// <c>Association</c> could lose both its effective ids in transit with a green suite.</para>
    /// </summary>
    public class EventPayloadWireRoundTripTests
    {
        /// <summary>
        /// Every entity model that can ride an event envelope, derived rather than listed so a
        /// new entity is covered the day it is added.
        /// </summary>
        /// <remarks>
        /// <c>IKey</c> is the discriminator because it is the one the substrate itself uses:
        /// <c>ReactToEntityFactAsync&lt;TEntity&gt;</c> is constrained <c>where TEntity : IKey</c>.
        /// Exceptions are excluded — they live in these namespaces too, and they inherit a raft
        /// of non-public setters from <c>Exception</c> that no envelope ever carries.
        /// </remarks>
        public static TheoryData<Type> EventCarriedModels()
        {
            var models = new TheoryData<Type>();

            IEnumerable<Type> entityModels = typeof(Association).Assembly
                .GetTypes()
                .Where(type => type.IsClass && type.IsPublic && !type.IsAbstract)
                .Where(type => typeof(IKey).IsAssignableFrom(type))
                .Where(type => !typeof(Exception).IsAssignableFrom(type));

            foreach (Type model in entityModels.OrderBy(type => type.FullName))
            {
                models.Add(model);
            }

            return models;
        }

        [Theory]
        [MemberData(nameof(EventCarriedModels))]
        public void ShouldDeserializeEveryPropertyItSerializes(Type model)
        {
            // given: System.Text.Json WRITES a public property with a non-public setter but will
            // not READ back into one. So such a property leaves the publisher inside the signed
            // payload and arrives at the receiver empty — a silent divergence, not an error.
            //
            // [JsonInclude] is the opt-in that makes the setter visible to deserialization, so a
            // property carrying it is safe. Everything else with a non-public setter is not.

            // when
            IReadOnlyList<string> propertiesLostInTransit = model
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.CanRead && property.GetMethod.IsPublic)
                .Where(property => property.SetMethod is null || !property.SetMethod.IsPublic)
                .Where(property => property.GetCustomAttribute<JsonIncludeAttribute>() is null)
                .Where(property => property.GetCustomAttribute<JsonIgnoreAttribute>() is null)
                .Select(property => property.Name)
                .ToList();

            // then
            propertiesLostInTransit.Should().BeEmpty(
                because: $"{model.Name} can ride an event envelope, and every property it " +
                    "serializes must deserialize back — a value the publisher signs and the " +
                    "receiver cannot see makes the recomputed HMAC differ, so the receiver " +
                    "refuses a genuine fact. Add [JsonInclude] to a database-computed property, " +
                    "or [JsonIgnore] if it genuinely must not cross the wire");
        }

        [Fact]
        public void ShouldCarryAssociationEffectiveIdsAcrossTheWire()
        {
            // given: both effective ids are computed and persisted by the database and exposed
            // with a non-public setter, so they are the concrete case the guard above abstracts
            var association = new Association { Id = Guid.NewGuid() };
            Guid entityAEffectiveId = Guid.NewGuid();
            Guid entityBEffectiveId = Guid.NewGuid();

            typeof(Association).GetProperty(nameof(Association.EntityAEffectiveId))
                .SetValue(association, entityAEffectiveId);

            typeof(Association).GetProperty(nameof(Association.EntityBEffectiveId))
                .SetValue(association, entityBEffectiveId);

            // when: exactly what EventBroker does between publisher and receiver
            Association received = JsonSerializer.Deserialize<Association>(
                JsonSerializer.Serialize(association));

            // then
            received.EntityAEffectiveId.Should().Be(entityAEffectiveId,
                because: "the endpoint an association resolves to is inside the signed payload, " +
                    "so losing it in transit breaks the signature of every association fact");

            received.EntityBEffectiveId.Should().Be(entityBEffectiveId,
                because: "the same holds for the canonical high side");
        }
    }
}
