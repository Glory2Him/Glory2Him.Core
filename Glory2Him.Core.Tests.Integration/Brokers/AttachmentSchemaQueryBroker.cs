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
using Glory2Him.Core.Models.Foundations.Attachments;
using Microsoft.EntityFrameworkCore;

namespace Glory2Him.Core.Tests.Integration.Brokers
{
    /// <summary>
    /// A real <see cref="StorageBroker"/> over LocalDB for the published-slot schema tests.
    ///
    /// <para>No service is wired in, and deliberately so: <c>Attachment</c> has no foundation
    /// service yet (#181), and the rule under test is the database's rather than any
    /// service's. Every write below goes straight to the storage broker.</para>
    /// </summary>
    public sealed class AttachmentSchemaQueryBroker : IDisposable
    {
        // Its own catalogue. This fixture creates and drops a schema, and so does
        // AssociationQueryBroker — xUnit serialises within a collection but runs collections
        // in parallel, so sharing one database would let either drop the other's mid-run.
        private const string CatalogueSuffix = "_Attachments";

        private readonly StorageBroker storageBroker;

        public AttachmentSchemaQueryBroker()
        {
            this.storageBroker = new StorageBroker(
                IntegrationDatabase.BuildConfiguration(CatalogueSuffix));

            IntegrationDatabase.EnsureSchema(this.storageBroker);
        }

        /// <summary>
        /// Attempts an insert and returns the exception the database raised, or <c>null</c>
        /// when the row was accepted.
        ///
        /// <para>Detaching on failure is not tidiness. A rejected <c>SaveChanges</c> leaves
        /// the entity tracked in the <c>Added</c> state, and this fixture shares one context
        /// across the whole collection — the next save would retry the rejected row and fail
        /// a test that has nothing to do with it.</para>
        /// </summary>
        public async ValueTask<Exception> TryInsertAsync(Attachment attachment)
        {
            try
            {
                await this.storageBroker.InsertAttachmentAsync(
                    attachment, CancellationToken.None);

                return null;
            }
            catch (Exception exception)
            {
                this.storageBroker.Entry(attachment).State = EntityState.Detached;

                return exception;
            }
        }

        /// <summary>
        /// Marks a row deleted the way a soft remove does, leaving it in the table with
        /// <c>IsPublished</c> untouched — which is the state §5.6.4 rule 4 is about.
        /// </summary>
        public async ValueTask SoftDeleteAsync(Attachment attachment)
        {
            attachment.IsDeleted = true;

            await this.storageBroker.UpdateAttachmentAsync(
                attachment, CancellationToken.None);
        }

        /// <summary>
        /// Returns the predicate SQL Server actually stored for an index, or <c>null</c> when
        /// no index of that name exists.
        ///
        /// <para>This reads the DEPLOYED object rather than the configuration that produced
        /// it, which is the only way to assert that what the model declares is what the
        /// database ended up with.</para>
        /// </summary>
        public async ValueTask<string> GetIndexFilterDefinitionAsync(string indexName)
        {
            List<string> definitions = await this.storageBroker.Database
                .SqlQuery<string>(
                    $@"SELECT filter_definition AS [Value]
                       FROM sys.indexes
                       WHERE name = {indexName}")
                .ToListAsync();

            return definitions.Count == 0 ? null : definitions[0];
        }

        /// <summary>
        /// Builds a valid row. Only the fields a test cares about are set by the caller;
        /// everything else is filled with something the columns accept.
        /// </summary>
        public static Attachment CreateVersion(Guid groupId, int version, bool isPublished)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            return new Attachment
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                Version = version,
                IsPublished = isPublished,
                Name = $"attachment-{version}.png",
                BlobUri = $"https://example.invalid/{Guid.NewGuid()}",
                Hash = Guid.NewGuid().ToString("N"),
                CreatedBy = "integration",
                CreatedWhen = now,
                UpdatedBy = "integration",
                UpdatedWhen = now
            };
        }

        /// <summary>
        /// Removes every row a test seeded, so the database can be reused without cross-test
        /// interference.
        /// </summary>
        public async ValueTask ClearAsync(IEnumerable<Attachment> attachments)
        {
            foreach (Attachment attachment in attachments)
            {
                Attachment stored =
                    await this.storageBroker.SelectAttachmentByIdAsync(
                        attachment.Id, CancellationToken.None);

                if (stored is not null)
                {
                    await this.storageBroker.DeleteAttachmentAsync(
                        stored, CancellationToken.None);
                }
            }
        }

        // xUnit disposes a collection fixture once, after the last test in the collection.
        public void Dispose()
        {
            IntegrationDatabase.Drop(this.storageBroker);
            this.storageBroker.Dispose();
        }
    }

    /// <summary>
    /// Binds <see cref="AttachmentSchemaQueryBroker"/> to a collection so xUnit builds it
    /// once, shares it across every test in the collection, and disposes it once at the end.
    /// </summary>
    [CollectionDefinition(AttachmentSchemaCollection.Name)]
    public sealed class AttachmentSchemaCollection
        : ICollectionFixture<AttachmentSchemaQueryBroker>
    {
        public const string Name = "Attachment schema integration";
    }
}
