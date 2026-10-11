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
using System.Linq.Expressions;
using Glory2Him.Core.Brokers.LiveUpdates;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Services.Foundations.LiveUpdates;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.LiveUpdates
{
    public partial class LiveUpdateServiceTests
    {
        private readonly Mock<ILiveUpdateBroker> liveUpdateBrokerMock;
        private readonly Mock<ILoggingBroker> loggingBrokerMock;
        private readonly ILiveUpdateService liveUpdateService;

        public LiveUpdateServiceTests()
        {
            this.liveUpdateBrokerMock = new Mock<ILiveUpdateBroker>();
            this.loggingBrokerMock = new Mock<ILoggingBroker>();

            this.liveUpdateService = new LiveUpdateService(
                liveUpdateBroker: this.liveUpdateBrokerMock.Object,
                loggingBroker: this.loggingBrokerMock.Object);
        }

        private static Expression<Func<Xeption, bool>> SameExceptionAs(Xeption expectedException) =>
            actualException => actualException.SameExceptionAs(expectedException);
    }
}
