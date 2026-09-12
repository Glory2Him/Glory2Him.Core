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

using ADotNet.Models.Pipelines.GithubPipelines.DotNets;
using YamlDotNet.Serialization;

namespace Glory2Him.Core.Infrastructure.Models.Pipelines
{
    /// <summary>
    /// A <see cref="Job"/> whose <c>environment:</c> is the MAPPING form rather than a bare name.
    ///
    /// <para>ADotNet 5.1.0 types <see cref="Job.Environment"/> as a <see cref="string"/>, which can
    /// carry <c>environment: Development</c> but not the <c>url:</c> beside it — and that URL is
    /// what puts a clickable link on the deployment in GitHub's UI. Shadowing the property with a
    /// richer type keeps the generator authoritative over the whole block; without it, regenerating
    /// would silently drop a key somebody added by hand and nothing would fail.</para>
    ///
    /// <para>Bumping the package to obtain the key is deliberately not the answer: a dependency
    /// bump is a design decision (design 12.10 rule 8), and this needs no new dependency.</para>
    /// </summary>
    internal class DeploymentJob : Job
    {
        [YamlMember(Alias = "environment")]
        public new DeploymentEnvironment Environment { get; set; }
    }
}
