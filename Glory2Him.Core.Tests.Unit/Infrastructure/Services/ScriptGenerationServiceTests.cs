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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Glory2Him.Core.Infrastructure.Services;

namespace Glory2Him.Core.Tests.Unit.Infrastructure.Services
{
    public class ScriptGenerationServiceTests
    {
        [Fact]
        public void ShouldEmitTheRejectAiAttributionJobFromTheGenerator()
        {
            // given
            var scriptGenerationService = new ScriptGenerationService();

            string prLinterPath = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "../../../../.github/workflows/prLinter.yml"));

            byte[] committedPrLinterBytes = File.ReadAllBytes(prLinterPath);
            string expectedPrLinter = NormalizeLineEndings(File.ReadAllText(prLinterPath));
            string actualPrLinter;

            // when
            try
            {
                scriptGenerationService.GeneratePrLintScript(branchName: "main");
                actualPrLinter = NormalizeLineEndings(File.ReadAllText(prLinterPath));
            }
            finally
            {
                File.WriteAllBytes(prLinterPath, committedPrLinterBytes);
            }

            // then
            actualPrLinter.Should().Be(expectedPrLinter);

            actualPrLinter.Should().Contain(
                "  rejectAiAttribution:\n    name: Reject AI Identity And Attribution\n");
        }

        [Fact]
        public void ShouldEmitTheCommittedBuildScriptFromTheGenerator()
        {
            // given
            var scriptGenerationService = new ScriptGenerationService();
            string buildPath = GetWorkflowPath(workflowFileName: "build.yml");
            byte[] committedBuildBytes = File.ReadAllBytes(buildPath);
            string expectedBuild = NormalizeLineEndings(File.ReadAllText(buildPath));
            string actualBuild;

            // when
            try
            {
                GenerateBuildScript(scriptGenerationService);
                actualBuild = NormalizeLineEndings(File.ReadAllText(buildPath));
            }
            finally
            {
                File.WriteAllBytes(buildPath, committedBuildBytes);
            }

            // then
            actualBuild.Should().Be(expectedBuild);
        }

        [Fact]
        public void ShouldSwitchOnWebSocketsBeforeTheDeploy()
        {
            // given
            var scriptGenerationService = new ScriptGenerationService();
            string buildPath = GetWorkflowPath(workflowFileName: "build.yml");
            byte[] committedBuildBytes = File.ReadAllBytes(buildPath);
            string actualBuild;

            // when
            try
            {
                GenerateBuildScript(scriptGenerationService);
                actualBuild = NormalizeLineEndings(File.ReadAllText(buildPath));
            }
            finally
            {
                File.WriteAllBytes(buildPath, committedBuildBytes);
            }

            // then
            string deployJob = actualBuild.Substring(actualBuild.IndexOf("\n  deploy_webapp:\n"));

            List<string> deploySteps = deployJob
                .Split("\n    - name: ")
                .Skip(1)
                .ToList();

            int loginIndex = deploySteps.FindIndex(step => step.StartsWith("Login to Azure\n"));
            int deployIndex = deploySteps.FindIndex(step => step.StartsWith("Deploy to Azure Web App\n"));

            int webSocketsIndex = deploySteps.FindIndex(step =>
                step.Contains(
                    "resourceGroup=$(az webapp list " +
                    "--query \"[?name=='g2h-dev'].resourceGroup | [0]\" --output tsv)")
                && step.Contains(
                    "az webapp config set --name g2h-dev " +
                    "--resource-group \"$resourceGroup\" --web-sockets-enabled true"));

            loginIndex.Should().BeGreaterThanOrEqualTo(0);
            webSocketsIndex.Should().BeGreaterThan(loginIndex);
            deployIndex.Should().BeGreaterThan(webSocketsIndex);
        }

        [Fact]
        public void ShouldStopTheDeployWhenTheAppRunsOnMoreThanOneInstance()
        {
            // given
            var scriptGenerationService = new ScriptGenerationService();
            string buildPath = GetWorkflowPath(workflowFileName: "build.yml");
            byte[] committedBuildBytes = File.ReadAllBytes(buildPath);
            string actualBuild;

            // when
            try
            {
                GenerateBuildScript(scriptGenerationService);
                actualBuild = NormalizeLineEndings(File.ReadAllText(buildPath));
            }
            finally
            {
                File.WriteAllBytes(buildPath, committedBuildBytes);
            }

            // then
            string deployJob = actualBuild.Substring(actualBuild.IndexOf("\n  deploy_webapp:\n"));

            List<string> deploySteps = deployJob
                .Split("\n    - name: ")
                .Skip(1)
                .ToList();

            int loginIndex = deploySteps.FindIndex(step => step.StartsWith("Login to Azure\n"));
            int deployIndex = deploySteps.FindIndex(step => step.StartsWith("Deploy to Azure Web App\n"));

            int instanceCountIndex = deploySteps.FindIndex(step =>
                step.Contains("set -euo pipefail")
                && step.Contains(
                    "resourceGroup=$(az webapp list " +
                    "--query \"[?name=='g2h-dev'].resourceGroup | [0]\" --output tsv)")
                && step.Contains(
                    "instanceCount=$(az webapp list-instances --name g2h-dev " +
                    "--resource-group \"$resourceGroup\" --query \"length(@)\" --output tsv)")
                && step.Contains("if ! [ \"$instanceCount\" -le 1 ]; then")
                && step.Contains("§ARC12.12 rule 5")
                && step.Contains("exit 1"));

            loginIndex.Should().BeGreaterThanOrEqualTo(0);
            instanceCountIndex.Should().BeGreaterThan(loginIndex);
            deployIndex.Should().BeGreaterThan(instanceCountIndex);
        }

        private static void GenerateBuildScript(ScriptGenerationService scriptGenerationService) =>
            scriptGenerationService.GenerateBuildScript(
                branchName: "main",
                projectName: "Glory2Him.Core",
                dotNetVersion: "10.x");

        private static string GetWorkflowPath(string workflowFileName) =>
            Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "../../../../.github/workflows", workflowFileName));

        private static string NormalizeLineEndings(string text) =>
            text.Replace("\r\n", "\n");
    }
}
