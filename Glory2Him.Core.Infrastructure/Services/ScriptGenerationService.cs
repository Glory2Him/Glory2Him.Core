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
using ADotNet.Clients;
using ADotNet.Models.Pipelines.GithubPipelines.DotNets;
using ADotNet.Models.Pipelines.GithubPipelines.DotNets.Tasks;
using ADotNet.Models.Pipelines.GithubPipelines.DotNets.Tasks.SetupDotNetTaskV5s;
using Glory2Him.Core.Infrastructure.Models.Pipelines;

namespace Glory2Him.Core.Infrastructure.Services
{
    internal class ScriptGenerationService
    {
        // ADotNet 5.1.0 models no `working-directory:` key on a step, so every npm command
        // carries its own `cd`. The four React gates exist as separate steps — and not as
        // anything `dotnet build` does — because the esproj sets ShouldRunBuildScript=false
        // and therefore never runs npm at all.
        private const string ReactAppPath = "Websites/Glory2Him.WebApp.React";

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

                    // The branch filter is NOT a duplicate of Push.Branches above: without it the
                    // build would also run for pull requests targeting a branch other than main.
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

                                // The publish target and every React gate below build the SPA, so
                                // pin Node rather than relying on whatever the runner image ships.
                                new GithubTask
                                {
                                    Name = "Setup Node",
                                    Uses = "actions/setup-node@v4",

                                    With = new Dictionary<string, string>
                                    {
                                        { "node-version", "22" },
                                        { "cache", "npm" },
                                        { "cache-dependency-path", $"{ReactAppPath}/package-lock.json" }
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

                                new GithubTask
                                {
                                    Name = "Restore React Dependencies",
                                    Run =
                                        $"""
                                        cd {ReactAppPath}
                                        npm ci
                                        """
                                },

                                new GithubTask
                                {
                                    Name = "Lint React App",
                                    Run =
                                        $"""
                                        cd {ReactAppPath}
                                        npm run lint
                                        """
                                },

                                new GithubTask
                                {
                                    Name = "Run React Unit Tests",
                                    Run =
                                        $"""
                                        cd {ReactAppPath}
                                        npm run test
                                        """
                                },

                                new GithubTask
                                {
                                    Name = "Build React App",
                                    Run =
                                        $"""
                                        # `npm run build` is `tsc -b && vite build`, so this type-checks BOTH tsconfig
                                        # projects and then proves the bundle still builds. `dotnet build` cannot stand
                                        # in for it: the esproj sets ShouldRunBuildScript=false, so it never runs npm.
                                        cd {ReactAppPath}
                                        npm run build
                                        """
                                },

                                new TestTask
                                {
                                    Name = "Run Unit Tests",
                                    Shell = "pwsh",
                                    Run =
                                        """
                                        # A pwsh step takes its conclusion from the LAST command, so without this
                                        # guard only the last discovered project could fail the build and every
                                        # earlier project's failure was discarded silently. Same guard as below.
                                        $projects = Get-ChildItem -Path . -Filter "*Tests.Unit*.csproj" -Recurse
                                        foreach ($project in $projects) {
                                          Write-Host "Running tests for: $($project.FullName)"
                                          dotnet test $project.FullName --no-build --verbosity normal
                                          if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
                                        }
                                        """
                                },

                                new TestTask
                                {
                                    Name = "Run Acceptance Tests",
                                    Shell = "pwsh",
                                    Run =
                                        """
                                        $projects = Get-ChildItem -Path . -Filter "*Tests.Acceptance*.csproj" -Recurse
                                        foreach ($project in $projects) {
                                          Write-Host "Running tests for: $($project.FullName)"
                                          dotnet test $project.FullName --no-build --verbosity normal
                                          if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
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
                                    Name = "Verify the model matches the migrations",
                                    Shell = "pwsh",
                                    Run =
                                        """
                                        # A configuration change with no migration behind it leaves the snapshot and the
                                        # history describing a different schema from the model. Nothing else in the build
                                        # notices — the code compiles and every test passes against the model — and the
                                        # drift only surfaces when Database.Migrate() runs somewhere real.
                                        dotnet tool install --global dotnet-ef --version 10.0.10 `
                                          || dotnet tool update --global dotnet-ef --version 10.0.10
                                        dotnet ef migrations has-pending-model-changes `
                                          --project Glory2Him.Core/Glory2Him.Core.csproj `
                                          --context Glory2Him.Core.Brokers.Storages.Sql.StorageBroker
                                        if ($LASTEXITCODE -ne 0) { throw "the model has changes no migration carries" }
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
                        new DeploymentJob
                        {
                            Name = "Deploy Web App To Azure (g2h-dev)",
                            RunsOn = BuildMachines.UbuntuLatest,
                            Needs = ["publish_webapp"],

                            // Binds the run to the Development environment and hangs the deployed
                            // URL off it in GitHub's UI.
                            Environment = new DeploymentEnvironment
                            {
                                Name = "Development",
                                Url = "${{ steps.deploy_to_webapp.outputs.webapp-url }}"
                            },

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
                                        { "client-id", "${{ secrets.AZURE_DEV_CLIENTID }}" },
                                        { "tenant-id", "${{ secrets.AZURE_DEV_TENANTID }}" },
                                        { "subscription-id", "${{ secrets.AZURE_DEV_SUBSCRIPTIONID }}" }
                                    }
                                },

                                new GithubTask
                                {
                                    Name = "Deploy to Azure Web App",
                                    Id = "deploy_to_webapp",
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

            WriteWorkflow(githubPipeline, workflowFileName: "build.yml");
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

            WriteWorkflow(githubPipeline, workflowFileName: "prLinter.yml");
        }

        /// <summary>
        /// Anchored on the assembly's own location rather than the current directory, so
        /// <c>dotnet run</c> from the project folder and running the built executable from
        /// <c>bin/Debug/net10.0</c> both write the SAME file. A relative path resolved against the
        /// working directory silently wrote the workflows outside the repository — and the
        /// regeneration check is only worth anything if it cannot miss.
        /// </summary>
        private void WriteWorkflow(GithubPipeline githubPipeline, string workflowFileName)
        {
            string workflowsDirectory = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "../../../../.github/workflows"));

            if (!Directory.Exists(workflowsDirectory))
            {
                Directory.CreateDirectory(workflowsDirectory);
            }

            adotNetClient.SerializeAndWriteToFile(
                adoPipeline: githubPipeline,
                path: Path.Combine(workflowsDirectory, workflowFileName));
        }
    }
}
