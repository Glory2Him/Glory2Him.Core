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
using G2H.EventEnvelope.Client.Models.Clients.Exceptions;
using G2H.EventEnvelope.Client.Models.Foundations;
using G2H.EventEnvelope.Client.Models.Foundations.Exceptions;
using G2H.EventEnvelope.Client.Services.Foundations.Events;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xeptions;

namespace G2H.EventEnvelope.Client.Clients
{
    public class EventEnvelopeClient : IEventEnvelopeClient
    {
        private readonly IEventEnvelopeService eventEnvelopeService;

        public EventEnvelopeClient()
        {
            IServiceProvider serviceProvider = RegisterServices();
            this.eventEnvelopeService = serviceProvider.GetRequiredService<IEventEnvelopeService>();
        }

        internal EventEnvelopeClient(IEventEnvelopeService eventEnvelopeService)
        {
            this.eventEnvelopeService = eventEnvelopeService;
        }

        public async ValueTask<EventEnvelope<T>> CreateAsync<T>(T content)
        {
            try
            {
                return await this.eventEnvelopeService.CreateAsync(content);
            }
            catch (EventEnvelopeValidationException eventEnvelopeValidationException)
            {
                throw CreateEventEnvelopeClientValidationException(
                    eventEnvelopeValidationException.InnerException as Xeption);
            }
            catch (EventEnvelopeDependencyValidationException eventEnvelopeDependencyValidationException)
            {
                throw CreateEventEnvelopeClientValidationException(
                    eventEnvelopeDependencyValidationException.InnerException as Xeption);
            }
            catch (EventEnvelopeDependencyException eventEnvelopeDependencyException)
            {
                throw CreateEventEnvelopeClientDependencyException(
                    eventEnvelopeDependencyException.InnerException as Xeption);
            }
            catch (EventEnvelopeServiceException eventEnvelopeServiceException)
            {
                throw CreateEventEnvelopeClientDependencyException(
                    eventEnvelopeServiceException.InnerException as Xeption);
            }
            catch (Exception exception)
            {
                throw CreateEventEnvelopeClientServiceException(exception);
            }
        }

        public async ValueTask<EventEnvelope<T>> CreateNextAsync<TSource, T>(
            EventEnvelope<TSource> sourceEnvelope,
            T content)
        {
            try
            {
                return await this.eventEnvelopeService.CreateNextAsync(sourceEnvelope, content);
            }
            catch (EventEnvelopeValidationException eventEnvelopeValidationException)
            {
                throw CreateEventEnvelopeClientValidationException(
                    eventEnvelopeValidationException.InnerException as Xeption);
            }
            catch (EventEnvelopeDependencyValidationException eventEnvelopeDependencyValidationException)
            {
                throw CreateEventEnvelopeClientValidationException(
                    eventEnvelopeDependencyValidationException.InnerException as Xeption);
            }
            catch (EventEnvelopeDependencyException eventEnvelopeDependencyException)
            {
                throw CreateEventEnvelopeClientDependencyException(
                    eventEnvelopeDependencyException.InnerException as Xeption);
            }
            catch (EventEnvelopeServiceException eventEnvelopeServiceException)
            {
                throw CreateEventEnvelopeClientDependencyException(
                    eventEnvelopeServiceException.InnerException as Xeption);
            }
            catch (Exception exception)
            {
                throw CreateEventEnvelopeClientServiceException(exception);
            }
        }

        private static EventEnvelopeClientValidationException CreateEventEnvelopeClientValidationException(
            Xeption? innerException)
        {
            return new EventEnvelopeClientValidationException(
                message: "Event envelope client validation error occurred, fix errors and try again.",
                innerException!,
                data: innerException?.Data!);
        }

        private static EventEnvelopeClientDependencyException CreateEventEnvelopeClientDependencyException(
            Xeption? innerException)
        {
            return new EventEnvelopeClientDependencyException(
                message: "Event envelope client dependency error occurred, please contact support.",
                innerException!,
                data: innerException?.Data!);
        }

        private static EventEnvelopeClientServiceException CreateEventEnvelopeClientServiceException(
            Exception innerException)
        {
            return new EventEnvelopeClientServiceException(
                message: "Event envelope client service error occurred, please contact support.",
                innerException!,
                data: innerException?.Data!);
        }

        private static IServiceProvider RegisterServices()
        {
            var serviceCollection = new ServiceCollection()
                .AddSingleton<IHttpContextAccessor, HttpContextAccessor>()
                .AddTransient<IDateTimeBroker, DateTimeBroker>()
                .AddTransient<IIdentifierBroker, IdentifierBroker>()
                .AddTransient<ISecurityBroker, SecurityBroker>()
                .AddTransient<IEventEnvelopeService, EventEnvelopeService>();

            IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

            return serviceProvider;
        }
    }
}
