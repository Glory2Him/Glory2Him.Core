// Wire shape of the hub's ReceiveLiveUpdate message (Backend/Models/LiveUpdate.md §1): the type
// travels as a number mirroring the server's LiveUpdateType, and contentItemId is null on a setting
// change, which names no one content item.
export enum LiveUpdateType {
    ContentItemSettingChanged = 1,
    ContentItemReactionsChanged = 2,
}

export type LiveUpdate = {
    type: LiveUpdateType;
    contentItemId: string | null;
};

export type LiveUpdateListeners = {
    onLiveUpdate: (liveUpdate: LiveUpdate) => void;
    onReconnected: () => void;
    onClosed: () => void;
};
