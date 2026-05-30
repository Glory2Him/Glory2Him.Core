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
using G2H.Security.Client.Brokers.DateTimes;
using G2H.Security.Client.Clients.Audits;
using G2H.Security.Client.Clients.Users;
using G2H.Security.Client.Services.Foundations.Audits;
using G2H.Security.Client.Services.Foundations.Users;
using G2H.Security.Client.Services.Orchestrations.Audits;
using Microsoft.Extensions.DependencyInjection;

namespace G2H.Security.Client.Clients
{
    public class SecurityClient : ISecurityClient
    {
        public SecurityClient()
        {
            IServiceProvider serviceProvider = RegisterServices();
            InitializeClients(serviceProvider);
        }

        public IUserClient Users { get; private set; }
        public IAuditClient Audits { get; private set; }

        private void InitializeClients(IServiceProvider serviceProvider)
        {
            Users = serviceProvider.GetRequiredService<IUserClient>();
            Audits = serviceProvider.GetRequiredService<IAuditClient>();
        }

        private static IServiceProvider RegisterServices()
        {
            var serviceCollection = new ServiceCollection()
                .AddTransient<IDateTimeBroker, DateTimeBroker>()
                .AddTransient<IAuditService, AuditService>()
                .AddTransient<IUserService, UserService>()
                .AddTransient<IAuditOrchestrationService, AuditOrchestrationService>()
                .AddTransient<IUserClient, UserClient>()
                .AddTransient<IAuditClient, AuditClient>();

            IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

            return serviceProvider;
        }
    }
}
