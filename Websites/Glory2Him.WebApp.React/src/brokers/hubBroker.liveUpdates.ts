import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { LiveUpdate, LiveUpdateListeners } from '../models/foundations/liveUpdates/liveUpdate';

// The tab's one live connection (UI/Brokers/LiveUpdateBroker.md). The connection's handlers are
// registered once, when it is built, and forward to whatever the latest start was handed: SignalR
// offers no way to take an onreconnected or onclose handler off again, so registering a call's
// listeners directly would leave an earlier call's live, or fire one twice, after a restart.
class LiveUpdateBroker {
    relativeLiveUpdatesUrl = '/api/LiveUpdates';
    private hubConnection: HubConnection | null = null;
    private listeners: LiveUpdateListeners | null = null;
    private nextRetryDelayInMilliseconds: ((previousRetryCount: number) => number | null) | null = null;

    // Catches nothing: a failed start rejects with SignalR's own error, unchanged, as the user
    // story's one deviation has it.
    async startLiveUpdatesAsync(
        listeners: LiveUpdateListeners,
        nextRetryDelayInMilliseconds: (previousRetryCount: number) => number | null
    ): Promise<void> {
        this.listeners = listeners;
        this.nextRetryDelayInMilliseconds = nextRetryDelayInMilliseconds;
        this.hubConnection ??= this.buildHubConnection();

        await this.hubConnection.start();
    }

    private buildHubConnection(): HubConnection {
        const hubConnection = new HubConnectionBuilder()
            .withUrl(this.relativeLiveUpdatesUrl)
            .withAutomaticReconnect({
                nextRetryDelayInMilliseconds: retryContext =>
                    this.nextRetryDelayInMilliseconds!(retryContext.previousRetryCount)
            })
            .build();

        hubConnection.on('ReceiveLiveUpdate', (liveUpdate: LiveUpdate) =>
            this.listeners!.onLiveUpdate(liveUpdate));

        hubConnection.onreconnected(() => this.listeners!.onReconnected());
        hubConnection.onclose(() => this.listeners!.onClosed());

        return hubConnection;
    }
}

export default LiveUpdateBroker;
