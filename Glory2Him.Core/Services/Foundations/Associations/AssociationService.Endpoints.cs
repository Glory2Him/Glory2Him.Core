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
using System.Data.SqlTypes;
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Associations;

namespace Glory2Him.Core.Services.Foundations.Associations
{
    internal partial class AssociationService
    {
        /// <summary>
        /// Orders the two endpoints canonically, A before B, so one row serves both
        /// endpoints' lists and "is X linked to Y" is a single lookup rather than two.
        ///
        /// <para>Returns a negative number when the first endpoint sorts low, positive when
        /// it sorts high, zero when the two are indistinguishable — which validation rejects
        /// as a self-association.</para>
        /// </summary>
        internal static int CompareEndpoints(
            EntityType firstType,
            Guid firstGroupId,
            EntityType secondType,
            Guid secondGroupId)
        {
            // ordinal on the enum NAME, not the numeric value: the name is what SQL stores,
            // so a rename shows up as a data change while a renumber would silently reorder
            // existing rows. The canonical-order check constraint that will enforce this
            // ordering in the database does not exist yet — it arrives with the schema work,
            // and must apply COLLATE Latin1_General_BIN2 to match this ordinal comparison.
            int typeComparison = string.CompareOrdinal(
                firstType.ToString(),
                secondType.ToString());

            if (typeComparison != 0)
            {
                return typeComparison;
            }

            // SqlGuid, never Guid.CompareTo. SQL Server orders `uniqueidentifier` by bytes
            // 10-15 first; .NET compares the leading `_a`/`_b`/`_c` fields as integers. The
            // two disagree on most pairs, so a Guid.CompareTo here would put rows in an
            // order the database's own canonical-order check constraint then rejects.
            return new SqlGuid(firstGroupId).CompareTo(new SqlGuid(secondGroupId));
        }

        /// <summary>
        /// Swaps the endpoints when they are the wrong way round. Every field of an endpoint
        /// moves together — a half-swapped row would claim a key id belonging to the other
        /// entity. <c>EffectiveId</c> is excluded because the database computes it from the
        /// scope and ids that just moved.
        /// </summary>
        private static Association NormalizeEndpointOrder(Association association)
        {
            bool isAlreadyCanonical =
                CompareEndpoints(
                    firstType: association.EntityAType,
                    firstGroupId: association.EntityAGroupId,
                    secondType: association.EntityBType,
                    secondGroupId: association.EntityBGroupId) <= 0;

            if (isAlreadyCanonical)
            {
                return association;
            }

            (association.EntityAType, association.EntityBType) =
                (association.EntityBType, association.EntityAType);

            (association.EntityAKeyId, association.EntityBKeyId) =
                (association.EntityBKeyId, association.EntityAKeyId);

            (association.EntityAGroupId, association.EntityBGroupId) =
                (association.EntityBGroupId, association.EntityAGroupId);

            (association.EntityAScope, association.EntityBScope) =
                (association.EntityBScope, association.EntityAScope);

            (association.EntityAContentType, association.EntityBContentType) =
                (association.EntityBContentType, association.EntityAContentType);

            return association;
        }

        /// <summary>
        /// Fills in the endpoint fields the caller does not own: the group id of a
        /// non-versioned endpoint (which has exactly one row, so its group is itself) and
        /// the scope of both (design §7.5.1).
        ///
        /// <para>An endpoint whose type is outside <see cref="EntityType"/> is left
        /// untouched rather than resolved — it has no publication model, and reporting that
        /// as a validation error is more useful than throwing
        /// <see cref="NotSupportedException"/> out of a derivation.</para>
        /// </summary>
        private static Association ApplyDerivedEndpointFields(Association association)
        {
            if (Enum.IsDefined(association.EntityAType))
            {
                if (EntityTypeVersioning.IsVersioned(association.EntityAType) is false)
                {
                    association.EntityAGroupId = association.EntityAKeyId;
                }

                association.EntityAScope =
                    EntityTypeVersioning.DefaultScopeFor(association.EntityAType);
            }

            if (Enum.IsDefined(association.EntityBType))
            {
                if (EntityTypeVersioning.IsVersioned(association.EntityBType) is false)
                {
                    association.EntityBGroupId = association.EntityBKeyId;
                }

                association.EntityBScope =
                    EntityTypeVersioning.DefaultScopeFor(association.EntityBType);
            }

            return association;
        }
    }
}
