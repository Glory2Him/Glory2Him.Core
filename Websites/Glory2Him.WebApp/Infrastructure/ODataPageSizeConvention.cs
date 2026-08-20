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

using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.OData.Query;

namespace Glory2Him.WebApp.Infrastructure
{
    /// <summary>
    /// Hands the host's configured page size to every bare <c>[EnableQuery]</c>, so an exposer
    /// declares only that its collection read is queryable and the host decides how large a page
    /// is. This replaced the <c>#if DEBUG</c> pair that used to sit above each of those actions:
    /// the build configuration is the wrong thing to decide a runtime posture, and it forced the
    /// number to be restated on every action that gained one.
    ///
    /// <para><b>How it takes effect.</b> <see cref="EnableQueryAttribute"/> is an MVC filter, so
    /// MVC reads it off the method once while it builds the application model and keeps that one
    /// instance in the action's descriptor for the life of the process. Setting
    /// <see cref="EnableQueryAttribute.PageSize"/> here — before the descriptors are built — is
    /// therefore the same as having written the number in the attribute.</para>
    ///
    /// <para><b>What it leaves alone.</b> An action that names its own page size keeps it.
    /// <see cref="EnableQueryAttribute.PageSize"/> reads back as <c>0</c> until something assigns
    /// it and its setter refuses anything below <c>1</c>, so <c>0</c> is exactly "this action
    /// named no page size of its own" and is the only case this fills in.</para>
    ///
    /// <para><b>What it does not reach.</b> Only the attributes hanging off the application model
    /// — a controller's and an action's. An <see cref="EnableQueryAttribute"/> registered globally
    /// through <c>MvcOptions.Filters</c> lives outside that model, so it would keep the framework
    /// default of no paging at all. Nothing registers one that way today; a change that starts to
    /// must carry its own page size, because this will not supply one.</para>
    /// </summary>
    public sealed class ODataPageSizeConvention : IApplicationModelConvention
    {
        /// <summary>
        /// The configuration key holding the page size. It must name a positive number — see
        /// <see cref="FromConfiguration"/> for why a value that would remove paging is refused
        /// rather than honoured.
        /// </summary>
        public const string ConfigurationKey = "OData:PageSize";

        /// <summary>
        /// The page size used when configuration names none, so a host that was never configured
        /// still pages rather than serving whole tables.
        /// </summary>
        public const int FallbackPageSize = 50;

        private readonly int pageSize;

        public ODataPageSizeConvention(int pageSize)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

            this.pageSize = pageSize;
        }

        /// <summary>
        /// Reads <see cref="ConfigurationKey"/>, falling back to <see cref="FallbackPageSize"/>
        /// when it is absent. Configuration is read once here rather than per request, which is
        /// what lets the acceptance suite raise the size through
        /// <c>Program.TestConfigurationOverrides</c> — that hook runs before the first
        /// <c>AddControllers</c> call, so a test host's value is the one this sees.
        ///
        /// <para>A configured value below <c>1</c> is refused rather than treated as "no paging".
        /// Paging is the only limit standing between one request and a whole table, and a host
        /// that lost it would keep serving — no exception, no log line, nothing to notice — so a
        /// setting that would remove it stops the host at startup instead.</para>
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The key names a value below <c>1</c>.
        /// </exception>
        public static ODataPageSizeConvention FromConfiguration(IConfiguration configuration)
        {
            int configuredPageSize =
                configuration.GetValue<int?>(ConfigurationKey) ?? FallbackPageSize;

            if (configuredPageSize < 1)
            {
                throw new InvalidOperationException(
                    $"Configuration '{ConfigurationKey}' is {configuredPageSize}, which would "
                        + "serve every queryable collection read unpaged. It must be 1 or greater; "
                        + $"remove the key to take the default of {FallbackPageSize}.");
            }

            return new ODataPageSizeConvention(configuredPageSize);
        }

        public void Apply(ApplicationModel application)
        {
            foreach (ControllerModel controller in application.Controllers)
            {
                ApplyPageSize(controller.Filters);

                foreach (ActionModel action in controller.Actions)
                {
                    ApplyPageSize(action.Filters);
                }
            }
        }

        private void ApplyPageSize(IList<IFilterMetadata> filters)
        {
            foreach (EnableQueryAttribute attribute in filters.OfType<EnableQueryAttribute>())
            {
                if (attribute.PageSize == 0)
                {
                    attribute.PageSize = this.pageSize;
                }
            }
        }
    }
}
