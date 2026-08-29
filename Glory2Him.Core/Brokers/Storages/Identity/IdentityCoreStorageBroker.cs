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
using Glory2Him.Core.Models.Foundations.IdentityUsers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Glory2Him.Core.Brokers.Storages.Identity
{
    /// <summary>
    /// The EF Core implementation of <see cref="IIdentityCoreStorageBroker"/>, bound to the
    /// <c>Glory2HimSecurityConnection</c> connection string.
    ///
    /// <para><b>Query-tracking is off and every read is a projection.</b> Nothing here is
    /// attached to a change tracker, so there is no path by which a caller could mutate a row and
    /// have it saved — this type exposes no <c>SaveChanges</c> route at all.</para>
    ///
    /// <para><b>EF tooling note.</b> Adding this context makes Glory2Him.Core a two-context
    /// project, so <c>dotnet ef</c> can no longer infer which one a command means. Migration
    /// commands must now name the Core context explicitly:</para>
    ///
    /// <code>dotnet ef migrations add &lt;Name&gt; --project Glory2Him.Core --context StorageBroker</code>
    ///
    /// <para>There is deliberately no design-time factory for THIS context: Core owns no
    /// migrations against the identity schema, and not providing a factory is what makes an
    /// accidental <c>--context IdentityCoreStorageBroker</c> fail loudly instead of generating a
    /// migration that would fight the host's own.</para>
    /// </summary>
    internal class IdentityCoreStorageBroker : DbContext, IIdentityCoreStorageBroker
    {
        private readonly IConfiguration configuration;

        public IdentityCoreStorageBroker(IConfiguration configuration) =>
            this.configuration = configuration;

        public DbSet<IdentityUser> IdentityUsers { get; set; }
        public DbSet<IdentityRole> IdentityRoles { get; set; }
        public DbSet<IdentityUserRole> IdentityUserRoles { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // No tracking, matching the read-only contract: an entity that is never tracked can
            // never be saved, whatever a future caller does with it.
            optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

            string connectionString = this.configuration
                .GetConnectionString(name: "Glory2HimSecurityConnection") ?? string.Empty;

            optionsBuilder.UseSqlServer(connectionString);
        }

        // Only the columns the review-tier lookup needs are mapped. The rest of the host's
        // Identity schema is deliberately invisible here, so a column added or renamed over there
        // cannot break a read over here unless it is one of these.
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<IdentityUser>(model =>
            {
                model.ToTable("AspNetUsers");
                model.HasKey(identityUser => identityUser.Id);
                model.Property(identityUser => identityUser.UserName).HasMaxLength(256);
                model.Property(identityUser => identityUser.Email).HasMaxLength(256);
                model.Property(identityUser => identityUser.IsDisabled);
                model.Property(identityUser => identityUser.Name);
                model.Property(identityUser => identityUser.Surname);
                model.Property(identityUser => identityUser.PreferredName);
            });

            modelBuilder.Entity<IdentityRole>(model =>
            {
                model.ToTable("AspNetRoles");
                model.HasKey(identityRole => identityRole.Id);
                model.Property(identityRole => identityRole.Name).HasMaxLength(256);
            });

            modelBuilder.Entity<IdentityUserRole>(model =>
            {
                model.ToTable("AspNetUserRoles");

                model.HasKey(identityUserRole =>
                    new { identityUserRole.UserId, identityUserRole.RoleId });
            });
        }

        public async ValueTask<List<IdentityUser>> SelectIdentityUsersInRolesAsync(
            IReadOnlyList<string> normalizedRoleNames,
            CancellationToken cancellationToken = default)
        {
            IQueryable<Guid> matchingRoleIds = IdentityRoles
                .Where(identityRole => normalizedRoleNames.Contains(identityRole.Name.ToUpper()))
                .Select(identityRole => identityRole.Id);

            IQueryable<Guid> memberUserIds = IdentityUserRoles
                .Where(identityUserRole => matchingRoleIds.Contains(identityUserRole.RoleId))
                .Select(identityUserRole => identityUserRole.UserId);

            return await IdentityUsers
                .Where(identityUser =>
                    identityUser.IsDisabled == false
                        && memberUserIds.Contains(identityUser.Id))
                .ToListAsync(cancellationToken);
        }
    }
}
