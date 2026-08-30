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
using Glory2Him.Core.Brokers.Storages.Identity;
using Glory2Him.Core.Services.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Services.Foundations.IdentityUsers;
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
            // SecurityAuditBroker no longer reads the ambient HttpContext — it resolves its
            // actor from the envelope's SecurityContext instead — but the accessor is still
            // needed. EventEnvelopeBroker's EventEnvelopeClient builds its OWN separate internal
            // DI container (see the comment below) with its own IHttpContextAccessor
            // registration, so it never resolves THIS registration directly; it works only
            // because Microsoft.AspNetCore.Http.HttpContextAccessor keeps its value in a
            // process-wide static AsyncLocal that every instance of that concrete type reads,
            // and that field is populated per request from whichever IHttpContextAccessor this
            // app's own root provider hands the framework — which is this registration. Removing
            // it would leave that static field forever unpopulated. The host's own services
            // (e.g. ASP.NET Core Identity's SignInManager) depend on it too.
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

            // Security-sensitive, unlike the stateless brokers above: left Scoped rather than
            // promoted to Singleton even though it holds nothing today but a self-constructed
            // SecurityClient and the singleton SecurityConfigurations. HashBroker,
            // EnvelopeIntegrityBroker, EventBroker, DateTimeBroker and IdentifierBroker carry no
            // per-caller identity even in principle, so a future stateful constructor is not a
            // security concern for them; this broker's entire job is attributing actions to
            // actors, so the same mistake here would misattribute every CreatedBy/UpdatedBy in
            // the process. The accepted cost of that margin: SecurityClient's constructor builds
            // its own small internal DI container, so this rebuilds it once per scope rather than
            // once per process.
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

            // The remaining eleven. Not exposed by any endpoint this host serves — they are here
            // because this host now BINDS every subscription, and a subscription resolves its
            // service out of a scope when a fact arrives. Registering only the five the
            // controllers use would leave the other bindings throwing
            // InvalidOperationException at delivery time, which the substrate records as a
            // failed delivery and nothing surfaces. Bind a subscription, register its service.
            services.AddScoped<IApprovalReviewRequestService, ApprovalReviewRequestService>();

            // The read-only identity-store window (design 12.7.1). Scoped like every other
            // DbContext here: it is one, and a singleton would capture a connection for the life
            // of the process.
            services.AddScoped<IIdentityCoreStorageBroker, IdentityCoreStorageBroker>();
            services.AddScoped<IIdentityUserService, IdentityUserService>();

            // The workflow's own retirement seam (§7.9 rule 6), resolved through the public door
            // so there is one object. The system identity it runs under carries no roles, which
            // is exactly why the public withdraw verb cannot serve that rule.
            services.AddScoped<IApprovalReviewRequestWorkflowService>(provider =>
                (ApprovalReviewRequestService)provider
                    .GetRequiredService<IApprovalReviewRequestService>());

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
    }
}
