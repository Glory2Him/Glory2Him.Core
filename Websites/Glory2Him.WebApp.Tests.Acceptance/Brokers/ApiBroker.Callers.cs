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

namespace Glory2Him.WebApp.Tests.Acceptance.Brokers
{
    /// <summary>
    /// Chooses who the next requests are made as. A gate is only proven by the callers it turns
    /// away, and one fixed principal cannot express "not the owner" or "holds no role".
    ///
    /// <para>Tests in a collection run one at a time, so setting the client's default headers is
    /// safe; every test class resets to the seeded administrator in its constructor so the
    /// acting caller is never inherited from whatever ran before.</para>
    /// </summary>
    public partial class ApiBroker
    {
        /// <summary>The seeded administrator, carrying that account's real Identity roles.</summary>
        public void ActAsSeededAdministrator()
        {
            ClearCallerHeaders();
        }

        /// <summary>No credentials at all — the request arrives unauthenticated.</summary>
        public void ActAsAnonymous()
        {
            ClearCallerHeaders();
            this.httpClient.DefaultRequestHeaders.Add(TestAuthHandler.AnonymousHeader, "true");
        }

        /// <summary>
        /// An authenticated caller holding NO role — the ordinary contributor. Returns the user
        /// id so a test can assert ownership against it.
        /// </summary>
        public string ActAsContributor() =>
            ActAs(Guid.NewGuid().ToString());

        /// <summary>An authenticated caller holding exactly the roles given.</summary>
        public string ActAs(string userId, params string[] roleNames)
        {
            ClearCallerHeaders();

            // The user-id header is what tells the handler a caller was named. The roles header
            // is only sent when there ARE roles, because an empty header value is not
            // transmitted at all — relying on it would make "holds no role" silently become
            // "the seeded administrator".
            this.httpClient.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId);

            if (roleNames is not null && roleNames.Length > 0)
            {
                this.httpClient.DefaultRequestHeaders.Add(
                    TestAuthHandler.RolesHeader,
                    string.Join(",", roleNames));
            }

            return userId;
        }

        private void ClearCallerHeaders()
        {
            this.httpClient.DefaultRequestHeaders.Remove(TestAuthHandler.AnonymousHeader);
            this.httpClient.DefaultRequestHeaders.Remove(TestAuthHandler.UserIdHeader);
            this.httpClient.DefaultRequestHeaders.Remove(TestAuthHandler.RolesHeader);
        }
    }
}
