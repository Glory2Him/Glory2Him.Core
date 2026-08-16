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

using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Securities;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.Tags
{
    public partial class TagApiTests
    {
        [Theory]
        [InlineData(Roles.Admin)]
        [InlineData(Roles.Reviewer)]
        [InlineData(Roles.Publisher)]
        [InlineData(Roles.TagPublisher)]
        [InlineData(Roles.TagReviewer)]
        [InlineData(Roles.ReadOnly)]
        [InlineData(Roles.TagReadOnly)]
        public async Task ShouldSeedCoreRoleAsync(string roleName)
        {
            // given
            // when
            bool actualRoleExists = await this.apiBroker.RoleExistsAsync(roleName);

            // then
            actualRoleExists.Should().BeTrue();
        }

        [Fact]
        public async Task ShouldGrantCoreAdministratorRoleToTheSeededAdministratorAsync()
        {
            // given
            // when
            IList<string> actualRoles = await this.apiBroker.GetSeededAdministratorRolesAsync();

            // then
            actualRoles.Should().Contain(Roles.Admin);
        }
    }
}
