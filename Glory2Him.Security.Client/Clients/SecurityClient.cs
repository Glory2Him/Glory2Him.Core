// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System;
using Glory2Him.Security.Client.Brokers.DateTimes;
using Glory2Him.Security.Client.Clients.Audits;
using Glory2Him.Security.Client.Clients.Users;
using Glory2Him.Security.Client.Services.Foundations.Audits;
using Glory2Him.Security.Client.Services.Foundations.Users;
using Glory2Him.Security.Client.Services.Orchestrations.Audits;
using Microsoft.Extensions.DependencyInjection;

namespace Glory2Him.Security.Client.Clients
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
