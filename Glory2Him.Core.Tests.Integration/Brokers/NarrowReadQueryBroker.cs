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
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Brokers.Storages.Sql;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.Links;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Glory2Him.Core.Tests.Integration.Brokers
{
    /// <summary>
    /// A real <see cref="StorageBroker"/> over LocalDB for the NARROW READS — the purpose-built
    /// single-row, group and existence queries the services ask for instead of enumerating a
    /// collection read's live queryable.
    ///
    /// <para><b>Why these need a real database at all.</b> Each of these reads used to be a LINQ
    /// predicate composed in a service and executed by a synchronous terminal operator, which
    /// made it provable in the unit suite against an in-memory queryable. Moving the predicate to
    /// the broker is what lets the query be awaited and lets the cancellation token reach the
    /// database — and it also means LINQ-to-Objects no longer stands in for SQL. Whether the
    /// predicate translates, and whether the ORDERING it depends on holds server-side, is now a
    /// question only a real catalogue answers. Same reasoning, and the same fixture shape, as
    /// <see cref="IdentityCoreQueryBroker"/> (issue #351).</para>
    ///
    /// <para>Its own catalogue suffix: every fixture creates and DROPS its schema, and xUnit only
    /// serialises within a collection, so two sharing a catalogue would delete each other's rows
    /// mid-run.</para>
    /// </summary>
    public sealed class NarrowReadQueryBroker : IDisposable
    {
        private readonly StorageBroker storageBroker;

        public NarrowReadQueryBroker()
        {
            this.storageBroker = new StorageBroker(
                IntegrationDatabase.BuildConfiguration(catalogueSuffix: "_NarrowReads"));

            IntegrationDatabase.EnsureSchema(this.storageBroker);
        }

        internal IStorageBroker StorageBroker => this.storageBroker;

        /// <summary>
        /// Attempts an insert and returns the exception the database raised, or <c>null</c> when
        /// the row was accepted.
        ///
        /// <para>Detaching on failure is not tidiness. A rejected <c>SaveChanges</c> leaves the
        /// entity tracked in the <c>Added</c> state, and this fixture shares one context across the
        /// whole collection — the next save would retry the rejected row and fail a test that has
        /// nothing to do with it. Any test that EXPECTS an insert to be refused must come through
        /// here rather than through <c>SeedAsync</c>.</para>
        /// </summary>
        public async ValueTask<Exception> TryInsertAsync(Approval approval)
        {
            try
            {
                await this.storageBroker.InsertApprovalAsync(approval, CancellationToken.None);

                return null;
            }
            catch (Exception exception)
            {
                this.storageBroker.Entry(approval).State = EntityState.Detached;

                return exception;
            }
        }

        public async ValueTask SeedAsync(params Approval[] approvals)
        {
            foreach (Approval approval in approvals)
            {
                await this.storageBroker.InsertApprovalAsync(approval, CancellationToken.None);
            }
        }

        public async ValueTask SeedAsync(params ApprovalReviewRequest[] approvalReviewRequests)
        {
            foreach (ApprovalReviewRequest approvalReviewRequest in approvalReviewRequests)
            {
                await this.storageBroker.InsertApprovalReviewRequestAsync(
                    approvalReviewRequest, CancellationToken.None);
            }
        }

        public async ValueTask SeedAsync(params Association[] associations)
        {
            foreach (Association association in associations)
            {
                await this.storageBroker.InsertAssociationAsync(association, CancellationToken.None);
            }
        }

        public async ValueTask SeedAsync(params ContentItem[] contentItems)
        {
            foreach (ContentItem contentItem in contentItems)
            {
                await this.storageBroker.InsertContentItemAsync(contentItem, CancellationToken.None);
            }
        }

        public async ValueTask SeedAsync(params Link[] links)
        {
            foreach (Link link in links)
            {
                await this.storageBroker.InsertLinkAsync(link, CancellationToken.None);
            }
        }

        /// <summary>
        /// Removes the rows a test seeded. Every read here is keyed, so cross-test rows cannot
        /// normally be seen — but the tables are shared, and a leftover row on a reused key would
        /// be indistinguishable from a bug in the read.
        /// </summary>
        public async ValueTask ClearAsync(IEnumerable<Approval> approvals)
        {
            foreach (Approval approval in approvals)
            {
                Approval stored = await this.storageBroker.SelectApprovalByIdAsync(
                    approval.Id, CancellationToken.None);

                if (stored is not null)
                {
                    await this.storageBroker.DeleteApprovalAsync(stored, CancellationToken.None);
                }
            }
        }

        // Cleared BEFORE the approvals they hang off, since the FK refuses the other order.
        public async ValueTask ClearAsync(IEnumerable<ApprovalReviewRequest> approvalReviewRequests)
        {
            foreach (ApprovalReviewRequest approvalReviewRequest in approvalReviewRequests)
            {
                ApprovalReviewRequest stored =
                    await this.storageBroker.SelectApprovalReviewRequestByIdAsync(
                        approvalReviewRequest.Id, CancellationToken.None);

                if (stored is not null)
                {
                    await this.storageBroker.DeleteApprovalReviewRequestAsync(
                        stored, CancellationToken.None);
                }
            }
        }

        public async ValueTask ClearAsync(IEnumerable<Association> associations)
        {
            foreach (Association association in associations)
            {
                Association stored = await this.storageBroker.SelectAssociationByIdAsync(
                    association.Id, CancellationToken.None);

                if (stored is not null)
                {
                    await this.storageBroker.DeleteAssociationAsync(stored, CancellationToken.None);
                }
            }
        }

        public async ValueTask ClearAsync(IEnumerable<ContentItem> contentItems)
        {
            foreach (ContentItem contentItem in contentItems)
            {
                ContentItem stored = await this.storageBroker.SelectContentItemByIdAsync(
                    contentItem.Id, CancellationToken.None);

                if (stored is not null)
                {
                    await this.storageBroker.DeleteContentItemAsync(stored, CancellationToken.None);
                }
            }
        }

        public async ValueTask ClearAsync(IEnumerable<Link> links)
        {
            foreach (Link link in links)
            {
                Link stored = await this.storageBroker.SelectLinkByIdAsync(
                    link.Id, CancellationToken.None);

                if (stored is not null)
                {
                    await this.storageBroker.DeleteLinkAsync(stored, CancellationToken.None);
                }
            }
        }

        // xUnit disposes a collection fixture once, after the last test in the collection
        public void Dispose()
        {
            IntegrationDatabase.Drop(this.storageBroker);
            this.storageBroker.Dispose();
        }
    }

    /// <summary>
    /// Binds <see cref="NarrowReadQueryBroker"/> to a collection so xUnit builds it once, shares
    /// it, and disposes it once at the end — and so the tests inside it are serialised, because
    /// they share four tables.
    /// </summary>
    [CollectionDefinition(NarrowReadIntegrationCollection.Name)]
    public sealed class NarrowReadIntegrationCollection
        : ICollectionFixture<NarrowReadQueryBroker>
    {
        public const string Name = "Narrow read integration";
    }
}
