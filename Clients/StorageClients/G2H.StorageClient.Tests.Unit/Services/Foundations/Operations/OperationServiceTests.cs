// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’" 
// https://mark.bible/mark-16-15
// https://john.bible/john-14-6 
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Linq;
using G2H.StorageClient.Brokers.Storages;
using G2H.StorageClient.Services.Foundations.Operations;
using G2H.StorageClient.Tests.Unit.Models.Foundations.Users;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using Tynamix.ObjectFiller;

namespace G2H.StorageClient.Tests.Unit.Services.Foundations.Operations
{
    public partial class OperationServiceTests
    {
        private readonly Mock<IStorageBroker> storageBrokerMock;
        private readonly OperationService operationService;
        private readonly Mock<IDbContextTransaction> dbContextTransactionMock;
        public OperationServiceTests()
        {
            storageBrokerMock = new Mock<IStorageBroker>();
            dbContextTransactionMock = new Mock<IDbContextTransaction>();
            this.operationService = new OperationService(storageBrokerMock.Object);
        }

        private static int GetRandomNumber() =>
            new IntRange(min: 2, max: 10).GetValue();

        private static string GetRandomString() =>
            new MnemonicString().GetValue();

        private static List<User> CreateRandomUsers() =>
            CreateUserFiller().Create(count: GetRandomNumber()).ToList();

        private static User CreateRandomUser() =>
            CreateUserFiller().Create();

        private static Filler<User> CreateUserFiller()
        {
            var filler = new Filler<User>();
            filler.Setup().OnProperty(user => user.Id).Use(() => Guid.NewGuid());

            return filler;
        }
    }
}
