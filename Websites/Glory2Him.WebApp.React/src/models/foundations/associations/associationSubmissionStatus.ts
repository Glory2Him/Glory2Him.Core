// Mirrors Glory2Him.Core.Models.Orchestrations.Associations.AssociationSubmissionStatus,
// including its numbering: the wire carries the NUMBER — the host registers no
// JsonStringEnumConverter — so these values are a contract rather than a convenience.
export enum AssociationSubmissionStatus {
    Created = 0,
    AlreadySuggested = 1,
    AlreadySuggestedByCaller = 2,
    PreviouslySuggestedByCaller = 3,
}
