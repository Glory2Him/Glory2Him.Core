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

using G2H.Security.Client.Models.Clients;
using Glory2Him.Core.Brokers.DateTimes;
using Glory2Him.Core.Brokers.EventEnvelopes;
using Glory2Him.Core.Brokers.Events;
using Glory2Him.Core.Brokers.Identifiers;
using Glory2Him.Core.Brokers.Integrities;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Brokers.Storages.Sql;
using Glory2Him.Core.Services.Foundations.ApprovalComments;
using Glory2Him.Core.Services.Foundations.ApprovalReviews;
using Glory2Him.Core.Services.Orchestrations.Approvals;
using Glory2Him.Core.Services.Foundations.Approvals;
using Glory2Him.Core.Services.Foundations.Tags;

namespace Glory2Him.WebApp.Infrastructure
{
    /// <summary>
    /// Wires the slice of <c>Glory2Him.Core</c> that the exposed endpoints need into the portal host.
    /// Everything here is internal to Core and reachable only because Core names this assembly
    /// in <c>InternalsVisibleTo</c>.
    /// </summary>
    public static class CoreRegistration
    {
        /// <summary>
        /// Registers the foundation services this host exposes and the brokers behind them.
        /// </summary>
        /// <remarks>
        /// <para><b>Lifetimes.</b> This deliberately does not call
        /// <c>ServiceRegistration.AddTagService()</c>. That helper registers the service as a
        /// singleton for one reason only — <c>EventSubscriptionRegistration</c> binds the
        /// substrate handlers into the singleton <c>IEventBroker</c> as method groups, and a
        /// shorter lifetime would be captured by it. This host wires no event subscriptions, so
        /// that reason does not apply, and a singleton would be actively wrong here: the service
        /// reaches <c>StorageBroker</c>, which is a <c>DbContext</c> and is neither thread-safe
        /// nor shareable across concurrent requests. The service and its request-bound brokers
        /// are therefore scoped, and only the stateless brokers are singletons.</para>
        ///
        /// <para><b>Configuration.</b> The brokers resolve lazily, so registering them costs
        /// nothing until a Core endpoint is called. Serving one needs
        /// <c>ConnectionStrings:Glory2HimConnectionString</c> (Core's schema),
        /// <c>ConnectionStrings:EventHighwayConnectionString</c> (the event substrate) and the
        /// envelope signing keys section.</para>
        /// </remarks>
        public static IServiceCollection AddCoreServices(this IServiceCollection services)
        {
            // SecurityAuditBroker reads the caller's ClaimsPrincipal off the ambient
            // HttpContext in its constructor, so it needs the accessor and a per-request
            // lifetime — a longer-lived instance would freeze the first caller's identity.
            services.AddHttpContextAccessor();

            // The defaults already name the audit members these entities carry (CreatedBy,
            // UpdatedWhen, IsDeleted, DeletionReason, ...), so no overrides are needed.
            services.AddSingleton(new SecurityConfigurations());

            services.AddSingleton<IDateTimeBroker, DateTimeBroker>();
            services.AddSingleton<IIdentifierBroker, IdentifierBroker>();
            services.AddSingleton<IEnvelopeIntegrityBroker, EnvelopeIntegrityBroker>();
            services.AddSingleton<IEventBroker, EventBroker>();
            services.AddTransient<ILoggingBroker, LoggingBroker>();

            // Scoped, not singleton. EventEnvelopeBroker news up an EventEnvelopeClient in its
            // constructor, that client resolves its IEventEnvelopeService once, and the
            // SecurityBroker underneath reads httpContextAccessor.HttpContext?.User in ITS
            // constructor. A singleton would therefore stamp every envelope in the process with
            // whichever principal happened to be current the first time it was built.
            services.AddScoped<IEventEnvelopeBroker, EventEnvelopeBroker>();

            services.AddScoped<IStorageBroker, StorageBroker>();
            services.AddScoped<IAccessBroker, AccessBroker>();
            services.AddScoped<ISecurityAuditBroker, SecurityAuditBroker>();
            // The service INTERFACES are public; the implementations are not. Binding the two is
            // only possible from inside Core's friend set, which is the point — the host names
            // the contract, Core keeps the implementation. Each controller then takes its one
            // service through an ordinary public constructor and the default activator builds it.
            services.AddScoped<ITagService, TagService>();
            services.AddScoped<IApprovalCommentService, ApprovalCommentService>();
            services.AddScoped<IApprovalReviewService, ApprovalReviewService>();
            services.AddScoped<IApprovalService, ApprovalService>();

            // Scoped for the same reason the foundations are: it reaches them, and through
            // them the DbContext. ServiceRegistration.AddApprovalOrchestrationService()
            // registers a singleton instead, which is right only for a host that binds the
            // substrate handlers into the singleton event broker as method groups. This host
            // wires no subscriptions, so it takes the request-bound lifetime.
            services.AddScoped<IApprovalOrchestrationService, ApprovalOrchestrationService>();

            return services;
        }
    }
}
