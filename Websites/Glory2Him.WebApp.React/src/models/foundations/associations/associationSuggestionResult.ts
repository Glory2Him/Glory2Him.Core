// Mirrors Glory2Him.Core.Models.Orchestrations.Associations.AssociationSuggestionStatus,
// including its numbering: the wire carries the NUMBER — the host registers no
// JsonStringEnumConverter — so these values are a contract rather than a convenience.
export enum AssociationSuggestionStatus {
    Created = 0,
    AlreadyPending = 1,
    AlreadyApproved = 2,
    OverlapsExisting = 3,
    Restored = 4,
    Repointed = 5,
}

export interface AssociationSuggestionResult {
    status: AssociationSuggestionStatus;
    associationId: string;
}
