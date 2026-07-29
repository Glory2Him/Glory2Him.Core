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
using System.Threading.Tasks;
using G2H.EventEnvelope.Client.Brokers.DateTimes;
using G2H.EventEnvelope.Client.Brokers.Identifiers;
using G2H.EventEnvelope.Client.Brokers.Securities;
using G2H.EventEnvelope.Client.Models.Foundations;
using G2H.EventEnvelope.Client.Services.Foundations.Events;
using Microsoft.Extensions.DependencyInjection;

namespace G2H.EventEnvelope.Client.Clients
{
    public class EventEnvelopeClient : IEventEnvelopeClient
    {
        private readonly IEventEnvelopeService eventEnvelopeService;

        public EventEnvelopeClient()
        {
            IServiceProvider serviceProvider = RegisterServices();
            eventEnvelopeService = serviceProvider.GetRequiredService<IEventEnvelopeService>();
        }

        public ValueTask<EventEnvelope<T>> CreateAsync<T>(T content)
        {
            return eventEnvelopeService.CreateAsync(content);
        }

        public ValueTask<EventEnvelope<T>> CreateNextAsync<TSource, T>(EventEnvelope<TSource> sourceEnvelope, T content)
        {
            return eventEnvelopeService.CreateNextAsync(sourceEnvelope, content);
        }

        private static IServiceProvider RegisterServices()
        {
            var serviceCollection = new ServiceCollection()
                .AddTransient<IDateTimeBroker, DateTimeBroker>()
                .AddTransient<IIdentifierBroker, IdentifierBroker>()
                .AddTransient<ISecurityBroker, SecurityBroker>()
                .AddTransient<IEventEnvelopeService, EventEnvelopeService>();

            IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

            return serviceProvider;
        }
    }
}
