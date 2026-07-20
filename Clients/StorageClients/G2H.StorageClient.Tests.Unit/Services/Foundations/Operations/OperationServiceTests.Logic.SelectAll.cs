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
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Force.DeepCloner;
using G2H.StorageClient.Tests.Unit.Models.Foundations.Users;
using Moq;

namespace G2H.StorageClient.Tests.Unit.Services.Foundations.Operations
{
    public partial class OperationServiceTests
    {
        [Fact]
        public async Task SelectAllAsyncShouldOnlyReturnExpectedUsersAsync()
        {
            // Given
            List<User> randomUsers = CreateRandomUsers();
            IQueryable<User> inputUsers = randomUsers.AsQueryable();
            IQueryable<User> storageUsers = inputUsers;
            IQueryable<User> expectedUsers = storageUsers.DeepClone();

            storageBrokerMock.Setup(broker =>
                broker.SelectAllAsync<User>())
                    .ReturnsAsync(storageUsers.AsQueryable());

            // When
            IQueryable<User> actualUsers = await operationService.SelectAllAsync<User>();

            // Then
            actualUsers.Should().BeEquivalentTo(expectedUsers);

            storageBrokerMock.Verify(broker =>
                broker.SelectAllAsync<User>(),
                    Times.Once);

            storageBrokerMock.VerifyNoOtherCalls();
        }
    }
}
