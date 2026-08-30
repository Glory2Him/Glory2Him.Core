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

using System.Data.Common;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Securities;
using Glory2Him.WebApp.Models.Foundations.Roles;
using Glory2Him.WebApp.Models.Foundations.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Glory2Him.WebApp.Data
{
    // Idempotent first-run seed: creates the Users role, Core's own role set (Administrators
    // among them) and the default admin/user accounts (Spec Section 6.3). Default credentials are intentionally weak for
    // first-run/demo; production must enforce a strong password policy and force-change.
    public static class SeedData
    {
        private const string UsersRole = "Users";

        // Glory2Him.Core decides authorization against role NAMES it owns, compared by exact
        // ordinal equality — never by suffix, so until these rows exist and somebody holds them
        // the moderation tier is unreachable: approve and hard delete answer 403 at the
        // attribute, and a moderator can neither modify another user's tag nor see non-public
        // rows. Referenced from Core rather than re-spelled here so the two cannot drift.
        //
        // "Administrators" IS ONE OF THESE NAMES NOW. It used to be the portal's own vocabulary,
        // seeded here beside a separate Core "Admin" and satisfying none of Core's checks — the
        // two-vocabulary split of issue #193, closed out by #368. One row now opens both
        // surfaces, which is why it is no longer minted separately below: CoreRoles carries it.
        //
        // Reviewers and Tag-Reviewers appear in no [Authorize(Roles = ...)] list — the gates they
        // satisfy are owner-OR-review-role and cannot be written as a fixed list — but they are
        // what makes a reviewer's write and read reach past their own rows (§14.7 posture A).
        // Both tiers are provisioned: HasReviewRole tests the global Reviewers as well as the
        // entity-scoped one, so seeding only the scoped role would leave half the rule dead.
        //
        // DERIVED FROM THE ENUM RATHER THAN LISTED. Until the exposers arrived this was a hand
        // written array, and it had drifted to cover Tag alone — every other entity type's
        // review, publish and block roles were missing, so their moderation tiers were
        // unreachable in exactly the way the paragraph above describes, and each new controller
        // would have had to remember to append three more names to one shared array.
        //
        // Deriving is correct here rather than the kind of inference §7.5.1 rule 1 forbids.
        // That rule bans discovering a row's PUBLICATION MODEL from its runtime shape, where
        // the shape is not the source of truth. Here the enum IS the source: Roles.ReviewersFor
        // composes the name from entityType.ToString(), so the set of entity types and the set
        // of scoped role names are the same fact, and writing them out twice is what lets them
        // disagree.
        // INTERNAL rather than private so ContentTypeRoleSeedTests can pin the seeded set to
        // the enums it is composed from. The seed is the only place a role can be minted, and
        // a name that silently stops being seeded is invisible until somebody cannot be granted
        // a tier that the code already reads.
        internal static readonly string[] CoreRoles = BuildCoreRoleNames();

        // Association is excluded, and its absence is a rule rather than an oversight: it "has
        // no scoped roles of its own (design §14.7, §18.6) — authorization is derived from its
        // two endpoint entity types instead", which Roles.cs states at the point where the
        // Association-* constants would otherwise sit. Seeding Association-Reviewers would mint a
        // role no gate in the codebase ever asks for, and hand an administrator a grant that
        // silently does nothing.
        //
        // A METHOD, not a static field. As a field it would have to be declared above CoreRoles
        // to be assigned before the initializer that reads it — static field initializers run in
        // textual order — and getting that wrong throws inside the type initializer, which
        // SeedAsync's caller retries and then swallows by design. The seed would simply not
        // happen, and the portal would come up serving with no roles at all.
        private static EntityType[] ScopedRoleEntityTypes() =>
            Enum.GetValues<EntityType>()
                .Where(entityType => entityType != EntityType.Association)
                .ToArray();

        private static string[] BuildCoreRoleNames()
        {
            var coreRoleNames = new List<string>
            {
                Roles.Administrators,
                Roles.Reviewers,
                Roles.Publishers,

                // The block tier (design §18.6): "assigned to users who misbehave, takes
                // precedence over every other role". The foundation tests for these on every
                // write and on hard delete, but SeedData is the only place a role can be minted
                // — IIdentityBroker assigns and never creates — so without these rows the
                // sanction path is code that can never be reached and an administrator has no
                // way to restrain a contributor.
                Roles.ReadOnly
            };

            foreach (EntityType entityType in ScopedRoleEntityTypes())
            {
                coreRoleNames.Add(Roles.ReadOnlyFor(entityType));
                coreRoleNames.Add(Roles.ReviewersFor(entityType));
                coreRoleNames.Add(Roles.PublishersFor(entityType));
            }

            // The NARROW tier of §18.6 rule 5 — "trusted with stories but not testimonies".
            // ContentItem ONLY, because it is the one entity type that carries a ContentType.
            // Composing it for any other type would mint exactly the roles §14.7 posture A′
            // rule 6 exists to refuse: AssociationService tests the endpoint type as well as
            // the content type so a ContentItem-Testimony-Reviewers can never be matched
            // against a Tag endpoint that happens to carry Testimony.
            //
            // EVERY member is seeded, Series and Topic included, and that is a decision rather
            // than a foreach nobody thought about. They are ContentType members on ContentItem,
            // and §18.6 rule 5 scopes the tier to the entity type, not to a chosen subset of its
            // content types. Withholding them would protect nothing — the coarse
            // ContentItem-Reviewers still admits somebody to a Series either way — it would only
            // remove an administrator's ability to scope a person narrowly. A role that exists
            // and is assigned to nobody grants nothing, which is the same argument Attachment
            // above is seeded on.
            //
            // There is deliberately NO ReadOnlyFor(EntityType, ContentType): Roles.cs says so at
            // the point where it would sit. The block tier has no content-type tier, and offering
            // the composition would invent a role nothing issues and nothing checks. Two roles
            // per content type, not three.
            //
            // Walking the enum is what keeps this correct as ContentType grows — see §18.6's
            // seeding rule, and ContentTypeRoleSeedTests, which fails if this loop is ever
            // replaced by a hand-written list.
            foreach (ContentType contentType in Enum.GetValues<ContentType>())
            {
                coreRoleNames.Add(Roles.ReviewersFor(EntityType.ContentItem, contentType));
                coreRoleNames.Add(Roles.PublishersFor(EntityType.ContentItem, contentType));
            }

            // Attachment is included even though it has no service yet (§12.4 entry 3). The
            // vocabulary is the enum's, not the service layer's, and a role that exists before
            // the entity does grants nothing — whereas one that arrives late leaves the entity
            // unmoderatable on the day it ships.
            return coreRoleNames.ToArray();
        }

        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using IServiceScope scope = serviceProvider.CreateScope();
            IServiceProvider services = scope.ServiceProvider;

            var securityDbContext = services.GetRequiredService<SecurityDbContext>();
            await securityDbContext.Database.MigrateAsync();
            await DisableAutoCloseForLocalDbAsync(securityDbContext);

            var roleManager = services.GetRequiredService<RoleManager<AppRole>>();
            var userManager = services.GetRequiredService<UserManager<AppUser>>();

            await EnsureRoleAsync(roleManager, UsersRole);

            foreach (string coreRole in CoreRoles)
            {
                await EnsureRoleAsync(roleManager, coreRole);
            }

            await EnsureUserAsync(
                userManager,
                userName: "admin",
                password: "admin",
                roleNames: new[] { Roles.Administrators },
                email: "admin@g2h.org",
                name: "Admin",
                surname: "User");

            await EnsureUserAsync(
                userManager,
                userName: "user",
                password: "user",
                roleNames: new[] { UsersRole },
                email: "user@g2h.org",
                name: "Normal",
                surname: "User");

            await EnsureUserAsync(
                userManager,
                userName: "cjdutoit",
                password: "P@ssword!",
                roleNames: new[] { Roles.Administrators },
                email: "christo@dutoit.co.uk",
                name: "Christo",
                surname: "du Toit",
                dateOfBirth: new DateOnly(1977, 10, 8));
        }

        // LocalDB creates databases with AUTO_CLOSE ON (inherited from the model database), which
        // cold-starts the database on every connection and can surface as a transient 0x89c5010a on
        // connection open. Turn it off so the database stays warm. Only attempted for a (localdb)
        // data source (a no-op on real SQL Server, where AUTO_CLOSE is already OFF) and best-effort:
        // failures (e.g. the account cannot ALTER DATABASE) are ignored.
        private static async Task DisableAutoCloseForLocalDbAsync(SecurityDbContext securityDbContext)
        {
            DbConnection connection = securityDbContext.Database.GetDbConnection();

            bool isLocalDb = connection.DataSource?.Contains(
                "(localdb)", StringComparison.OrdinalIgnoreCase) is true;

            if (isLocalDb is false)
            {
                return;
            }

            try
            {
                string databaseName = connection.Database.Replace("]", "]]");

                await securityDbContext.Database.ExecuteSqlRawAsync(
                    $"ALTER DATABASE [{databaseName}] SET AUTO_CLOSE OFF WITH NO_WAIT;");
            }
            catch
            {
                // Best-effort dev-experience tweak; ignore when AUTO_CLOSE cannot be changed.
            }
        }

        private static async Task EnsureRoleAsync(
            RoleManager<AppRole> roleManager,
            string roleName)
        {
            if ((await roleManager.RoleExistsAsync(roleName)) is false)
            {
                await roleManager.CreateAsync(new AppRole { Name = roleName });
            }
        }

        private static async Task EnsureUserAsync(
            UserManager<AppUser> userManager,
            string userName,
            string password,
            string[] roleNames,
            string email,
            string name,
            string surname,
            DateOnly? dateOfBirth = null)
        {
            AppUser user = await userManager.FindByNameAsync(userName);

            if (user is null)
            {
                user = new AppUser
                {
                    UserName = userName,
                    Email = email,
                    EmailConfirmed = true,
                    Name = name,
                    Surname = surname,
                    DateOfBirth = dateOfBirth
                };

                await userManager.CreateAsync(user, password);
            }

            // Deliberately outside the creation branch. Membership used to be granted only to
            // users this seed had just created, so adding a role name to the list changed
            // nothing on any database that had already been seeded — the rows would appear,
            // nobody would hold them, and the endpoints would go on answering 403.
            foreach (string roleName in roleNames)
            {
                await EnsureUserInRoleAsync(userManager, user, roleName);
            }
        }

        private static async Task EnsureUserInRoleAsync(
            UserManager<AppUser> userManager,
            AppUser user,
            string roleName)
        {
            if ((await userManager.IsInRoleAsync(user, roleName)) is false)
            {
                await userManager.AddToRoleAsync(user, roleName);
            }
        }
    }
}
