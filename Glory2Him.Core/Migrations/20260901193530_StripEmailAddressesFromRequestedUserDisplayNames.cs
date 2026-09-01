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

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glory2Him.Core.Migrations
{
    /// <summary>
    /// Issue #378, the half no code fix can reach. <c>RequestedUserDisplayName</c> is
    /// deliberately denormalised (§7.9, §18.3): the name is fixed at request time because there
    /// is no join across the two databases, so a row written while a username was still an email
    /// address keeps holding that address no matter what the composer does afterwards.
    ///
    /// <para>THE DOMAIN IS DROPPED, THE LOCAL PART IS KEPT — <c>christo@example.org</c> becomes
    /// <c>christo</c>. The column exists to name a person in a reviewer panel, and blanking it
    /// or replacing it with a placeholder would make several such accounts indistinguishable
    /// there, which §16.7.4 is precisely about. What is left is not an email address, and it is
    /// the same value the sibling migration in the SECURITY database gives that account as its
    /// new username — so the panel and the profile agree rather than drifting apart.</para>
    ///
    /// <para>THE OTHER DATABASE IS THE OTHER HALF and is NOT here.
    /// <c>SeparateUserNamesFromEmailAddresses</c> renames the identity rows themselves under
    /// <c>SecurityDbContext</c> — another context and another connection string entirely
    /// (§12.7.1). Both must run: this one alone leaves new rows safe and old rows leaking; that
    /// one alone leaves the stored copies leaking after the source has been fixed.</para>
    ///
    /// <para>Matched on <c>@</c> rather than rewritten blindly, so it is safe to run twice and
    /// cannot touch a name that was always a real name. A value with no local part at all —
    /// a display name that somehow begins with <c>@</c> — is left for the fallback to answer
    /// rather than being replaced by an empty string that would render as nothing.</para>
    ///
    /// <para>NOT REVERSIBLE, and <see cref="Down"/> says so by doing nothing. The domain is not
    /// recorded anywhere once removed, and reconstructing it would put the leak back.</para>
    /// </summary>
    /// <inheritdoc />
    public partial class StripEmailAddressesFromRequestedUserDisplayNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) =>
            migrationBuilder.Sql(
                @"UPDATE [ApprovalReviewRequests]
                  SET [RequestedUserDisplayName] =
                      LEFT(
                          [RequestedUserDisplayName],
                          CHARINDEX(N'@', [RequestedUserDisplayName]) - 1)
                  WHERE [RequestedUserDisplayName] LIKE N'%@%'
                      AND CHARINDEX(N'@', [RequestedUserDisplayName]) > 1;");

        /// <summary>
        /// Deliberately empty — see the class summary. The removed domain is recorded nowhere,
        /// and a name that is now just a name is not a broken state to recover from.
        /// </summary>
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
