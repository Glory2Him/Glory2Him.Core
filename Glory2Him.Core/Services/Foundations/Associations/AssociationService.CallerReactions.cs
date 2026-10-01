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
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;

namespace Glory2Him.Core.Services.Foundations.Associations
{
    internal partial class AssociationService
    {
        public ValueTask<IReadOnlyList<AssociationPairKey>> RetrieveCallerContentItemReactionsAsync(
            IReadOnlyList<Guid> contentItemGroupIds,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateOnRetrieveCallerContentItemReactions(contentItemGroupIds);

                EventEnvelope<Association> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: new Association());

                SecurityContext? securityContext = envelope.SecurityContext;

                // an anonymous caller holds no rows, so nothing is asked of storage
                if (securityContext is null || securityContext.IsAuthenticated is false)
                {
                    return Array.Empty<AssociationPairKey>();
                }

                string callerUserId =
                    await this.securityAuditBroker.GetUserIdAsync(securityContext);

                // an editorial row's UserId is null, so a caller with no id owns no row; a
                // signed-in identity without one is a misconfiguration, so it is logged
                if (string.IsNullOrWhiteSpace(callerUserId))
                {
                    await this.loggingBroker.LogWarningAsync(
                        message: "Content item association caller reactions read answered empty. " +
                            "The caller is signed in but their identity carries no user id.");

                    return Array.Empty<AssociationPairKey>();
                }

                return await this.storageBroker.SelectAssociationsAsync(
                    query: associations => associations
                        .Where(association =>
                            association.EntityAType == EntityType.ContentItem
                                && contentItemGroupIds.Contains(association.EntityAEffectiveId)
                                && association.EntityBType == EntityType.Reaction
                                && association.UserId == callerUserId
                                && association.IsDeleted == false)
                        .Select(association => new AssociationPairKey
                        {
                            EntityAEffectiveId = association.EntityAEffectiveId,
                            EntityBKeyId = association.EntityBKeyId
                        }),
                    cancellationToken: cancellationToken);
            });
    }
}
