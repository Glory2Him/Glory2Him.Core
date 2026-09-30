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
using FluentAssertions;
using Force.DeepCloner;
using G2H.StorageClient.Tests.Unit.Models.Foundations.Users;
using Moq;

namespace G2H.StorageClient.Tests.Unit.Clients
{
    public partial class EFCoreClientTests
    {
        [Fact]
        public async Task ShouldReturnOnlyTheRowsTheQuerySelectsAsync()
        {
            // Given
            string matchingSurname = GetRandomString();
            List<User> matchingUsers = CreateRandomUsers();
            matchingUsers.ForEach(user => user.Surname = matchingSurname);
            List<User> nonMatchingUsers = CreateRandomUsers();
            IQueryable<User> storageUsers = matchingUsers.Concat(nonMatchingUsers).AsQueryable();
            List<User> expectedUsers = matchingUsers.DeepClone();
            CancellationToken inputCancellationToken = new CancellationTokenSource().Token;

            Func<IQueryable<User>, IQueryable<User>> inputQuery = users =>
                users.Where(user => user.Surname == matchingSurname);

            operationServiceMock.Setup(service =>
                service.SelectListAsync(inputQuery, inputCancellationToken))
                    .ReturnsAsync((Func<IQueryable<User>, IQueryable<User>> query, CancellationToken _) =>
                        query(storageUsers).ToList());

            // When
            IReadOnlyList<User> actualUsers =
                await efCoreClient.SelectListAsync(inputQuery, inputCancellationToken);

            // Then
            actualUsers.Should().BeEquivalentTo(expectedUsers);

            operationServiceMock.Verify(service =>
                service.SelectListAsync(inputQuery, inputCancellationToken),
                    Times.Once);

            operationServiceMock.VerifyNoOtherCalls();
        }
    }
}
