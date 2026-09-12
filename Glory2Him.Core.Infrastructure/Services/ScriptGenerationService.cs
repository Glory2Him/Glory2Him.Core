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
                            RunsOn = BuildMachines.UbuntuLatest,

                            // Literals only. A job-level env block is evaluated before any step
                            // runs, so it can see github, inputs, matrix, needs, secrets, strategy
                            // and vars and nothing else — anything a step computes has to travel
                            // through $GITHUB_ENV instead, which is how the connection strings
                            // below reach the steps that need them.
                            EnvironmentVariables = new Dictionary<string, string>
                            {
                                // The version the migrations are rehearsed against, stated here so
                                // it is one line to find and one line to move.
                                { "SQL_SERVER_IMAGE", "mcr.microsoft.com/mssql/server:2022-latest" },
                                { "SQL_SERVER_CONTAINER", "g2h-sql" },

                                // The ODBC sqlcmd that ships INSIDE the server image, and the
                                // choice matters: it defaults QUOTED_IDENTIFIER OFF, whereas
                                // go-sqlcmd defaults it ON and would make the migration rehearsal
                                // below pass without proving anything (design 12.10 rule 6).
                                { "SQLCMD", "/opt/mssql-tools18/bin/sqlcmd" }
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

                                // Started here rather than immediately before the first suite that
                                // needs it: restore, build and the four React gates take minutes,
                                // and the server spends them recovering and warming instead of
                                // making the first Database.Migrate() wait.
                                new GithubTask
                                {
                                    Name = "Start SQL Server",
                                    Shell = "bash",
                                    Run =
                                        """
                                        # ubuntu-latest has no LocalDB, so the job runs its own SQL Server and every
                                        # database-backed suite reaches it through a TEST-ONLY key. Not one of the
                                        # portal's three PRODUCTION connection keys is set anywhere in this workflow,
                                        # and their absence is the point rather than an oversight: the
                                        # catalogue-dropping fixtures resolve test-only keys precisely so a host
                                        # configured through the environment cannot be answered by their EnsureDeleted
                                        # (#302, #351, design 12.10 rule 1). Exporting a production key here would not
                                        # turn the build red; it would drop a real database. Their names are left out
                                        # of this file entirely so that grepping for one and finding nothing is a
                                        # meaningful check.
                                        set -euo pipefail

                                        # Minted for this run and nothing else — no repository or environment secret
                                        # exists for it. The mask is registered BEFORE the value is used anywhere.
                                        SA_PASSWORD="G2h_$(openssl rand -hex 18)Aa1"
                                        echo "::add-mask::$SA_PASSWORD"

                                        docker run --detach --name "$SQL_SERVER_CONTAINER" --publish 1433:1433 --env ACCEPT_EULA=Y --env MSSQL_PID=Developer --env MSSQL_SA_PASSWORD="$SA_PASSWORD" "$SQL_SERVER_IMAGE"

                                        # Readiness is a QUERY the engine answers, not a port that accepts. SQL Server
                                        # binds 1433 before it has finished recovering master, and a suite connecting in
                                        # that window does not fail to connect — it fails its first Database.Migrate()
                                        # on a command timeout, which reads like a flaky test rather than a server that
                                        # was not up yet.
                                        for attempt in $(seq 1 60); do
                                          if docker exec "$SQL_SERVER_CONTAINER" "$SQLCMD" -S localhost -U sa -P "$SA_PASSWORD" -C -b -Q "SELECT 1" > /dev/null 2>&1; then
                                            echo "SQL Server answered a query after $attempt attempt(s)."
                                            break
                                          fi
                                          if [ "$attempt" -eq 60 ]; then
                                            docker logs "$SQL_SERVER_CONTAINER"
                                            echo "SQL Server never answered a query."
                                            exit 1
                                          fi
                                          sleep 2
                                        done

                                        # Integrated authentication cannot reach a container, so CI authenticates with
                                        # the credential minted above and never with the trusted-connection route the
                                        # dev loop uses — that keyword appears nowhere in this file, so grepping for it
                                        # and finding nothing is a meaningful check. Microsoft.Data.SqlClient also
                                        # defaults Encrypt=True against a certificate the container signs itself, so
                                        # that is stated rather than discovered as a connection failure. Command
                                        # Timeout is a property of THIS server rather than of any suite: building a
                                        # schema into an empty container is slower than a warm LocalDB, and SqlCommand
                                        # takes the connection's value when EF sets none.
                                        SERVER="Server=localhost,1433"
                                        CREDENTIALS="User ID=sa;Password=$SA_PASSWORD;Encrypt=True;TrustServerCertificate=True"
                                        OPTIONS="MultipleActiveResultSets=true;Connect Timeout=60;Command Timeout=300;ConnectRetryCount=5;ConnectRetryInterval=5"

                                        # The catalogue names match each project's own appsettings.json, so the only
                                        # thing these override is WHERE the server is and HOW the suite authenticates.
                                        # The three integration keys and the acceptance key are templates whose
                                        # Database the owning fixture replaces per process id.
                                        echo "ConnectionStrings__Glory2HimCoreIntegrationConnectionString=$SERVER;Database=Glory2Him.Core_Integration;$CREDENTIALS;$OPTIONS" >> "$GITHUB_ENV"
                                        echo "ConnectionStrings__EventHighwayIntegrationConnectionString=$SERVER;Database=Glory2Him.Events_Integration;$CREDENTIALS;$OPTIONS" >> "$GITHUB_ENV"
                                        echo "ConnectionStrings__Glory2HimSecurityIntegrationConnectionString=$SERVER;Database=Glory2Him.Security_Integration;$CREDENTIALS;$OPTIONS" >> "$GITHUB_ENV"
                                        echo "ConnectionStrings__Glory2HimAcceptanceConnectionString=$SERVER;Database=Glory2HimAcceptance;$CREDENTIALS;$OPTIONS" >> "$GITHUB_ENV"

                                        # DefaultConnection is read by BOTH storage-client suites, which hold SEPARATE
                                        # migration histories over the same table — pointing them at one catalogue makes
                                        # whichever runs second fail on an object that already exists. The job value
                                        # serves the acceptance suite; the integration step overrides it with the
                                        # catalogue its own appsettings.json names.
                                        echo "ConnectionStrings__DefaultConnection=$SERVER;Database=EFCoreClientAcceptance;$CREDENTIALS;$OPTIONS" >> "$GITHUB_ENV"
                                        echo "STORAGE_CLIENT_INTEGRATION_CONNECTION=$SERVER;Database=EFCoreClientIntegration;$CREDENTIALS;$OPTIONS" >> "$GITHUB_ENV"

                                        # Carried forward for the migration rehearsal, which talks to the server
                                        # directly rather than through a fixture. Masked above, so it cannot print.
                                        echo "SQL_SERVER_SA_PASSWORD=$SA_PASSWORD" >> "$GITHUB_ENV"
                                        """
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
                                    Shell = "bash",
                                    Run =
                                        $"""
                                        cd {ReactAppPath}
                                        npm ci
                                        """
                                },

                                new GithubTask
                                {
                                    Name = "Lint React App",
                                    Shell = "bash",
                                    Run =
                                        $"""
                                        cd {ReactAppPath}
                                        npm run lint
                                        """
                                },

                                new GithubTask
                                {
                                    Name = "Run React Unit Tests",
                                    Shell = "bash",
                                    Run =
                                        $"""
                                        cd {ReactAppPath}
                                        npm run test
                                        """
                                },

                                new GithubTask
                                {
                                    Name = "Build React App",
                                    Shell = "bash",
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
                                    Shell = "bash",
                                    Run =
                                        """
                                        # `find` stands in for Get-ChildItem -Recurse and discovers the same set,
                                        # including the projects under Clients/ and Websites/. The per-project exit
                                        # check is not decoration: a step takes its conclusion from the LAST command,
                                        # so without it only the last discovered project could fail the build and
                                        # every earlier project's failure was discarded silently. Same guard below.
                                        set -euo pipefail
                                        for project in $(find . -name "*Tests.Unit*.csproj" -not -path "*/node_modules/*" | sort); do
                                          echo "Running tests for: $project"
                                          dotnet test "$project" --no-build --verbosity normal || exit $?
                                        done
                                        """
                                },

                                new TestTask
                                {
                                    Name = "Run Acceptance Tests",
                                    Shell = "bash",
                                    Run =
                                        """
                                        # The WebApp and storage-client acceptance suites both open real connections,
                                        # so they resolve the same job-scoped test-only keys the integration step does.
                                        set -euo pipefail
                                        for project in $(find . -name "*Tests.Acceptance*.csproj" -not -path "*/node_modules/*" | sort); do
                                          echo "Running tests for: $project"
                                          dotnet test "$project" --no-build --verbosity normal || exit $?
                                        done
                                        """
                                },

                                new TestTask
                                {
                                    Name = "Run Integration Tests",
                                    Shell = "bash",

                                    // See the DefaultConnection remark in "Start SQL Server": the two
                                    // storage-client suites share the key but not the schema, so this
                                    // step names the catalogue its own appsettings.json names.
                                    EnvironmentVariables = new Dictionary<string, string>
                                    {
                                        {
                                            "ConnectionStrings__DefaultConnection",
                                            "${{ env.STORAGE_CLIENT_INTEGRATION_CONNECTION }}"
                                        }
                                    },

                                    Run =
                                        """
                                        # The fixtures resolve their per-store test-only key from the environment and
                                        # replace the catalogue with one named for this process id, so nothing here has
                                        # to create a database or name one.
                                        set -euo pipefail
                                        for project in $(find . -name "*Tests.Integration*.csproj" -not -path "*/node_modules/*" | sort); do
                                          echo "Running integration tests for: $project"
                                          dotnet test "$project" --no-build --verbosity normal || exit $?
                                        done
                                        """
                                },

                                new TestTask
                                {
                                    Name = "Verify the model matches the migrations",
                                    Shell = "bash",
                                    Run =
                                        """
                                        # A configuration change with no migration behind it leaves the snapshot and the
                                        # history describing a different schema from the model. Nothing else in the build
                                        # notices — the code compiles and every test passes against the model — and the
                                        # drift only surfaces when Database.Migrate() runs somewhere real.
                                        #
                                        # The command answers from the model and the migration history and opens no
                                        # connection, so it is indifferent to the container above.
                                        set -euo pipefail
                                        export PATH="$PATH:$HOME/.dotnet/tools"
                                        dotnet tool install --global dotnet-ef --version 10.0.10 || dotnet tool update --global dotnet-ef --version 10.0.10
                                        if ! dotnet ef migrations has-pending-model-changes --project Glory2Him.Core/Glory2Him.Core.csproj --context Glory2Him.Core.Brokers.Storages.Sql.StorageBroker; then
                                          echo "the model has changes no migration carries"
                                          exit 1
                                        fi
                                        """
                                },

                                new TestTask
                                {
                                    Name = "Verify migration script applies under sqlcmd defaults",
                                    Shell = "bash",
                                    Run =
                                        """
                                        # Regression guard for the QUOTED_IDENTIFIER fix: the generated script must
                                        # carry its own SET options so it applies under a client whose
                                        # QUOTED_IDENTIFIER default is OFF. We regenerate via the tool and apply
                                        # WITHOUT -I; a broken header fails the very first CREATE INDEX with Msg 1934.
                                        set -euo pipefail
                                        export PATH="$PATH:$HOME/.dotnet/tools"
                                        dotnet tool install --global dotnet-ef --version 10.0.10 || dotnet tool update --global dotnet-ef --version 10.0.10
                                        bash Tools/new-database-script.sh Glory2Him.Core.Database.sql

                                        # The negative control, and it comes FIRST because it is what keeps the rest
                                        # honest. A copy of the script with the tool's SET header removed must FAIL to
                                        # apply. If it applies, this client is defaulting QUOTED_IDENTIFIER ON, the
                                        # rehearsal below would pass without exercising anything, and the schema's
                                        # filtered indexes would have lost their only cover — so the job fails on the
                                        # SUCCESS of this one. awk drops everything up to and including the header's
                                        # terminating GO, which is the first line in the file that is exactly "GO".
                                        awk 'stripped { print } /^GO$/ && !stripped { stripped = 1 }' Glory2Him.Core.Database.sql > Glory2Him.Core.Database.HeaderStripped.sql

                                        docker cp Glory2Him.Core.Database.sql "$SQL_SERVER_CONTAINER":/tmp/with-header.sql
                                        docker cp Glory2Him.Core.Database.HeaderStripped.sql "$SQL_SERVER_CONTAINER":/tmp/without-header.sql

                                        docker exec "$SQL_SERVER_CONTAINER" "$SQLCMD" -S localhost -U sa -P "$SQL_SERVER_SA_PASSWORD" -C -b -Q "IF DB_ID('G2H_MigrationCheck') IS NOT NULL DROP DATABASE [G2H_MigrationCheck]; CREATE DATABASE [G2H_MigrationCheck]; IF DB_ID('G2H_HeaderStrippedCheck') IS NOT NULL DROP DATABASE [G2H_HeaderStrippedCheck]; CREATE DATABASE [G2H_HeaderStrippedCheck];"

                                        if docker exec "$SQL_SERVER_CONTAINER" "$SQLCMD" -S localhost -U sa -P "$SQL_SERVER_SA_PASSWORD" -C -b -d G2H_HeaderStrippedCheck -i /tmp/without-header.sql; then
                                          echo "the header-stripped script applied, so this client defaults QUOTED_IDENTIFIER ON and the rehearsal proves nothing"
                                          exit 1
                                        fi
                                        echo "The header-stripped script failed as required: this client defaults QUOTED_IDENTIFIER OFF."

                                        # Applied without -I, exactly as a deployment would apply it.
                                        docker exec "$SQL_SERVER_CONTAINER" "$SQLCMD" -S localhost -U sa -P "$SQL_SERVER_SA_PASSWORD" -C -b -d G2H_MigrationCheck -i /tmp/with-header.sql

                                        docker exec "$SQL_SERVER_CONTAINER" "$SQLCMD" -S localhost -U sa -P "$SQL_SERVER_SA_PASSWORD" -C -b -Q "DROP DATABASE [G2H_MigrationCheck]; DROP DATABASE [G2H_HeaderStrippedCheck];"
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
