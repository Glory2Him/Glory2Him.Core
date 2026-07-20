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

using System.Threading.Tasks;
using FluentAssertions;
using Force.DeepCloner;
using G2H.StorageClient.Tests.Unit.Models.Foundations.Users;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace G2H.StorageClient.Tests.Unit.Services.Foundations.Operations
{
    public partial class OperationServiceTests
    {
        [Fact]
        public async Task InsertAsyncShouldMarkEntityAsAddedSaveChangesAndDetach()
        {
            // Given
            User randomUser = CreateRandomUser();
            User inputUser = randomUser;
            User expectedUser = inputUser.DeepClone();

            // When
            User actualUser = await operationService.InsertAsync(@object: inputUser);

            // Then
            actualUser.Should().BeEquivalentTo(expectedUser);

            storageBrokerMock.Verify(broker =>
                broker.UpdateObjectStateAsync(inputUser, EntityState.Added),
                    Times.Once);

            storageBrokerMock.Verify(broker =>
                broker.SaveChangesAsync(default),
                    Times.Once);

            storageBrokerMock.Verify(broker =>
                broker.UpdateObjectStateAsync(inputUser, EntityState.Detached),
                    Times.Once);

            storageBrokerMock.VerifyNoOtherCalls();
        }
    }
}
