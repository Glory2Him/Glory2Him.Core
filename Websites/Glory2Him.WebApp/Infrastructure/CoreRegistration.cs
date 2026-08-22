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
using Glory2Him.Core.Brokers.Hashes;
using Glory2Him.Core.Brokers.Identifiers;
using Glory2Him.Core.Brokers.Integrities;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Brokers.Storages.Sql;
using Glory2Him.Core.Registrations;
using Glory2Him.Core.Services.Foundations.ApprovalComments;
using Glory2Him.Core.Services.Foundations.ApprovalReviews;
using Glory2Him.Core.Services.Orchestrations.Approvals;
using Glory2Him.Core.Services.Processings.Links;
using Glory2Him.Core.Services.Processings.ContentItems;
using Glory2Him.Core.Services.Foundations.Approvals;
using Glory2Him.Core.Services.Foundations.Tags;
using Glory2Him.Core.Services.Foundations.ApprovalSettings;
using Glory2Him.Core.Services.Foundations.Reactions;
using Glory2Him.Core.Services.Foundations.Links;
using Glory2Him.Core.Services.Foundations.ContentItemSettings;
using Glory2Him.Core.Services.Foundations.ContentItems;
using Glory2Him.Core.Services.Foundations.Comments;
using Glory2Him.Core.Services.Foundations.BibleReferences;
using Glory2Him.Core.Services.Foundations.Associations;

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

            // Stateless, like the two above. Missing entirely until the substrate went live,
            // which is exactly when it started to matter: ContentItemProcessingService takes it
            // and carries five subscriptions, so every ContentItem approval routed through a
            // handler that could not be built. Nothing caught it, because a handler is resolved
            // per delivery rather than at boot, and a handler that throws is recorded against
            // its listener instead of failing the publisher.
            services.AddSingleton<IHashBroker, HashBroker>();
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
            // ONE object behind two doors. Registering the same implementation against two
            // service types would make two of them, because the container keys on the service
            // type rather than the implementation — harmless while this class holds nothing but
            // readonly brokers, and a silent divergence the day it holds anything else.
            //
            // The second door resolves THROUGH the first rather than both resolving a concrete
            // registration, so the implementation type never enters the container. A concrete
            // registration would be a second way to obtain the service, and one that a host
            // registering Core's own AddApprovalReviewService could give a different lifetime.
            services.AddScoped<IApprovalReviewService, ApprovalReviewService>();

            // The workflow's own write seam, separate from the public service the controllers
            // bind to. Same implementation, a narrower door: it carries only the dismissal the
            // workflow makes on its own behalf, so a controller holding the public service does
            // not acquire that capability along with it.
            //
            // `internal` states the intent rather than enforcing it against this assembly —
            // Core names Glory2Him.WebApp in InternalsVisibleTo. What it does enforce is the
            // idiomatic route: a public controller cannot take an internal type through its
            // constructor (CS0051), so reaching this from the portal takes a deliberate,
            // conspicuous service-locator call rather than ordinary injection.
            services.AddScoped<IApprovalReviewWorkflowService>(provider =>
                (ApprovalReviewService)provider.GetRequiredService<IApprovalReviewService>());
            services.AddScoped<IApprovalService, ApprovalService>();

            // The workflow's own Approval surface, separate from the public service the
            // controllers bind to. Same implementation, resolved through the public door so
            // there is one object; a narrower type so the orchestration cannot reach a
            // caller-gated twin by accident (#287).
            services.AddScoped<IApprovalWorkflowService>(provider =>
                (ApprovalService)provider.GetRequiredService<IApprovalService>());

            // Scoped for the same reason the foundations are: it reaches them, and through
            // them the DbContext.
            services.AddScoped<IApprovalOrchestrationService, ApprovalOrchestrationService>();

            // The remaining ten. Not exposed by any endpoint this host serves — they are here
            // because this host now BINDS all 109 subscriptions, and a subscription resolves its
            // service out of a scope when a fact arrives. Registering only the five the
            // controllers use would leave the other ten bindings throwing
            // InvalidOperationException at delivery time, which the substrate records as a
            // failed delivery and nothing surfaces. Bind a subscription, register its service.
            services.AddScoped<IContentItemService, ContentItemService>();
            services.AddScoped<ILinkService, LinkService>();
            services.AddScoped<IReactionService, ReactionService>();
            services.AddScoped<ICommentService, CommentService>();
            services.AddScoped<IBibleReferenceService, BibleReferenceService>();
            services.AddScoped<IAssociationService, AssociationService>();
            services.AddScoped<IApprovalSettingService, ApprovalSettingService>();
            services.AddScoped<IContentItemSettingService, ContentItemSettingService>();
            services.AddScoped<IContentItemProcessingService, ContentItemProcessingService>();
            services.AddScoped<ILinkProcessingService, LinkProcessingService>();

            // This host DOES wire the substrate, and the scoped lifetimes above are what makes
            // that safe. EventSubscriptionRegistration no longer holds services; it opens a
            // scope per delivery and resolves from it, so a handler gets its own DbContext for
            // the life of one fact — the same lifetime it would have serving one request.
            //
            // That matters because delivery is serialised WITHIN a publish but parallel ACROSS
            // publishes: eight concurrent publishes were measured running eight handlers at
            // once, on eight threads. Services captured once at registration would have handed
            // all eight the same DbContext.
            services.AddSingleton<IEventSubscriptionRegistration, EventSubscriptionRegistration>();

            return services;
        }

        /// <summary>
        /// Brings the event substrate up: registers the participant, every event address, and
        /// all 109 subscriptions. Idempotent, and safe to call once at startup.
        /// </summary>
        /// <remarks>
        /// Until this call existed the substrate was dormant — every handler on every service
        /// was unreachable, and the reactive half of the approval workflow did not run at all.
        /// Calling it is what makes a published fact reach the handler subscribed to it.
        /// </remarks>
        public static async Task UseCoreEventSubstrateAsync(this IServiceProvider services)
        {
            IEventSubscriptionRegistration registration =
                services.GetRequiredService<IEventSubscriptionRegistration>();

            await registration.RegisterAsync();
        }
    }
}
