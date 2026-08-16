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

using System.Collections.Generic;
using System.IO;
using ADotNet.Clients;
using ADotNet.Models.Pipelines.GithubPipelines.DotNets;
using ADotNet.Models.Pipelines.GithubPipelines.DotNets.Tasks;
using ADotNet.Models.Pipelines.GithubPipelines.DotNets.Tasks.SetupDotNetTaskV5s;

namespace Glory2Him.Core.Infrastructure.Services
{
    internal class ScriptGenerationService
    {
        private readonly ADotNetClient adotNetClient;

        public ScriptGenerationService() =>
            adotNetClient = new ADotNetClient();

        public void GenerateBuildScript(
            string branchName,
            string projectName,
            string dotNetVersion)
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
                                new CheckoutTaskV5
                                {
                                    Name = "Check out"
                                },

                                new SetupDotNetTaskV5
                                {
                                    Name = "Setup .Net",

                                    With = new TargetDotNetVersionV5
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
                                    Shell = "pwsh",
                                    Run =
                                        """
                                        $projects = Get-ChildItem -Path . -Filter "*Tests.Unit*.csproj" -Recurse
                                        foreach ($project in $projects) {
                                          Write-Host "Running tests for: $($project.FullName)"
                                          dotnet test $project.FullName --no-build --verbosity normal
                                        }
                                        """
                                },

                                new TestTask
                                {
                                    Name = "Run Acceptance Tests",
                                    Run =
                                        """
                                        $projects = Get-ChildItem -Path . -Filter "*Tests.Acceptance*.csproj" -Recurse
                                        foreach ($project in $projects) {
                                          Write-Host "Running tests for: $($project.FullName)"
                                          dotnet test $project.FullName --no-build --verbosity normal
                                        }
                                        """
                                },

                                new TestTask
                                {
                                    Name = "Run Integration Tests",
                                    Shell = "pwsh",
                                    Run =
                                        """
                                        # windows-latest ships SQL Server LocalDB; the fixtures connect to
                                        # (localdb)\MSSQLLocalDB and create one database per process id.
                                        sqllocaldb start MSSQLLocalDB
                                        $projects = Get-ChildItem -Path . -Filter "*Tests.Integration*.csproj" -Recurse
                                        foreach ($project in $projects) {
                                          Write-Host "Running integration tests for: $($project.FullName)"
                                          dotnet test $project.FullName --no-build --verbosity normal
                                          if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
                                        }
                                        """
                                },

                                new TestTask
                                {
                                    Name = "Verify migration script applies under sqlcmd defaults",
                                    Shell = "pwsh",
                                    Run =
                                        """
                                        # Regression guard for the QUOTED_IDENTIFIER fix: the generated script must
                                        # carry its own SET options so it applies under sqlcmd's default
                                        # QUOTED_IDENTIFIER OFF. We regenerate via the tool and apply WITHOUT -I; a
                                        # broken header fails the very first CREATE INDEX with Msg 1934.
                                        dotnet tool install --global dotnet-ef --version 10.0.10 `
                                          || dotnet tool update --global dotnet-ef --version 10.0.10
                                        bash Tools/new-database-script.sh Glory2Him.Core.Database.sql
                                        if ($LASTEXITCODE -ne 0) { throw "migration script generation failed" }
                                        sqllocaldb start MSSQLLocalDB
                                        sqlcmd -S "(localdb)\MSSQLLocalDB" -b -Q "IF DB_ID('G2H_MigrationCheck') IS NOT NULL DROP DATABASE [G2H_MigrationCheck]; CREATE DATABASE [G2H_MigrationCheck];"
                                        if ($LASTEXITCODE -ne 0) { throw "could not create the check database" }
                                        sqlcmd -S "(localdb)\MSSQLLocalDB" -d "G2H_MigrationCheck" -i "Glory2Him.Core.Database.sql" -b
                                        if ($LASTEXITCODE -ne 0) { throw "migration script failed under sqlcmd defaults (QUOTED_IDENTIFIER)" }
                                        sqlcmd -S "(localdb)\MSSQLLocalDB" -b -Q "DROP DATABASE [G2H_MigrationCheck];"
                                        """
                                }
                            }
                        }
                    },
                    {
                        "add_tag",
                        new TagJobV2(
                            runsOn: BuildMachines.UbuntuLatest,
                            dependsOn: "build",
                            projectRelativePath: $"{projectName}/{projectName}.csproj",
                            githubToken: "${{ secrets.PAT_FOR_TAGGING }}",
                            branchName: branchName)
                        {
                            Name = "Tag and Release"
                        }
                    },

                    // The Azure deploy is gated on the full build job (all test suites and the
                    // migration-script check) rather than racing it from a separate workflow.
                    // It only fires for pushes to main — never for pull request runs.
                    {
                        "publish_webapp",
                        new Job
                        {
                            Name = "Publish Web App",
                            RunsOn = BuildMachines.UbuntuLatest,
                            Needs = ["build"],

                            If =
                                "github.event_name == 'push' && " +
                                $"github.ref == 'refs/heads/{branchName}'",

                            Steps = new List<GithubTask>
                            {
                                new CheckoutTaskV5
                                {
                                    Name = "Check out"
                                },

                                new SetupDotNetTaskV5
                                {
                                    Name = "Setup .Net",

                                    With = new TargetDotNetVersionV5
                                    {
                                        DotNetVersion = dotNetVersion
                                    }
                                },

                                // The publish target builds the React SPA (npm install +
                                // npm run build in Glory2Him.WebApp.React), so pin Node
                                // rather than relying on the runner image.
                                new GithubTask
                                {
                                    Name = "Setup Node",
                                    Uses = "actions/setup-node@v4",

                                    With = new Dictionary<string, string>
                                    {
                                        { "node-version", "22" }
                                    }
                                },

                                // Publish ONLY the web host. Publishing the whole solution
                                // dumps every project into one folder and fails with
                                // NETSDK1152 collisions.
                                new GithubTask
                                {
                                    Name = "Publish Web App",

                                    Run =
                                        "dotnet publish Websites/Glory2Him.WebApp/Glory2Him.WebApp.csproj " +
                                        "-c Release -o ${{ env.DOTNET_ROOT }}/webapp"
                                },

                                new GithubTask
                                {
                                    Name = "Upload artifact for deployment job",
                                    Uses = "actions/upload-artifact@v4",

                                    With = new Dictionary<string, string>
                                    {
                                        { "name", ".net-app" },
                                        { "path", "${{ env.DOTNET_ROOT }}/webapp" }
                                    }
                                }
                            }
                        }
                    },
                    {
                        "deploy_webapp",
                        new Job
                        {
                            Name = "Deploy Web App To Azure (g2h-dev)",
                            RunsOn = BuildMachines.UbuntuLatest,
                            Needs = ["publish_webapp"],

                            Permissions = new Dictionary<string, string>
                            {
                                // Required for requesting the OIDC JWT azure/login exchanges.
                                { "id-token", "write" },
                                { "contents", "read" }
                            },

                            Steps = new List<GithubTask>
                            {
                                new GithubTask
                                {
                                    Name = "Download artifact from publish job",
                                    Uses = "actions/download-artifact@v4",

                                    With = new Dictionary<string, string>
                                    {
                                        { "name", ".net-app" }
                                    }
                                },

                                new GithubTask
                                {
                                    Name = "Login to Azure",
                                    Uses = "azure/login@v2",

                                    With = new Dictionary<string, string>
                                    {
                                        { "client-id", "${{ secrets.AZUREAPPSERVICE_CLIENTID_8FD47A61697C42B88B3A8698602713EF }}" },
                                        { "tenant-id", "${{ secrets.AZUREAPPSERVICE_TENANTID_FAA97044B19A4630977F2C86C14AB5A4 }}" },
                                        { "subscription-id", "${{ secrets.AZUREAPPSERVICE_SUBSCRIPTIONID_5C67A75889F64364850175AD19913AA1 }}" }
                                    }
                                },

                                new GithubTask
                                {
                                    Name = "Deploy to Azure Web App",
                                    Uses = "azure/webapps-deploy@v3",

                                    With = new Dictionary<string, string>
                                    {
                                        { "app-name", "g2h-dev" },
                                        { "slot-name", "Production" },
                                        { "package", "." }
                                    }
                                }
                            }
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
                        new LabelJobV3(runsOn: BuildMachines.UbuntuLatest)
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
                        new RequireIssueOrTaskJobV2(excludedAuthors: "dependabot[bot]")
                        {
                            Name = "Require Issue Or Task Association",
                        }
                    },
                    {
                        "setAuthorAsPrAssignee",
                        new SetAuthorAsPrAssigneeJobV2(runsOn: BuildMachines.UbuntuLatest)
                        {
                            Name = "Set Author As PR Assignee",
                        }
                    }
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
