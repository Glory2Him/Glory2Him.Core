// --------------------------------------------------------------------------------
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// https://mark.bible/mark-16-15
// --------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using ADotNet.Clients;
using ADotNet.Models.Pipelines.GithubPipelines.DotNets;
using ADotNet.Models.Pipelines.GithubPipelines.DotNets.Tasks;
using ADotNet.Models.Pipelines.GithubPipelines.DotNets.Tasks.SetupDotNetTaskV3s;

namespace Glory2Him.Core.Infrastructure.Services
{
    internal class ScriptGenerationService
    {
        private readonly ADotNetClient adotNetClient;

        public ScriptGenerationService() =>
            adotNetClient = new ADotNetClient();

        public void GenerateBuildScript(string branchName, string projectName, string dotNetVersion)
        {
            var githubPipeline = new GithubPipeline
            {
                Name = "Build",

                OnEvents = new Events
                {
                    Push = new PushEvent { Branches = [branchName] },

                    PullRequest = new PullRequestEvent
                    {
                        Types = ["opened", "synchronize", "reopened", "closed"],
                        Branches = [branchName]
                    }
                },

                Jobs = new Dictionary<string, Job>
                {
                    {
                        "build",
                        new Job
                        {
                            Name = "Build",
                            RunsOn = BuildMachines.WindowsLatest,

                            EnvironmentVariables = new Dictionary<string, string>
                            {
                            },

                            Steps = new List<GithubTask>
                            {
                                new CheckoutTaskV3
                                {
                                    Name = "Check out"
                                },

                                new SetupDotNetTaskV3
                                {
                                    Name = "Setup .Net",

                                    With = new TargetDotNetVersionV3
                                    {
                                        DotNetVersion = dotNetVersion
                                    }
                                },

                                new RestoreTask
                                {
                                    Name = "Restore"
                                },

                                new DotNetBuildTask
                                {
                                    Name = "Build"
                                },

                                new TestTask
                                {
                                    Name = "Run Unit Tests",
                                    Run = "dotnet test **/*Tests.Unit.csproj --no-build --verbosity normal"
                                },

                                new TestTask
                                {
                                    Name = "Run Acceptance Tests",
                                    Run = "dotnet test **/*Tests.Acceptance.csproj --no-build --verbosity normal"
                                }
                            }
                        }
                    },
                    {
                        "add_tag",
                        new TagJob(
                            runsOn: BuildMachines.UbuntuLatest,
                            dependsOn: "build",
                            projectRelativePath: $"{projectName}/{projectName}.csproj",
                            githubToken: "${{ secrets.PAT_FOR_TAGGING }}",
                            branchName: branchName)
                        {
                            Name = "Tag and Release"
                        }
                    },
                }
            };

            string buildScriptPath = "../../../../.github/workflows/build.yml";
            string directoryPath = Path.GetDirectoryName(buildScriptPath);

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            adotNetClient.SerializeAndWriteToFile(
                adoPipeline: githubPipeline,
                path: buildScriptPath);
        }

        public void GeneratePrLintScript(string branchName)
        {
            var githubPipeline = new GithubPipeline
            {
                Name = "PR Linter",

                OnEvents = new Events
                {
                    PullRequest = new PullRequestEvent
                    {
                        Types = ["opened", "edited", "synchronize", "reopened", "closed"],
                        Branches = [branchName]
                    }
                },

                Jobs = new Dictionary<string, Job>
                {
                    {
                        "label",
                        new LabelJobV2(runsOn: BuildMachines.UbuntuLatest)
                        {
                            Name = "Label",
                            Permissions = new Dictionary<string, string>
                            {
                                { "contents", "read" },
                                { "pull-requests", "write" },
                                { "issues", "write" }
                            }
                        }
                    },
                    {
                        "requireIssueOrTask",
                        new RequireIssueOrTaskJob()
                        {
                            Name = "Require Issue Or Task Association",
                        }
                    },
                }
            };

            string buildScriptPath = "../../../../.github/workflows/prLinter.yml";
            string directoryPath = Path.GetDirectoryName(buildScriptPath);

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            adotNetClient.SerializeAndWriteToFile(
                adoPipeline: githubPipeline,
                path: buildScriptPath);
        }
    }
}
