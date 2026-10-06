# Live updates hub
Parent: [LiveUpdates.md](../../LiveUpdates.md)
Level: exposer — `LiveUpdatesHub` (`Websites/Glory2Him.WebApp/Hubs/`), a SignalR hub, new
Inherits: §ARC12.12, §EVN13, §SEC14.8 rules 1, 2 and 6, LiveUpdates.md rules 4–8

The live connection's endpoint. It is to the connection what a controller is to a route, thin and holding no logic, and thinner still: it takes no service at all, because a reader sends nothing over the connection (§SEC14.8 rule 6). What it carries is sent through it from inside, by `ILiveUpdateBroker` (`Backend/Brokers/LiveUpdateBroker.md §1`). Its acceptance tests are the proof, over real HTTP, of the whole path from a write, through the forwarder (`Backend/Orchestrations/LiveUpdateOrchestrationService.md`), to a connected reader.

**The dependency it grants.** `Microsoft.AspNetCore.SignalR.Client`, at 10.0.10 like the solution's other ASP.NET Core packages, is added to `Glory2Him.WebApp.Tests.Acceptance` and to no other project. Its tests connect to the hub as the React app does, and the `Microsoft.AspNetCore.App` shared framework carries SignalR's server, not its client (§ARC12.12 rule 4).

## 1. The hub at /api/LiveUpdates (#T14)

1. **The hub is mapped at `/api/LiveUpdates`** in `Program.cs`, after authorization and before the `/api` fallback, and is open to every caller (`[AllowAnonymous]`). A signed-out reader connects as a signed-in one does, and hears the same messages (§SEC14.8 rule 2).
2. **It declares no method a client may call.** SignalR refuses a client's invocation as one that names no method, and nothing is read or written (§SEC14.8 rule 6).
3. **A connected reader receives what the forwarder sends, as `ReceiveLiveUpdate`** (`Backend/Brokers/LiveUpdateBroker.md §1`): a setting message for every setting written over HTTP (LiveUpdates.md rule 5), and a count message for every reaction change on a host whose counts anyone may read (LiveUpdates.md rules 6 and 8).
4. **It receives nothing the forwarder withholds**: no count message for a host with no canonically visible version, nor for one whose winning setting hides reactions (§SEC14.8 rule 1).
